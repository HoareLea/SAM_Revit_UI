// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;
using SAM.Geometry.Revit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ImportExportAnalyticalModel : ISAMRibbonItemData
    {
        public string RibbonPanelName => "Analytical";

        public int Index => 7;

        public void Create(RibbonPanel ribbonPanel)
        {
            SplitButtonData splitButtonData = new SplitButtonData(Core.Query.FullTypeName(GetType()), "ImportExportAnalyticalModel");
            SplitButton splitButton = ribbonPanel.AddItem(splitButtonData) as SplitButton;

            PushButtonData pushButtonData_Import = new PushButtonData(Core.Query.FullTypeName(typeof(ImportAnalyticalModel)), "Import\nModel", GetType().Assembly.Location, typeof(ImportAnalyticalModel).FullName);
            pushButtonData_Import.LargeImage = Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small, 32, 32);
            pushButtonData_Import.ToolTip = "Import SAM AnalyticalModel";
            splitButton.AddPushButton(pushButtonData_Import);


            PushButtonData pushButtonData_Export = new PushButtonData(Core.Query.FullTypeName(typeof(ExportAnalyticalModel)), "Export\nModel", GetType().Assembly.Location, typeof(ExportAnalyticalModel).FullName);
            pushButtonData_Export.LargeImage = Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small, 32, 32);
            pushButtonData_Export.ToolTip = "Export SAM AnalyticalModel";
            splitButton.AddPushButton(pushButtonData_Export);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ImportAnalyticalModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData externalCommandData, ref string message, ElementSet elementSet)
        {
            Document document = externalCommandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return Result.Failed;
            }

            string path = null;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                //openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog(ExternalApplication.WindowHandle) != DialogResult.OK)
                {
                    return Result.Cancelled;
                }

                path = openFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return Result.Failed;
            }

            AnalyticalModel analyticalModel = Core.Convert.ToSAM<AnalyticalModel>(path)?.FirstOrDefault();
            if (analyticalModel == null)
            {
                return Result.Failed;
            }

            List<Architectural.Level> levels = null;

            List<Panel> panels = analyticalModel.GetPanels();
            if (panels != null)
            {
                levels = Architectural.Create.Levels(panels);
            }

            Core.Revit.ConvertSettings convertSettings = new Core.Revit.ConvertSettings(true, true, true);
            convertSettings.AddParameter("AnalyticalModel", analyticalModel);

            using (Transaction transaction = new Transaction(document, "Load Analytical Model"))
            {
                transaction.Start();

                List<Element> elements = new List<Element>();

                using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Load Analytical Model", 5))
                {
                    progressForm.Update("Creating Levels");

                    foreach (Architectural.Level level in levels)
                    {
                        Level level_Revit = Architectural.Revit.Convert.ToRevit(level, document, convertSettings);
                        if (level_Revit != null)
                        {
                            elements.Add(level_Revit);
                        }
                    }

                    progressForm.Update("Creating Model");

                    Revit.Modify.UpdatePanelTypes(document, panels);
                    panels?.ForEach(x => analyticalModel.AddPanel(x));

                    List<Element> elements_AnalyticalModel = Revit.Convert.ToRevit(analyticalModel, document, convertSettings);
                    if (elements_AnalyticalModel != null)
                    {
                        elements.AddRange(elements_AnalyticalModel);
                    }

                    progressForm.Update("Importing Tags");
                    List<Tag> tags = analyticalModel.AdjacencyCluster.GetObjects<Tag>();
                    if (tags != null && tags.Count != 0)
                    {
                        // ImportTags shows a cancellable dialog of its own. It places tags inside this
                        // transaction and cannot undo them, so rolling back here covers the whole load -
                        // levels and model included - rather than leaving a model with no tags on it.
                        Modify.ImportTags(document, tags, out bool cancelled);
                        if (cancelled)
                        {
                            transaction.RollBack();

                            return Result.Cancelled;
                        }
                    }

                    progressForm.Update("Coping Parameters");
                    Revit.Modify.CopySpatialElementParameters(document, Tool.TAS);

                    progressForm.Update("Finishing");
                }

                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ExportAnalyticalModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData externalCommandData, ref string message, ElementSet elementSet)
        {
            Document document = externalCommandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return Result.Failed;
            }

            string path = null;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                string path_Document = document.PathName;
                if(!string.IsNullOrWhiteSpace(path_Document))
                {
                    saveFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(path_Document);
                    saveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(path_Document) + ".json";
                }

                if (saveFileDialog.ShowDialog(ExternalApplication.WindowHandle) != DialogResult.OK)
                {
                    return Result.Cancelled;
                }

                path = saveFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return Result.Failed;
            }

            GeometryCalculationMethod geometryCalculationMethod = GeometryCalculationMethod.Undefined;

            // The single-argument constructor populates the drop-down from the enum values, using
            // Core.Query.Description for each - same as the WinForms ComboBoxForm it replaces.
            Core.UI.WPF.ComboBoxWindow<GeometryCalculationMethod> comboBoxWindow = new Core.UI.WPF.ComboBoxWindow<GeometryCalculationMethod>("Geometry Calculation Method");
            comboBoxWindow.SelectedItem = geometryCalculationMethod;

            new System.Windows.Interop.WindowInteropHelper(comboBoxWindow).Owner = externalCommandData.Application.MainWindowHandle;

            if (comboBoxWindow.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            geometryCalculationMethod = comboBoxWindow.SelectedItem;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            AnalyticalModel analyticalModel = Convert.ToSAM(document, geometryCalculationMethod, out Dictionary<Guid, ElementId> dictionary);
            if(analyticalModel == null)
            {
                return Result.Failed;
            }

            Core.Convert.ToSAM<AnalyticalModel>(path)?.FirstOrDefault();
            if (analyticalModel == null)
            {
                return Result.Failed;
            }

            Core.Convert.ToFile(analyticalModel, path);

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

            MessageBox.Show(string.Format("Model Exported.\nTime elapsed: {0}h {1}m {2}s", hoursString, minutesString, secondsString));

            return Result.Succeeded;
        }
    }
}
