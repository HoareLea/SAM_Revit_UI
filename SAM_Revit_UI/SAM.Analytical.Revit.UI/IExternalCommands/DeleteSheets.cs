// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DeleteSheets : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Project Setup";

        public override int Index => 15;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small);

        public override string Text => "Delete\nSheets";

        public override string ToolTip => "Delete Sheets";

        public override string AvailabilityClassName => null;

        public override Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document document = commandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return Result.Failed;
            }

            List<ViewSheet> viewSheets = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
            if (viewSheets == null || viewSheets.Count == 0)
            {
                return Result.Failed;
            }

            List<int> ids = new List<int>() { 725518, 725533, 802983, 805316, 835480, 1007139, 1008572 };

            Core.UI.WPF.MultipleSelectionTreeViewWindow treeViewWindow = new Core.UI.WPF.MultipleSelectionTreeViewWindow { Title = "Select Sheets" };
            treeViewWindow.GettingText += (object sender, Core.UI.WPF.GettingTextEventArgs e) =>
            {
                ViewSheet viewSheet_Temp = e?.Object as ViewSheet;
                e.Text = viewSheet_Temp == null ? null : string.Format("{0} - {1}", viewSheet_Temp.SheetNumber, viewSheet_Temp.Name);
            };
            treeViewWindow.GettingChecked += (object sender, Core.UI.WPF.GettingCheckedEventArgs e) =>
            {
                ViewSheet viewSheet_Temp = e?.Object as ViewSheet;
                e.Checked = viewSheet_Temp != null && !ids.Contains(System.Convert.ToInt32(viewSheet_Temp.Id.Value));
            };
            treeViewWindow.SetObjects(viewSheets);

            new System.Windows.Interop.WindowInteropHelper(treeViewWindow).Owner = commandData.Application.MainWindowHandle;

            if (treeViewWindow.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            viewSheets = treeViewWindow.GetObjects<ViewSheet>();

            if (viewSheets == null || viewSheets.Count == 0)
            {
                return Result.Failed;
            }

            using (Transaction transaction = new Transaction(document, "Delete Sheets"))
            {
                transaction.Start();

                document.Delete(viewSheets.ConvertAll(x => x.Id));

                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }
}
