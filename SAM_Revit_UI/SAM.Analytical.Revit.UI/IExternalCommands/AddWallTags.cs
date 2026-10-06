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
    public class AddWallTags : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Project Setup";

        public override int Index => 13;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small);

        public override string Text => "Add\nWall Tags";

        public override string ToolTip => "Add Wall Tags";

        public override string AvailabilityClassName => null;

        public override Result Execute(ExternalCommandData externalCommandData, ref string message, ElementSet elements)
        {
            Document document = externalCommandData.Application.ActiveUIDocument.Document;

            List<View> views = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().ToList();
            if (views == null || views.Count == 0)
            {
                return Result.Failed;
            }

            List<ElementType> elementTypes = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_WallTags).OfClass(typeof(ElementType)).Cast<ElementType>().ToList();
            if (elementTypes == null || elementTypes.Count == 0)
            {
                return Result.Failed;
            }

            List<Autodesk.Revit.DB.Wall> walls = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Walls).OfClass(typeof(Autodesk.Revit.DB.Wall)).Cast<Autodesk.Revit.DB.Wall>().ToList();
            if (walls == null || walls.Count == 0)
            {
                return Result.Failed;
            }

            for (int i = views.Count - 1; i >= 0; i--)
            {
                View view = views[i];
                if (view == null)
                {
                    views.RemoveAt(i);
                    continue;
                }

                if (view.ViewType != ViewType.FloorPlan)
                {
                    views.RemoveAt(i);
                    continue;
                }

                if (!view.IsTemplate)
                {
                    views.RemoveAt(i);
                    continue;
                }

            }

            double minLength = 1.5;
            Core.UI.WPF.TextBoxWindow textBoxWindow = new Core.UI.WPF.TextBoxWindow("Wall Length", "Min Wall Length", minLength);

            // TextBoxWindow is not generic, so it carries no numeric key filter of its own. The WinForms
            // TextBoxForm<double> got one from SetValue attaching EventHandler.ControlText_NumberOnly;
            // this is that handler's own WPF overload, verbatim.
            textBoxWindow.Validation = (string x) => !System.Text.RegularExpressions.Regex.IsMatch(x, "[^0-9.-]+");

            new System.Windows.Interop.WindowInteropHelper(textBoxWindow).Owner = externalCommandData.Application.MainWindowHandle;

            if (textBoxWindow.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            // GetValue<double>() with no default returns 0 on an unparseable entry, which is what
            // TextBoxForm<double>.Value did. Preserved deliberately rather than defaulting to minLength.
            minLength = textBoxWindow.GetValue<double>();

            List<string> templateNames = new List<string> { "Heating Load" };

            List<string> templateNames_Checked = templateNames;

            Core.UI.WPF.MultipleSelectionTreeViewWindow treeViewWindow = new Core.UI.WPF.MultipleSelectionTreeViewWindow { Title = "Select Templates" };
            treeViewWindow.GettingText += (object sender, Core.UI.WPF.GettingTextEventArgs e) => e.Text = (e?.Object as View)?.Name;
            treeViewWindow.GettingChecked += (object sender, Core.UI.WPF.GettingCheckedEventArgs e) => e.Checked = templateNames_Checked.Contains((e?.Object as View)?.Name);
            treeViewWindow.SetObjects(views);

            new System.Windows.Interop.WindowInteropHelper(treeViewWindow).Owner = externalCommandData.Application.MainWindowHandle;

            if (treeViewWindow.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            templateNames = treeViewWindow.GetObjects<View>()?.ConvertAll(x => x.Name);

            if (templateNames == null || templateNames.Count == 0)
            {
                return Result.Failed;
            }

            minLength = UnitUtils.ConvertToInternalUnits(minLength, UnitTypeId.Meters);

            for (int i = walls.Count - 1; i >= 0; i--)
            {
                LocationCurve locationCurve = walls[i]?.Location as LocationCurve;
                if (locationCurve == null)
                {
                    walls.RemoveAt(i);
                    continue;
                }

                Curve curve = locationCurve.Curve;
                if (curve == null)
                {
                    walls.RemoveAt(i);
                    continue;
                }

                if (curve.Length < minLength)
                {
                    walls.RemoveAt(i);
                    continue;
                }
            }

            List<Tuple<ElementId, List<Autodesk.Revit.DB.Wall>>> tuples = new List<Tuple<ElementId, List<Autodesk.Revit.DB.Wall>>>();
            tuples.Add(new Tuple<ElementId, List<Autodesk.Revit.DB.Wall>>(elementTypes.Find(x => x.FamilyName == "Anno_Tag_SAM_CurtainWall")?.Id, walls.FindAll(x => x.WallType.Kind == WallKind.Curtain)));
            tuples.Add(new Tuple<ElementId, List<Autodesk.Revit.DB.Wall>>(elementTypes.Find(x => x.FamilyName == "Anno_Tag_SAM_Wall")?.Id, walls.FindAll(x => x.WallType.Kind != WallKind.Curtain)));

            using (Transaction transaction = new Transaction(document, "Add Wall Tags"))
            {
                using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Add Wall Tags", tuples.Count + 1))
                {
                    transaction.Start();

                    foreach (Tuple<ElementId, List<Autodesk.Revit.DB.Wall>> tuple in tuples)
                    {
                        if (tuple.Item1 == null || tuple.Item2 == null || tuple.Item2.Count == 0)
                        {
                            progressForm.Update("???");
                            continue;
                        }

                        progressForm.Update((document.GetElement(tuple.Item1) as ElementType)?.FamilyName);

                        List<IndependentTag> independentTags = Core.Revit.Modify.TagElements(document, templateNames, tuple.Item1, tuple.Item2.ConvertAll(x => x.Id), false, TagOrientation.Horizontal, new ViewType[] { ViewType.FloorPlan }, false);
                    }

                    progressForm.Update("Finishing");

                    transaction.Commit();
                }
            }

            return Result.Succeeded;
        }
    }
}
