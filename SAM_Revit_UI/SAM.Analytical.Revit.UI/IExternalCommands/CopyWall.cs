using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CopyWall : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Tools";

        public override int Index => 19;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_CopyWall, 32, 32);

        public override string Text => "Copy\nWall";

        public override string ToolTip => "Copy Wall from Linked Model";

        public override string AvailabilityClassName => null;

        public override void Execute()
        {
            Revit.Modify.CopyWall(ExternalCommandData?.Application?.ActiveUIDocument);
        }
    }
}
