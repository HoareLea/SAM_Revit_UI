using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Hydra : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "General";

        public override int Index => 2;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Hydra, 32, 23);

        public override string Text => "Hydra";

        public override string ToolTip => "Hydra webpage";

        public override string AvailabilityClassName => typeof(AlwaysAvailableExternalCommandAvailability).FullName;

        public override void Execute()
        {
            Core.Query.StartProcess(Core.Link.Hydra);
        }
    }
}
