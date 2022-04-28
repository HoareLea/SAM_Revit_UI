using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit;
using SAM.Core.Revit.UI;
using SAM.Core.Tas;
using SAM.Geometry.Spatial;
using SAM.Weather;
using System;
using System.Collections.Generic;
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
            }

            if (weatherData == null || geometryCalculationMethod == GeometryCalculationMethod.SAM)
            {
                return Result.Failed;
            }

            AnalyticalModel analyticalModel = null;

            string path_TBD = System.IO.Path.Combine(outputDirectory, projectName + ".tbd");

            bool simulate = false;

            Dictionary<Guid, ElementId> dictionary = new Dictionary<Guid, ElementId>();
            using (Core.Windows.SimpleProgressForm simpleProgressForm = new Core.Windows.SimpleProgressForm("Preparing Model", string.Empty, 6))
            {
                simpleProgressForm.Increment("Converting Model");
                switch(geometryCalculationMethod)
                {
                    case GeometryCalculationMethod.gbXML:
                        using (Transaction transaction = new Transaction(document, "Convert Model"))
                        {
                            transaction.Start();

                            analyticalModel = Convert.ToSAM_AnalyticalModel(document, new ConvertSettings(true, true, false));
                            List<Panel> panels_Temp = analyticalModel?.GetPanels();
                            if (panels_Temp != null)
                            {
                                foreach (Panel panel in panels_Temp)
                                {
                                    EnergyAnalysisSurface energyAnalysisSurface = Core.Revit.Query.Element<EnergyAnalysisSurface>(document, panel);
                                    HostObject hostObject = Core.Revit.Query.Element(document, energyAnalysisSurface?.CADObjectUniqueId, energyAnalysisSurface?.CADLinkUniqueId) as HostObject;
                                    if (hostObject != null)
                                    {
                                        dictionary[panel.Guid] = hostObject.Id;
                                    }

                                    List<Aperture> apertures = panel.Apertures;
                                    if (apertures != null)
                                    {
                                        foreach (Aperture aperture in apertures)
                                        {
                                            EnergyAnalysisOpening energyAnalysisOpening = Core.Revit.Query.Element<EnergyAnalysisOpening>(document, aperture);
                                            FamilyInstance familyInstance = Core.Revit.Query.Element(energyAnalysisOpening) as FamilyInstance;
                                            if (familyInstance != null)
                                            {
                                                dictionary[aperture.Guid] = familyInstance.Id;
                                            }
                                        }
                                    }
                                }
                            }

                            transaction.RollBack();
                        }
                        break;
                    case GeometryCalculationMethod.SAM:
                        using (Transaction transaction = new Transaction(document, "Convert Model"))
                        {
                            transaction.Start();

                            analyticalModel = Convert.ToSAM_AnalyticalModel(document, new ConvertSettings(true, true, false));

                            transaction.RollBack();
                        }

                        ConvertSettings convertSettings = new ConvertSettings(true, true, true);
                        IEnumerable<Panel> panels = Convert.ToSAM<Panel>(document, convertSettings);

                        List<Shell> shells = Analytical.Query.Shells(panels, 0.1, Core.Tolerance.MacroDistance);
                        if(shells == null || shells.Count == 0)
                        {
                            return Result.Failed;
                        }

                        IEnumerable<Space> spaces = Convert.ToSAM<Space>(document, convertSettings);

                        AdjacencyCluster adjacencyCluster_Temp = Analytical.Create.AdjacencyCluster(shells, spaces, panels, false, true, 0.01, Core.Tolerance.MacroDistance, 0.01, 0.0872664626, Core.Tolerance.MacroDistance, Core.Tolerance.Distance, Core.Tolerance.Angle);

                        analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster_Temp);
                        break;
                }


                if (analyticalModel == null)
                {
                    MessageBox.Show("Could not convert to AnalyticalModel");
                    return Result.Failed;
                }

                if (System.IO.File.Exists(path_TBD))
                {
                    System.IO.File.Delete(path_TBD);
                }

                List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

                //Run Solar Calculation for cooling load

                simpleProgressForm.Increment("Solar Calculations");
                if(solarCalculationMethod != SolarCalculationMethod.None)
                {
                    SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), Core.Tolerance.MacroDistance, Core.Tolerance.MacroDistance, 0.012, Core.Tolerance.Distance);
                }

                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    simpleProgressForm.Increment("Updating WeatherData");
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

                    simpleProgressForm.Increment("Converting to TBD");
                    Tas.Convert.ToTBD(analyticalModel, tBDDocument);

                    simpleProgressForm.Increment("Updating Zones");
                    Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                    simpleProgressForm.Increment("Updating Shading");
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

            if (adjacencyCluster != null && results != null && results.Count != 0)
            {
                using (Core.Windows.SimpleProgressForm simpleProgressForm = new Core.Windows.SimpleProgressForm("Inserting Results", string.Empty, results.Count + 3))
                {
                    simpleProgressForm.Increment("Initialization");

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
                            simpleProgressForm.Increment(string.IsNullOrWhiteSpace(space?.Name) ? "???" : space.Name);

                            ElementId elementId = space.ElementId();

                            if(elementId != null && elementId != ElementId.InvalidElementId)
                            {
                                if (space.TryGetValue(SpaceParameter.Occupancy, out double occupancy) && occupancy == 0)
                                {
                                    space.RemoveValue(SpaceParameter.Occupancy);
                                }

                                if(space.InternalCondition != null)
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
                            simpleProgressForm.Increment(sAMObject?.Name == null ? "???" : sAMObject.Name);

                            if (sAMObject is SpaceSimulationResult)
                            {
                                Convert.ToRevit(adjacencyCluster, (SpaceSimulationResult)sAMObject, document, convertSettings)?.Cast<Element>().ToList();
                            }
                            else if (sAMObject is ZoneSimulationResult)
                            {
                                Convert.ToRevit(adjacencyCluster, (ZoneSimulationResult)sAMObject, document, convertSettings)?.Cast<Element>().ToList();
                            }
                            else if (sAMObject is AdjacencyClusterSimulationResult)
                            {
                                Convert.ToRevit((AdjacencyClusterSimulationResult)sAMObject, document, convertSettings);
                            }
                            else if (sAMObject is Panel)
                            {
                                Panel panel = (Panel)sAMObject;

                                ElementId elementId = null;

                                if (dictionary.TryGetValue(panel.Guid, out elementId))
                                {
                                    Core.Revit.Modify.SetValues(document.GetElement(elementId), panel, ActiveSetting.Setting, parameters: convertSettings.GetParameters());
                                }

                                List<Aperture> apertures = panel.Apertures;
                                if (apertures != null)
                                {
                                    foreach (Aperture aperture in apertures)
                                    {
                                        if (dictionary.TryGetValue(aperture.Guid, out elementId))
                                        {
                                            Core.Revit.Modify.SetValues(document.GetElement(elementId), aperture, ActiveSetting.Setting);
                                        }
                                    }
                                }
                            }
                        }

                        simpleProgressForm.Increment("Coping Parameters");

                        Modify.CopySpatialElementParameters(document, Tool.TAS);

                        simpleProgressForm.Increment("Finishing");

                        transaction.Commit();
                    }
                }
            }

            return Result.Succeeded;
        }
    }
}
