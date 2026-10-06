// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Analytical.UI;
using SAM.Core.Revit;
using SAM.Core.Revit.UI;
using SAM.Core.Tas;
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

        public override Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document document = commandData?.Application?.ActiveUIDocument?.Document;
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

            Forms.SimulateWindow simulateWindow = new Forms.SimulateWindow(System.IO.Path.GetFileNameWithoutExtension(path), System.IO.Path.GetDirectoryName(path));
            {
                Parameter parameter = document.ProjectInformation.LookupParameter("SAM_WeatherFile");
                simulateWindow.WeatherData = Core.Convert.ToSAM<WeatherData>(parameter?.AsString())?.FirstOrDefault();

                new System.Windows.Interop.WindowInteropHelper(simulateWindow).Owner = commandData.Application.MainWindowHandle;

                if (simulateWindow.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }

                projectName = simulateWindow.ProjectName;
                outputDirectory = simulateWindow.OutputDirectory;
                unmetHours = simulateWindow.UnmetHours;
                // SelectedWeatherData, not WeatherData - see the note on those properties. This is the same
                // pairing SAM.Analytical.UI.WPF.Modify.Simulate uses: seed with one, read back the other.
                weatherData = simulateWindow.SelectedWeatherData;
                solarCalculationMethod = simulateWindow.SolarCalculationMethod;
                geometryCalculationMethod = simulateWindow.GeometryCalculationMethod;
                updateConstructionLayersByPanelType = simulateWindow.UpdateConstructionLayersByPanelType;
                printRoomDataSheets = simulateWindow.RoomDataSheets;
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
            bool cancelled_Preparation = false;

            // The dialog runs on its own UI thread (ProgressWindowHost) because "Converting to TBD" and
            // "Updating Shading" block this one for minutes, and Windows discards clicks on a window whose
            // thread has stopped pumping - a Cancel button on Revit's own thread would lose the click and the
            // run would carry on regardless. The work itself stays here, so no TAS COM object changes apartment
            // and no Revit API call leaves the API thread. Cancellation is observed only BETWEEN COM calls; an
            // in-flight TAS COM call is never interrupted.
            using (System.Threading.CancellationTokenSource cancellationTokenSource = new System.Threading.CancellationTokenSource())
            {
                // The dialog is deliberately not a using: it has to be torn down BEFORE the final cancellation
                // check below, and a using would dispose it after. Its Cancel button lives on the dialog's own
                // thread, so a click can land at any instant - checking and then disposing would only move the
                // race. Dispose closes the window and joins its thread, so once it returns no further
                // CancelRequested can arrive and any in-flight one has already run. Same ordering as
                // Modify.RunWorkflow.
                //
                // No owner is set, unlike every other window this command opens over Revit. A WPF owner must
                // live on the same thread as the window it owns, and this one deliberately does not - that is
                // the whole point of the host. ProgressWindowHost sets Topmost instead, which is what keeps it
                // in front of a Revit window that has stopped painting.
                Core.Windows.WPF.ProgressWindowHost progressWindowHost = new Core.Windows.WPF.ProgressWindowHost("Preparing Model", 6, true, Analytical.Tas.Query.CancelNote(null));

                try
                {
                    progressWindowHost.CancelRequested += (s, e) => cancellationTokenSource.Cancel();

                    progressWindowHost.Update("Converting Model");
                    analyticalModel = Convert.ToSAM(document, geometryCalculationMethod, out dictionary);

                    if (analyticalModel == null)
                    {
                        MessageBox.Show("Could not convert to AnalyticalModel");
                        return Result.Cancelled;
                    }

                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

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

                    // Immediately before the delete, not merely at the next stage boundary: cancelling while the
                    // materials loop or UpdateConstructionLayersByPanelType was running would otherwise still
                    // erase the user's existing .tbd on the way out, then report the run as cancelled.
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    if (System.IO.File.Exists(path_TBD))
                    {
                        System.IO.File.Delete(path_TBD);
                    }

                    List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

                    //Run Solar Calculation for cooling load

                    progressWindowHost.Update("Solar Calculations");
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    if (solarCalculationMethod != SolarCalculationMethod.None)
                    {
                        SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), false, Core.Tolerance.MacroDistance, Core.Tolerance.MacroDistance, 0.012, Core.Tolerance.Distance);
                    }

                    using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                    {
                        TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                        progressWindowHost.Update("Updating WeatherData");
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
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

                        progressWindowHost.Note = Analytical.Tas.Query.CancelNote("Converting to TBD");
                        progressWindowHost.Update("Converting to TBD");
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        Tas.Convert.ToTBD(analyticalModel, tBDDocument);
                        progressWindowHost.Note = Analytical.Tas.Query.CancelNote(null);

                        progressWindowHost.Update("Updating Zones");
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                        progressWindowHost.Note = Analytical.Tas.Query.CancelNote("Updating Shading");
                        progressWindowHost.Update("Updating Shading");
                        cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        simulate = Tas.Modify.UpdateShading(tBDDocument, analyticalModel);
                        progressWindowHost.Note = Analytical.Tas.Query.CancelNote(null);

                        sAMTBDDocument.Save();
                    }

                }
                catch (OperationCanceledException)
                {
                    // Reported after the block so the dialog is gone before the message box appears.
                    cancelled_Preparation = true;
                }
                finally
                {
                    progressWindowHost.Dispose();
                }

                // Past this point no cancel can be raised, so this observation is final - and it is the only one
                // that covers the last stage. Checking on the way INTO each stage leaves "Updating Shading"
                // uncovered, which is the longest stage here and the one the note tells the user to expect to
                // wait through: a click there was recorded by the dialog and never observed, so the workflow
                // started anyway. The document has already been saved by now, so nothing is left half-written.
                //
                // "Final" holds only once the host confirms it shut down cleanly. If it could not - the dialog
                // thread was not joined, or a handler did not quiesce - that thread is still live and a click it
                // has queued may never have been observed, so success cannot be claimed and the safe direction
                // is to report the preparation as cancelled.
                if (!cancelled_Preparation && (cancellationTokenSource.IsCancellationRequested || !progressWindowHost.ShutdownCompleted))
                {
                    cancelled_Preparation = true;
                }
            }

            if (cancelled_Preparation)
            {
                MessageBox.Show("Cancelled while preparing the model. A partially written .tbd may remain in the output directory.");
                return Result.Cancelled;
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

            Tas.WorkflowSettings workflowSettings = new Tas.WorkflowSettings()
            {
                Path_TBD = path_TBD,
                Path_gbXML = null,
                WeatherData = null,
                DesignDays_Heating = heatingDesignDays,
                DesignDays_Cooling = coolingDesignDays,
                SurfaceOutputSpecs = surfaceOutputSpecs,
                UnmetHours = unmetHours,
                Simulate = simulate,
                Sizing = false,
                UpdateZones = true,
                UseWidths = false,
                AddIZAMs = false,
                SimulateFrom = 1,
                SimulateTo = 1
            };

            // Was Analytical.UI.WPF.Modify.RunWorkflow, which runs the workflow with no progress dialog and no
            // cancellation at all - a silent multi-minute freeze. This overload reports every stage and carries
            // a Cancel button; see Modify.RunWorkflow for why it is not shared with the Grasshopper twin.
            analyticalModel = Modify.RunWorkflow(analyticalModel, workflowSettings, System.Threading.CancellationToken.None, out bool cancelled_Workflow);

            if (cancelled_Workflow)
            {
                MessageBox.Show("Workflow cancelled. Partially written .tbd/.tsd files may remain in the output directory.");
                return Result.Cancelled;
            }

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

            // Set by the loops below when the user clicks Cancel, and read all the way out to the return: a
            // cancelled insert rolls the Revit transaction back and skips everything that follows it.
            bool cancelled_Insert = false;

            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Inserting Results", results.Count + 5))
            {
                // Unlike the preparation dialog above, this one does not need a host thread of its own: both loops
                // below step once per result and Update pumps the message queue every step, so the form never
                // goes long enough without pumping for Windows to ghost it and the click is always seen.
                progressForm.Cancellable = true;
                progressForm.Note = "Cancel stops after the current result - nothing is written into the model.";

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

                            // Checked after Update, because Update is what pumps the queue and so what turns a
                            // click made during the previous result into a set flag.
                            if (progressForm.CancellationRequested)
                            {
                                cancelled_Insert = true;
                                break;
                            }

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
                            // Carries a cancel out of the space loop above without re-indenting this one.
                            if (cancelled_Insert)
                            {
                                break;
                            }

                            progressForm.Update(sAMObject?.Name == null ? "???" : sAMObject.Name);

                            if (progressForm.CancellationRequested)
                            {
                                cancelled_Insert = true;
                                break;
                            }

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

                        // Every SetValues above happened inside this transaction, so rolling back is what
                        // makes the Note true: the model is left exactly as it was, however far the loops got.
                        if (cancelled_Insert)
                        {
                            transaction.RollBack();
                        }
                        else
                        {
                            progressForm.Update("Coping Parameters");

                            Revit.Modify.CopySpatialElementParameters(document, Tool.TAS);

                            progressForm.Update("Finising Transaction");

                            transaction.Commit();
                        }
                    }
                }

                // Saving the JSON and printing the room data sheets are the remaining work of this command,
                // so a cancel skips them too. The simulation itself already ran - its .tbd/.tsd are on disk
                // either way - and the message at the end says so rather than claiming the run succeeded.
                if (!cancelled_Insert)
                {
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
            }

            stopwatch.Stop();

            // Was hand-padded from Elapsed.Hours, which is the hours *component* of the TimeSpan and so wraps
            // back to 00 after a day - a long enough run reported the wrong time. Query.Duration promotes past
            // the hour properly and is the same formatter the progress dialog uses, so the two agree.
            if (cancelled_Insert)
            {
                MessageBox.Show(string.Format("Results were not written into the model - cancelled.\nThe simulation itself finished and its output is in {0}.\nTime elapsed: {1}", outputDirectory, Core.Windows.Query.Duration(stopwatch.Elapsed)));

                return Result.Cancelled;
            }

            MessageBox.Show(string.Format("Simulation finished.\nTime elapsed: {0}", Core.Windows.Query.Duration(stopwatch.Elapsed)));

            return Result.Succeeded;
        }
    }
}
