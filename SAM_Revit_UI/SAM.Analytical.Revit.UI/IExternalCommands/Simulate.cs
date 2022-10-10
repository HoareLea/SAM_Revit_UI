using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Analytical.UI;
using SAM.Core.Revit;
using SAM.Core.Revit.UI;
using SAM.Core.Tas;
using SAM.Geometry.Spatial;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Simulate : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Tas";

        public override int Index => 17;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Simulate, 32, 32);

        public override string Text => "Simulate";

        public override string ToolTip => "Simulate";

        public override string AvailabilityClassName => null;

        public override Result Execute(ExternalCommandData externalCommandData, ref string message, ElementSet elementSet)
        {
            Document document = externalCommandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return Result.Failed;
            }

            string path = document.PathName;
            if (string.IsNullOrWhiteSpace(path))
            {
                string name = document.Title;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "000000_SAM_AnalyticalModel";
                }

                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "Select Directory";
                    folderBrowserDialog.ShowNewFolderButton = true;
                    if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
                    {
                        return Result.Cancelled;
                    }

                    path = System.IO.Path.Combine(folderBrowserDialog.SelectedPath, name + ".rvt");
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    return Result.Failed;
                }

                document.SaveAs(path);
            }

            string projectName = null;
            string outputDirectory = null;
            bool unmetHours = false;
            WeatherData weatherData = null;
            SolarCalculationMethod solarCalculationMethod = SolarCalculationMethod.None;
            GeometryCalculationMethod geometryCalculationMethod = GeometryCalculationMethod.SAM;
            bool updateConstructionLayersByPanelType = false;
            bool printRoomDataSheets = false;

            using (Forms.SimulateForm simulateForm = new Forms.SimulateForm(System.IO.Path.GetFileNameWithoutExtension(path), System.IO.Path.GetDirectoryName(path)))
            {
                Parameter parameter = document.ProjectInformation.LookupParameter("SAM_WeatherFile");
                simulateForm.WeatherData = Core.Convert.ToSAM<WeatherData>(parameter?.AsString())?.FirstOrDefault();

                if (simulateForm.ShowDialog() != DialogResult.OK)
                {
                    return Result.Cancelled;
                }

                projectName = simulateForm.ProjectName;
                outputDirectory = simulateForm.OutputDirectory;
                unmetHours = simulateForm.UnmetHours;
                weatherData = simulateForm.WeatherData;
                solarCalculationMethod = simulateForm.SolarCalculationMethod;
                geometryCalculationMethod = simulateForm.GeometryCalculationMethod;
                updateConstructionLayersByPanelType = simulateForm.UpdateConstructionLayersByPanelType;
                printRoomDataSheets = simulateForm.RoomDataSheets;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (weatherData == null || geometryCalculationMethod == GeometryCalculationMethod.Undefined)
            {
                return Result.Failed;
            }

            AnalyticalModel analyticalModel = null;

            bool simulate = false;

            string path_TBD = System.IO.Path.Combine(outputDirectory, projectName + ".tbd");

            Dictionary<Guid, ElementId> dictionary = null;
            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Preparing Model", 6))
            {
                progressForm.Update("Converting Model");
                analyticalModel = Convert.ToSAM(document, geometryCalculationMethod, out dictionary);

                if (analyticalModel == null)
                {
                    MessageBox.Show("Could not convert to AnalyticalModel");
                    return Result.Failed;
                }

                IEnumerable<Core.IMaterial> materials = Analytical.Query.Materials(analyticalModel.AdjacencyCluster, Analytical.Query.DefaultMaterialLibrary());
                if (materials != null)
                {
                    foreach (Core.IMaterial material in materials)
                    {
                        if (analyticalModel.HasMaterial(material))
                        {
                            continue;
                        }

                        analyticalModel.AddMaterial(material);
                    }
                }

                analyticalModel = updateConstructionLayersByPanelType ? analyticalModel.UpdateConstructionLayersByPanelType() : analyticalModel;

                if (System.IO.File.Exists(path_TBD))
                {
                    System.IO.File.Delete(path_TBD);
                }

                List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

                //Run Solar Calculation for cooling load

                progressForm.Update("Solar Calculations");
                if (solarCalculationMethod != SolarCalculationMethod.None)
                {
                    SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), false, Core.Tolerance.MacroDistance, Core.Tolerance.MacroDistance, 0.012, Core.Tolerance.Distance);
                }

                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    progressForm.Update("Updating WeatherData");
                    Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, analyticalModel == null ? 0 : analyticalModel.AdjacencyCluster.BuildingHeight());

                    TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                    List<TBD.dayType> dayTypes = Query.DayTypes(calendar);
                    if (dayTypes.Find(x => x.name == "HDD") == null)
                    {
                        TBD.dayType dayType = calendar.AddDayType();
                        dayType.name = "HDD";
                    }

                    if (dayTypes.Find(x => x.name == "CDD") == null)
                    {
                        TBD.dayType dayType = calendar.AddDayType();
                        dayType.name = "CDD";
                    }

                    progressForm.Update("Converting to TBD");
                    Tas.Convert.ToTBD(analyticalModel, tBDDocument);

                    progressForm.Update("Updating Zones");
                    Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                    progressForm.Update("Updating Shading");
                    simulate = Tas.Modify.UpdateShading(tBDDocument, analyticalModel);

                    sAMTBDDocument.Save();
                }
            }

            List<DesignDay> heatingDesignDays = new List<DesignDay>() { Analytical.Query.HeatingDesignDay(weatherData) };
            List<DesignDay> coolingDesignDays = new List<DesignDay>() { Analytical.Query.CoolingDesignDay(weatherData) };

            SurfaceOutputSpec surfaceOutputSpec = new SurfaceOutputSpec("Tas.Simulate")
            {
                SolarGain = true,
                Conduction = true,
                ApertureData = false,
                Condensation = false,
                Convection = false,
                LongWave = false,
                Temperature = false
            };

            List<SurfaceOutputSpec> surfaceOutputSpecs = new List<SurfaceOutputSpec>() { surfaceOutputSpec };

            analyticalModel = Tas.Modify.RunWorkflow(analyticalModel, path_TBD, null, null, heatingDesignDays, coolingDesignDays, surfaceOutputSpecs, unmetHours, simulate, false);

            List<Core.ISAMObject> results = null;

            AdjacencyCluster adjacencyCluster = null;
            if (analyticalModel != null)
            {
                adjacencyCluster = analyticalModel?.AdjacencyCluster;
                if (adjacencyCluster != null)
                {
                    results = new List<Core.ISAMObject>();
                    adjacencyCluster.GetObjects<SpaceSimulationResult>()?.ForEach(x => results.Add(x));
                    adjacencyCluster.GetObjects<ZoneSimulationResult>()?.ForEach(x => results.Add(x));
                    adjacencyCluster.GetObjects<AdjacencyClusterSimulationResult>()?.ForEach(x => results.Add(x));
                    adjacencyCluster.GetPanels()?.ForEach(x => results.Add(x));
                    adjacencyCluster.GetSpaces()?.ForEach(x => results.Add(x));
                }
            }

            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Inserting Results", results.Count + 5))
            {
                progressForm.Update("Processing Revit");
                if (adjacencyCluster != null && results != null && results.Count != 0)
                {
                    ConvertSettings convertSettings = new ConvertSettings(false, true, false);
                    convertSettings.AddParameter("AdjacencyCluster", adjacencyCluster);
                    convertSettings.AddParameter("AnalyticalModel", analyticalModel);

                    using (Transaction transaction = new Transaction(document, "Simulate"))
                    {
                        transaction.Start();

                        Parameter parameter = document.ProjectInformation.LookupParameter("SAM_WeatherFile");
                        parameter?.Set(Core.Convert.ToString(weatherData));

                        foreach (Space space in results.FindAll(x => x is Space))
                        {
                            progressForm.Update(string.IsNullOrWhiteSpace(space?.Name) ? "???" : space.Name);

                            ElementId elementId = space.ElementId();

                            if (elementId != null && elementId != ElementId.InvalidElementId)
                            {
                                if (space.TryGetValue(SpaceParameter.Occupancy, out double occupancy) && occupancy == 0)
                                {
                                    space.RemoveValue(SpaceParameter.Occupancy);
                                }

                                if (space.InternalCondition != null)
                                {
                                    InternalCondition internalCondition = space.InternalCondition;
                                    if (internalCondition.TryGetValue(InternalConditionParameter.AreaPerPerson, out double areaPerPerson) && areaPerPerson == 0)
                                    {
                                        internalCondition.RemoveValue(InternalConditionParameter.AreaPerPerson);
                                        space.InternalCondition = internalCondition;
                                    }
                                }

                                Core.Revit.Modify.SetValues(document.GetElement(elementId), space, ActiveSetting.Setting, parameters: convertSettings.GetParameters());
                            }
                        }

                        foreach (Core.ISAMObject sAMObject in results.FindAll(x => !(x is Space)))
                        {
                            progressForm.Update(sAMObject?.Name == null ? "???" : sAMObject.Name);

                            if (sAMObject is SpaceSimulationResult)
                            {
                                Revit.Convert.ToRevit(adjacencyCluster, (SpaceSimulationResult)sAMObject, document, convertSettings)?.Cast<Element>().ToList();
                            }
                            else if (sAMObject is ZoneSimulationResult)
                            {
                                Revit.Convert.ToRevit(adjacencyCluster, (ZoneSimulationResult)sAMObject, document, convertSettings)?.Cast<Element>().ToList();
                            }
                            else if (sAMObject is AdjacencyClusterSimulationResult)
                            {
                                Revit.Convert.ToRevit((AdjacencyClusterSimulationResult)sAMObject, document, convertSettings);
                            }
                            else if (sAMObject is Panel)
                            {
                                Panel panel = (Panel)sAMObject;

                                ElementId elementId = null;
                                if (dictionary != null)
                                {
                                    if (!dictionary.TryGetValue(panel.Guid, out elementId))
                                    {
                                        elementId = null;
                                    }
                                }

                                if (elementId == null)
                                {
                                    elementId = panel.ElementId();
                                }

                                if (elementId != null)
                                {
                                    Core.Revit.Modify.SetValues(document.GetElement(elementId), panel, ActiveSetting.Setting, parameters: convertSettings.GetParameters());
                                }

                                List<Aperture> apertures = panel.Apertures;
                                if (apertures != null)
                                {
                                    foreach (Aperture aperture in apertures)
                                    {
                                        elementId = null;
                                        if (dictionary != null)
                                        {
                                            if (!dictionary.TryGetValue(aperture.Guid, out elementId))
                                            {
                                                elementId = null;
                                            }
                                        }

                                        if (elementId == null)
                                        {
                                            elementId = aperture.ElementId();
                                        }

                                        if (elementId != null)
                                        {
                                            Core.Revit.Modify.SetValues(document.GetElement(elementId), aperture, ActiveSetting.Setting);
                                        }
                                    }
                                }
                            }
                        }

                        progressForm.Update("Coping Parameters");

                        Revit.Modify.CopySpatialElementParameters(document, Tool.TAS);

                        progressForm.Update("Finising Transaction");

                        transaction.Commit();
                    }
                }

                string path_SAM = System.IO.Path.Combine(outputDirectory, projectName + ".json");

                progressForm.Update("Saving SAM Analytical Model");

                Core.Convert.ToFile(analyticalModel, path_SAM);

                progressForm.Update("Printing Room Data Sheets");
                if (printRoomDataSheets && analyticalModel != null)
                {
                    if (!System.IO.Directory.Exists(outputDirectory))
                    {
                        System.IO.Directory.CreateDirectory(outputDirectory);
                    }

                    Analytical.UI.Modify.PrintRoomDataSheets(analyticalModel, outputDirectory);
                }
            }

            stopwatch.Stop();

            string hoursString = stopwatch.Elapsed.Hours.ToString();
            while (hoursString.Length < 2)
            {
                hoursString = "0" + hoursString;
            }

            string minutesString = stopwatch.Elapsed.Minutes.ToString();
            while (minutesString.Length < 2)
            {
                minutesString = "0" + minutesString;
            }

            string secondsString = stopwatch.Elapsed.Seconds.ToString();
            while (secondsString.Length < 2)
            {
                secondsString = "0" + secondsString;
            }

            MessageBox.Show(string.Format("Simulation finished.\nTime elapsed: {0}h {1}m {2}s", hoursString, minutesString, secondsString));

            return Result.Succeeded;
        }
    }
}
