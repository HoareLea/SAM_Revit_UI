using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit;
using SAM.Core.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ImportExportTags : ISAMRibbonItemData
    {
        public string RibbonPanelName => "Tools";

        public int Index => 7;

        public void Create(RibbonPanel ribbonPanel)
        {
            SplitButtonData splitButtonData = new SplitButtonData(Core.Query.FullTypeName(GetType()), "ImportExportAnalyticalModel");
            SplitButton splitButton = ribbonPanel.AddItem(splitButtonData) as SplitButton;

            PushButtonData pushButtonData_Import = new PushButtonData(Core.Query.FullTypeName(typeof(ImportTags)), "Import\nTags", GetType().Assembly.Location, typeof(ImportTags).FullName);
            pushButtonData_Import.LargeImage = Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small, 32, 32);
            pushButtonData_Import.ToolTip = "Import Tags";
            splitButton.AddPushButton(pushButtonData_Import);


            PushButtonData pushButtonData_Export = new PushButtonData(Core.Query.FullTypeName(typeof(ExportTags)), "Export\nTags", GetType().Assembly.Location, typeof(ExportTags).FullName);
            pushButtonData_Export.LargeImage = Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small, 32, 32);
            pushButtonData_Export.ToolTip = "Export Tags";
            splitButton.AddPushButton(pushButtonData_Export);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ImportTags : IExternalCommand
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

            using (Transaction transaction = new Transaction(document, "Import Tags"))
            {
                transaction.Start();

                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ExportTags : IExternalCommand
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
                    saveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(path_Document) + "_Tags" +".json";
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

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<Element> elements = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_MEPSpaceTags).ToList();
            elements.AddRange(new FilteredElementCollector(document).OfClass(typeof(IndependentTag)).ToList());

            ConvertSettings convertSettings = new ConvertSettings(true, true, true);

            List<Geometry.Revit.Tag> tags = new List<Geometry.Revit.Tag>();
            foreach(Element element in elements)
            {
                Geometry.Revit.Tag tag = null;
                if (element is Autodesk.Revit.DB.Mechanical.SpaceTag)
                {
                    tag = Geometry.Revit.Convert.ToSAM((Autodesk.Revit.DB.Mechanical.SpaceTag)element, convertSettings);
                }
                else if (element is IndependentTag)
                {
                    tag = Geometry.Revit.Convert.ToSAM((IndependentTag)element, convertSettings);
                }

                if (tag == null)
                {
                    continue;
                }

                tags.Add(tag);
            }

            Core.Convert.ToFile(tags, path);

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

            MessageBox.Show(string.Format("Tags Exported.\nTime elapsed: {0}h {1}m {2}s", hoursString, minutesString, secondsString));

            return Result.Succeeded;
        }
    }
}
