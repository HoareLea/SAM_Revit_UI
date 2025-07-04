﻿using Autodesk.Revit.Attributes;
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
    public class CreateFloorPlan : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Project Setup";

        public override int Index => 9;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small);

        public override string Text => "Create\nFloor Plans";

        public override string ToolTip => "Create Floor Plans";

        public override string AvailabilityClassName => null;

        public override void Execute()
        {
            Document document = Document;
            if (document == null)
            {
                return;
            }

            List<ViewPlan> viewPlans = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().ToList();
            if (viewPlans == null || viewPlans.Count == 0)
            {
                return;
            }

            ViewPlan viewPlan = null;
#if Revit2017 || Revit2018 || Revit2019 || Revit2020 || Revit2021 || Revit2022 || Revit2023 || Revit2024
            using (Core.Windows.Forms.ComboBoxForm<ViewPlan> comboBoxForm = new Core.Windows.Forms.ComboBoxForm<ViewPlan>("Select ViewPlan", viewPlans, (ViewPlan x) => x.Name, viewPlans.Find(x => x.Id.IntegerValue == 312)))
#else
            using (Core.Windows.Forms.ComboBoxForm<ViewPlan> comboBoxForm = new Core.Windows.Forms.ComboBoxForm<ViewPlan>("Select ViewPlan", viewPlans, (ViewPlan x) => x.Name, viewPlans.Find(x => x.Id.Value == 312)))
#endif
            {
                if (comboBoxForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                viewPlan = comboBoxForm.SelectedItem;
            }

            if (viewPlan == null)
            {
                MessageBox.Show("Could not find view to be duplicated");
                return;
            }

            List<Level> levels = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().ToList();
            if (levels == null || levels.Count == 0)
            {
                MessageBox.Show("Could not find levels");
                return;
            }

            using (Transaction transaction = new Transaction(document, "Create Views"))
            {
                transaction.Start();
                List<ViewPlan> result = Core.Revit.Modify.DuplicateViewPlan(viewPlan, levels, true);
                transaction.Commit();
            }
        }
    }
}
