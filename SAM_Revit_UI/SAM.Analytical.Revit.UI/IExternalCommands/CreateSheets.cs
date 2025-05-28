using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CreateSheets : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Project Setup";

        public override int Index => 11;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small);

        public override string Text => "Create\nSheets";

        public override string ToolTip => "Create Sheets";

        public override string AvailabilityClassName => null;

        public override void Execute()
        {
            Document document = Document;
            if (document == null)
            {
                return;
            }

            List<ViewSheet> viewSheets = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();

            ViewSheet viewSheet = null;
#if Revit2017 || Revit2018 || Revit2019 || Revit2020 || Revit2021 || Revit2022 || Revit2023 || Revit2024
            using (Core.Windows.Forms.ComboBoxForm<ViewSheet> comboBoxForm = new Core.Windows.Forms.ComboBoxForm<ViewSheet>("Reference View Sheet", viewSheets, (ViewSheet x) => string.Format("{0} - {1}", x.SheetNumber, x.Name), viewSheets.Find(x => x.Id.IntegerValue == 725533)) )
#else
            using (Core.Windows.Forms.ComboBoxForm<ViewSheet> comboBoxForm = new Core.Windows.Forms.ComboBoxForm<ViewSheet>("Reference View Sheet", viewSheets, (ViewSheet x) => string.Format("{0} - {1}", x.SheetNumber, x.Name), viewSheets.Find(x => x.Id.Value == 725533)))
#endif
            {
                if (comboBoxForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                viewSheet = comboBoxForm.SelectedItem;
            }

            if (viewSheet == null)
            {
                return;
            }

            List<ViewPlan> viewPlans = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().ToList();
            viewPlans?.RemoveAll(x => !x.IsTemplate);
            if (viewPlans == null || viewPlans.Count == 0)
            {
                MessageBox.Show("Could not find Template View Plans");
                return;
            }

            List<string> templateNames = null;
            using (Core.Windows.Forms.TreeViewForm<ViewPlan> treeViewForm = new Core.Windows.Forms.TreeViewForm<ViewPlan>("Select Templates", viewPlans, (ViewPlan x) => x.Name, null, (ViewPlan x) => x.Name == "Cooling Load" || x.Name == "Heating Load"))
            {
                if (treeViewForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                templateNames = treeViewForm.SelectedItems?.ConvertAll(x => x.Name);
            }

            using (Transaction transaction = new Transaction(document, "Create Sheets"))
            {
                transaction.Start();
                List<ViewSheet> result = Core.Revit.Create.Sheets(viewSheet, templateNames, true);
                transaction.Commit();
            }
        }
    }
}
