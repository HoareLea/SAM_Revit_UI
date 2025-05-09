using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class OpenViewers : ISAMRibbonItemData
    {
        public string RibbonPanelName => "Viewers";

        public int Index => 18;

        public void Create(RibbonPanel ribbonPanel)
        {
            SplitButtonData splitButtonData = new SplitButtonData(Core.Query.FullTypeName(GetType()), "OpenViewers");
            SplitButton splitButton = ribbonPanel.AddItem(splitButtonData) as SplitButton;

            PushButtonData pushButtonData_T3D = new PushButtonData(Core.Query.FullTypeName(typeof(OpenGbXMLViewer)), "gbXML\nViewer", GetType().Assembly.Location, typeof(OpenGbXMLViewer).FullName);
            pushButtonData_T3D.LargeImage = Core.Windows.Convert.ToBitmapSource(Resources.SAM_gbXMLViewer, 32, 32);
            pushButtonData_T3D.ToolTip = "LadybugTools Spider gbXMLViewer";
            pushButtonData_T3D.AvailabilityClassName = typeof(AlwaysAvailableExternalCommandAvailability).FullName;
            splitButton.AddPushButton(pushButtonData_T3D);

            PushButtonData pushButtonData_SAM = new PushButtonData(Core.Query.FullTypeName(typeof(OpenSAMViewer)), "SAM\nViewer", GetType().Assembly.Location, typeof(OpenSAMViewer).FullName);
            pushButtonData_SAM.LargeImage = Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small, 32, 32);
            pushButtonData_SAM.ToolTip = "SAM Viewer";
            pushButtonData_SAM.AvailabilityClassName = typeof(AlwaysAvailableExternalCommandAvailability).FullName;
            splitButton.AddPushButton(pushButtonData_SAM);

            PushButtonData pushButtonData_TBD = new PushButtonData(Core.Query.FullTypeName(typeof(OpenRadViewer)), "Rad\nViewer", GetType().Assembly.Location, typeof(OpenRadViewer).FullName);
            pushButtonData_TBD.LargeImage = Core.Windows.Convert.ToBitmapSource(Resources.SAM_RadViewer, 32, 32);
            pushButtonData_TBD.ToolTip = "LadybugTools Spider radViewer WIP";
            pushButtonData_TBD.AvailabilityClassName = typeof(AlwaysAvailableExternalCommandAvailability).FullName;
            splitButton.AddPushButton(pushButtonData_TBD);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class OpenRadViewer : IExternalCommand
    {
        public Result Execute(ExternalCommandData ExternalCommandData, ref string Message, ElementSet Elements)
        {
            Core.Revit.Query.StartProcess("http://www.ladybug.tools/spider-rad-viewer/rad-viewer");

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class OpenSAMViewer : IExternalCommand
    {
        public Result Execute(ExternalCommandData ExternalCommandData, ref string Message, ElementSet Elements)
        {
            Core.Revit.Query.StartProcess("https://hoarelea.github.io/sam-viewer/sam-viewer/v-2020-05-29/sam-viewer.html");

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class OpenGbXMLViewer : IExternalCommand
    {
        public Result Execute(ExternalCommandData ExternalCommandData, ref string Message, ElementSet Elements)
        {
            Core.Revit.Query.StartProcess("http://www.ladybug.tools/spider/gbxml-viewer");

            return Result.Succeeded;
        }
    }
}
