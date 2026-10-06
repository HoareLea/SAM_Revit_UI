// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using Autodesk.Revit.Attributes;
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

        public override Result Execute(ExternalCommandData ExternalCommandData, ref string message, ElementSet elements)
        {
            Document document = ExternalCommandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return Result.Failed;
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

            List<Element> elements_Temp = new FilteredElementCollector(document).WherePasses(logicalOrFilter).WhereElementIsNotElementType().ToList();

            // No CollapseAll equivalent is needed: a WPF TreeViewItem starts collapsed, which is what the
            // WinForms tree had to be told to do.
            Core.UI.WPF.MultipleSelectionTreeViewWindow treeViewWindow = new Core.UI.WPF.MultipleSelectionTreeViewWindow { Title = "Select Elements" };
            treeViewWindow.GettingText += (object sender, Core.UI.WPF.GettingTextEventArgs e) =>
            {
                Element element = e?.Object as Element;
                e.Text = element == null ? null : string.Format("{0} [{1}]", element.Name, element.Id.Value);
            };
            treeViewWindow.GettingCategory += (object sender, Core.UI.WPF.GettingCategoryEventArgs e) =>
            {
                string category = (e?.Object as Element)?.Category?.Name;
                e.Category = string.IsNullOrEmpty(category) ? null : new Core.Category(category);
            };
            treeViewWindow.GettingChecked += (object sender, Core.UI.WPF.GettingCheckedEventArgs e) =>
            {
                Element element = e?.Object as Element;
                e.Checked = element != null && element.Id.Value != 311;
            };
            treeViewWindow.SetObjects(elements_Temp);

            new System.Windows.Interop.WindowInteropHelper(treeViewWindow).Owner = ExternalCommandData.Application.MainWindowHandle;

            if (treeViewWindow.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            elements_Temp = treeViewWindow.GetObjects<Element>();
            using (Transaction transaction = new Transaction(document, "Clean"))
            {
                transaction.Start();

                List<ElementId> elementIds = new List<ElementId>();
                foreach (Element element in elements_Temp)
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

            return Result.Succeeded;
        }
    }
}
