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
    public class AddWallTags : PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Project Setup";

        public override int Index => 13;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small);

        public override string Text => "Add\nWall Tags";

        public override string ToolTip => "Add Wall Tags";

        public override string AvailabilityClassName => null;

        public override void Execute()
        {
            Document document = Document;

            List<View> views = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().ToList();
            if (views == null || views.Count == 0)
            {
                return;
            }

            List<ElementType> elementTypes = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_WallTags).OfClass(typeof(ElementType)).Cast<ElementType>().ToList();
            if (elementTypes == null || elementTypes.Count == 0)
            {
                return;
            }

            List<Autodesk.Revit.DB.Wall> walls = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Walls).OfClass(typeof(Autodesk.Revit.DB.Wall)).Cast<Autodesk.Revit.DB.Wall>().ToList();
            if (walls == null || walls.Count == 0)
            {
                return;
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
            using (Core.Windows.Forms.TextBoxForm<double> textBoxForm = new Core.Windows.Forms.TextBoxForm<double>("Wall Length", "Min Wall Length"))
            {
                textBoxForm.Value = minLength;
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                minLength = textBoxForm.Value;
            }

            List<string> templateNames = new List<string> { "Heating Load" };

            using (Core.Windows.Forms.TreeViewForm<View> treeViewForm = new Core.Windows.Forms.TreeViewForm<View>("Select Templates", views, (View view) => view.Name, null, (View view) => templateNames.Contains(view.Name)))
            {
                if (treeViewForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                templateNames = treeViewForm.SelectedItems?.ConvertAll(x => x.Name);
            }

            if (templateNames == null || templateNames.Count == 0)
            {
                return;
            }

#if Revit2017 || Revit2018 || Revit2019 || Revit2020
            minLength = UnitUtils.ConvertToInternalUnits(minLength, DisplayUnitType.DUT_METERS);
#else
            minLength = UnitUtils.ConvertToInternalUnits(minLength, UnitTypeId.Meters);
#endif

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
        }
    }
}
