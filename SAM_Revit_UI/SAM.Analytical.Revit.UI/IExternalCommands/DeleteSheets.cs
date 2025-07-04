﻿using Autodesk.Revit.Attributes;
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

        public override void Execute()
        {
            Document document = ExternalCommandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return;
            }

            List<ViewSheet> viewSheets = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList();
            if (viewSheets == null || viewSheets.Count == 0)
            {
                return;
            }

            List<int> ids = new List<int>() { 725518, 725533, 802983, 805316, 835480, 1007139, 1008572 };

#if Revit2017 || Revit2018 || Revit2019 || Revit2020 || Revit2021 || Revit2022 || Revit2023 || Revit2024
            using (Core.Windows.Forms.TreeViewForm<ViewSheet> treeViewForm = new Core.Windows.Forms.TreeViewForm<ViewSheet>("Select Sheets", viewSheets, (ViewSheet x) => string.Format("{0} - {1}", x.SheetNumber, x.Name), null, (ViewSheet x) => !ids.Contains(x.Id.IntegerValue)))
#else
            using (Core.Windows.Forms.TreeViewForm<ViewSheet> treeViewForm = new Core.Windows.Forms.TreeViewForm<ViewSheet>("Select Sheets", viewSheets, (ViewSheet x) => string.Format("{0} - {1}", x.SheetNumber, x.Name), null, (ViewSheet x) => !ids.Contains(System.Convert.ToInt32(x.Id.Value))))
#endif
            {
                if (treeViewForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                viewSheets = treeViewForm.SelectedItems;
            }

            if (viewSheets == null || viewSheets.Count == 0)
            {
                return;
            }

            using (Transaction transaction = new Transaction(document, "Delete Sheets"))
            {
                transaction.Start();

                document.Delete(viewSheets.ConvertAll(x => x.Id));

                transaction.Commit();
            }
        }
    }
}
