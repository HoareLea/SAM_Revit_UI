using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CopyTags : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Tools";

        public override int Index => 20;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_CopyWall, 32, 32);

        public override string Text => "Copy\nTags";

        public override string ToolTip => "Copy Tags from one set of views to another";

        public override string AvailabilityClassName => null;

        public override Result Execute(ExternalCommandData externalCommandData, ref string message, ElementSet elementSet)
        {
            Modify.CopyTags(externalCommandData?.Application?.ActiveUIDocument);

            return Result.Succeeded;
        }
    }
}
