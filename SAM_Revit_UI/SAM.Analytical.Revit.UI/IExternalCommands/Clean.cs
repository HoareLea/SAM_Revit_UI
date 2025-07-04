﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Clean : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Project Setup";

        public override int Index => 8;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small);

        public override string Text => "Clean";

        public override string ToolTip => "Clean";

        public override string AvailabilityClassName => null;


        public override void Execute()
        {
            Document document = ExternalCommandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return;
            }

            List<BuiltInCategory> builtInCategories = new List<BuiltInCategory>()
            {
                BuiltInCategory.OST_MEPSpaces,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Lines,
                BuiltInCategory.OST_GenericModel,
                BuiltInCategory.OST_Levels,
                BuiltInCategory.OST_CLines,
                BuiltInCategory.OST_MEPSpaceTags,
                BuiltInCategory.OST_WallTags,
                BuiltInCategory.OST_WindowTags,
                BuiltInCategory.OST_DoorTags

            };

            LogicalOrFilter logicalOrFilter = new LogicalOrFilter(builtInCategories.ConvertAll(x => new ElementCategoryFilter(x) as ElementFilter));

            List<Element> elements = new FilteredElementCollector(document).WherePasses(logicalOrFilter).WhereElementIsNotElementType().ToList();

#if Revit2017 || Revit2018 || Revit2019 || Revit2020 || Revit2021 || Revit2022 || Revit2023 || Revit2024
            using (Core.Windows.Forms.TreeViewForm<Element> treeViewForm = new Core.Windows.Forms.TreeViewForm<Element>("Select Elements", elements, (Element x) => string.Format("{0} [{1}]", x.Name, x.Id.IntegerValue), (Element x) => x.Category.Name, (Element x) =>x.Id.IntegerValue != 311))
#else
            using (Core.Windows.Forms.TreeViewForm<Element> treeViewForm = new Core.Windows.Forms.TreeViewForm<Element>("Select Elements", elements, (Element x) => string.Format("{0} [{1}]", x.Name, x.Id.Value), (Element x) => x.Category.Name, (Element x) => x.Id.Value != 311))
#endif
            {
                treeViewForm.CollapseAll();
                if (treeViewForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                elements = treeViewForm.SelectedItems;
            }
            using (Transaction transaction = new Transaction(document, "Clean"))
            {
                transaction.Start();

                List<ElementId> elementIds = new List<ElementId>();
                foreach (Element element in elements)
                {
                    if (element == null || !element.IsValidObject)
                    {
                        continue;
                    }

                    try
                    {
                        document.Delete(element.Id);
                    }
                    catch (Exception exception)
                    {
                        elementIds.Add(element.Id);
                    }
                }

                transaction.Commit();
            }
        }
    }
}
