using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Forms;
using SAM.Core;
using SAM.Core.Revit;
using SAM.Geometry.Revit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Revit.UI
{
    public static partial class Modify
    {
        public static List<ElementId> CopyTags(this UIDocument uIDocument)
        {
            Document document = uIDocument?.Document;
            if(document == null)
            {
                return null;
            }

            View view_Source = null;
            View view_Destination = null;

            FamilySymbol familySymbol_Source = null;
            FamilySymbol familySymbol_Destination = null;

            using (CopyTagsForm copyTagsForm = new CopyTagsForm(uIDocument))
            {
                if(copyTagsForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return null;
                }


                view_Source = copyTagsForm.SourceViewTemplate;
                view_Destination = copyTagsForm.DestinationViewTemplate;

                familySymbol_Source = copyTagsForm.SourceTagType;
                familySymbol_Destination = copyTagsForm.DestinationTagType;
            }

            if(view_Source == null || view_Destination == null || familySymbol_Source == null)
            {
                return null;
            }

            if(familySymbol_Destination == null)
            {
                familySymbol_Destination = familySymbol_Source;
            }
            ConvertSettings convertSettings = new ConvertSettings(true, true, true);

            TagType tagType = Geometry.Revit.Convert.ToSAM_TagType(familySymbol_Destination, convertSettings);

            List<View> views = new FilteredElementCollector(document).OfClass(view_Source.GetType()).Cast<View>().ToList();
            if(views == null || views.Count == 0)
            {
                return null;
            }

            List<View> views_Source = views.FindAll(x => x.ViewTemplateId == view_Source.Id);
            if(views_Source == null || views_Source.Count == 0)
            {
                return null;
            }

            List<View> views_Destination = views.FindAll(x => x.ViewTemplateId == view_Destination.Id);
            if (views_Destination == null || views_Destination.Count == 0)
            {
                return null;
            }

            List<Tuple<View, List<Geometry.Revit.Tag>, List<View>>> tuples = new List<Tuple<View, List<Geometry.Revit.Tag>, List<View>>>();
            foreach (View view_Source_Temp in views_Source)
            {
                Level level_Source = view_Source_Temp?.GenLevel;
                if (level_Source == null)
                {
                    continue;
                }
                List<View> views_Destination_Temp = new List<View>();
                foreach (View view_Destination_Temp in views_Destination)
                {
                    Level level_Destination = view_Destination_Temp?.GenLevel;
                    if (level_Destination == null)
                    {
                        continue;
                    }

                    if (level_Source.Id == level_Destination.Id)
                    {
                        views_Destination_Temp.Add(view_Destination_Temp);
                    }
                }

                if (views_Destination_Temp == null || views_Destination_Temp.Count == 0)
                {
                    continue;
                }

                List<Element> elements = new FilteredElementCollector(document).OfCategoryId(familySymbol_Source.Category.Id).WherePasses(new ElementOwnerViewFilter(view_Source_Temp.Id)).ToList();
                elements.RemoveAll(x => x.GetTypeId() != familySymbol_Source.Id);

                if (elements == null || elements.Count == 0)
                {
                    continue;
                }

                List<Geometry.Revit.Tag> tags = new List<Geometry.Revit.Tag>();
                foreach (Element element in elements)
                {
                    Geometry.Revit.Tag tag = null;
                    if (element is SpaceTag)
                    {
                        tag = Geometry.Revit.Convert.ToSAM((SpaceTag)element, convertSettings);
                    }
                    else if (element is IndependentTag)
                    {
                        tag = Geometry.Revit.Convert.ToSAM((IndependentTag)element, convertSettings);
                    }

                    if(tag == null)
                    {
                        continue;
                    }

                    tags.Add(tag);
                }

                if(tags != null && tags.Count != 0)
                {
                    tuples.Add(new Tuple<View, List<Geometry.Revit.Tag>, List<View>>(view_Source_Temp, tags, views_Destination_Temp));
                }

            }

            if(tuples == null || tuples.Count == 0)
            {
                return null;
            }

            List<ElementId> result = new List<ElementId>();
            using (Transaction transaction = new Transaction(document, "Copy Tags"))
            {
                transaction.Start();

                using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Coping Tags", tuples.Count))
                {
                    foreach (Tuple<View, List<Geometry.Revit.Tag>, List<View>> tuple in tuples)
                    {
                        progressForm.Update(tuple.Item1.Name);
                        foreach (View view in tuple.Item3)
                        {
                            IntegerId viewId = Geometry.Revit.Query.IntegerId(view);

                            for (int i = 0; i < tuple.Item2.Count; i++)
                            {
                                Geometry.Revit.Tag tag = new Geometry.Revit.Tag(tagType, viewId, tuple.Item2[i].Location, tuple.Item2[i].Elbow, tuple.Item2[i].End,tuple.Item2[i].ReferenceId);
                                Core.Modify.CopyParameterSets(tuple.Item2[i], tag);

                                if(tag.Placed(document))
                                {
                                    continue;
                                }

                                BuiltInCategory? builtInCategory = tag.BuiltInCategory();
                                if (builtInCategory == null || !builtInCategory.HasValue)
                                {
                                    continue;
                                }

                                Element element = null;

                                if (builtInCategory != BuiltInCategory.OST_MEPSpaceTags)
                                {
                                    element = Geometry.Revit.Convert.ToRevit(tag, document, convertSettings);
                                }
                                else
                                {
                                    element = Geometry.Revit.Convert.ToRevit_SpaceTag(tag, document, convertSettings);
                                }

                                if (element != null)
                                {
                                    result.Add(element.Id);
                                }
                            }
                        }
                    }
                }


                transaction.Commit();
            }

            return result;

        }
    }
}