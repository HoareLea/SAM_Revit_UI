// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace SAM.Analytical.Revit.UI
{
    public static partial class Query
    {
        public static List<string> ParameterNames(this object[,] objects, int index_Group, int index_Name, IEnumerable<string> unselectedParameterGroups = null)
        {
            List<dynamic> dynamics = new List<dynamic>();
            for (int i = 5; i <= objects.GetLength(0); i++)
            {
                string parameterGroup = objects[i, index_Group] as string;
                if (string.IsNullOrWhiteSpace(parameterGroup))
                {
                    continue;
                }

                string name = objects[i, index_Name] as string;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                dynamic @dynamic = new ExpandoObject();
                dynamic.Name = name;
                dynamic.Group = parameterGroup;
                dynamic.Checked = unselectedParameterGroups != null ? !unselectedParameterGroups.Contains(parameterGroup) : true;

                dynamics.Add(dynamic);
            }

            dynamics.Sort((x, y) => (x.Group + x.Name).CompareTo(y.Group + y.Name));

            // No CollapseAll equivalent is needed: a WPF TreeViewItem starts collapsed, which is what the
            // WinForms tree had to be told to do. No owner is set either - unlike the IExternalCommands,
            // this is a plain extension method with no ExternalCommandData to take a window handle from.
            Core.UI.WPF.MultipleSelectionTreeViewWindow treeViewWindow = new Core.UI.WPF.MultipleSelectionTreeViewWindow
            {
                Title = "Select Parameters",
                Width = 430,
                Height = 850
            };

            treeViewWindow.GettingText += (object sender, Core.UI.WPF.GettingTextEventArgs e) => e.Text = (e?.Object as dynamic)?.Name as string;
            treeViewWindow.GettingCategory += (object sender, Core.UI.WPF.GettingCategoryEventArgs e) =>
            {
                string category = (e?.Object as dynamic)?.Group as string;
                e.Category = string.IsNullOrEmpty(category) ? null : new Core.Category(category);
            };
            treeViewWindow.GettingChecked += (object sender, Core.UI.WPF.GettingCheckedEventArgs e) => e.Checked = (e?.Object as dynamic)?.Checked == true;
            treeViewWindow.SetObjects(dynamics);

            if (treeViewWindow.ShowDialog() != true)
            {
                return null;
            }

            return treeViewWindow.GetObjects<ExpandoObject>()?.ConvertAll(x => ((dynamic)x).Name as string);
        }
    }
}