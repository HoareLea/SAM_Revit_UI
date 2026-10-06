// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors
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

        public override Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document document = commandData.Application.ActiveUIDocument.Document;
            if (document == null)
            {
                return Result.Failed;
            }

            List<ViewSheet> viewSheets = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();

            ViewSheet viewSheet = null;
            Core.UI.WPF.ComboBoxWindow<ViewSheet> comboBoxWindow = new Core.UI.WPF.ComboBoxWindow<ViewSheet>("Reference View Sheet", viewSheets, (ViewSheet x) => string.Format("{0} - {1}", x.SheetNumber, x.Name), viewSheets.Find(x => x.Id.Value == 725533));

            new System.Windows.Interop.WindowInteropHelper(comboBoxWindow).Owner = commandData.Application.MainWindowHandle;

            if (comboBoxWindow.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            viewSheet = comboBoxWindow.SelectedItem;

            if (viewSheet == null)
            {
                return Result.Failed;
            }

            List<ViewPlan> viewPlans = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().ToList();
            viewPlans?.RemoveAll(x => !x.IsTemplate);
            if (viewPlans == null || viewPlans.Count == 0)
            {
                MessageBox.Show("Could not find Template View Plans");
                return Result.Cancelled;
            }

            List<string> templateNames = null;

            Core.UI.WPF.MultipleSelectionTreeViewWindow treeViewWindow = new Core.UI.WPF.MultipleSelectionTreeViewWindow { Title = "Select Templates" };
            treeViewWindow.GettingText += (object sender, Core.UI.WPF.GettingTextEventArgs e) => e.Text = (e?.Object as ViewPlan)?.Name;
            treeViewWindow.GettingChecked += (object sender, Core.UI.WPF.GettingCheckedEventArgs e) =>
            {
                string name = (e?.Object as ViewPlan)?.Name;
                e.Checked = name == "Cooling Load" || name == "Heating Load";
            };
            treeViewWindow.SetObjects(viewPlans);

            new System.Windows.Interop.WindowInteropHelper(treeViewWindow).Owner = commandData.Application.MainWindowHandle;

            if (treeViewWindow.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            templateNames = treeViewWindow.GetObjects<ViewPlan>()?.ConvertAll(x => x.Name);

            using (Transaction transaction = new Transaction(document, "Create Sheets"))
            {
                transaction.Start();
                List<ViewSheet> result = Core.Revit.Create.Sheets(viewSheet, templateNames, true);
                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }
}
