using Autodesk.Revit.DB;
using SAM.Core.Revit;
using SAM.Core.Windows;
using SAM.Geometry.Revit;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Revit.UI
{
    public static partial class Modify
    {
        public static List<ElementId> ImportTags(this Document document, IEnumerable<Tag> tags)
        {
            if(document == null || tags == null)
            {
                return null;
            }

            List<ElementId> result = new List<ElementId>();

            ConvertSettings convertSettings = new ConvertSettings(true, true, true);

            using (SimpleProgressForm simpleProgressForm = new SimpleProgressForm("Importing Tags", string.Empty, tags.Count()))
            {
                foreach (Tag tag in tags)
                {
                    string name = tag?.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = "???";
                    }

                    simpleProgressForm.Increment(name);
                    if (tag == null)
                    {
                        continue;
                    }

                    if (tag.Placed(document))
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

                    if(element != null)
                    {
                        result.Add(element.Id);
                    }
                }
            }

            return result;
        }
    }
}