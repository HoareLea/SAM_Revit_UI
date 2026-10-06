// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Autodesk.Revit.DB;
using SAM.Core.Revit;
using SAM.Geometry.Revit;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Revit.UI
{
    public static partial class Modify
    {
        public static List<ElementId> ImportTags(this Document document, IEnumerable<Tag> tags)
        {
            return ImportTags(document, tags, out bool cancelled);
        }

        /// <summary>
        /// As above, but reports through <paramref name="cancelled"/> that the user stopped the import part
        /// way. This method does not own the transaction - every caller starts one around it - so it cannot
        /// undo the tags it has already placed; the caller has to roll back when this comes back true.
        /// </summary>
        public static List<ElementId> ImportTags(this Document document, IEnumerable<Tag> tags, out bool cancelled)
        {
            cancelled = false;

            if(document == null || tags == null)
            {
                return null;
            }

            List<ElementId> result = new List<ElementId>();

            ConvertSettings convertSettings = new ConvertSettings(true, true, true);

            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("Importing Tags", tags.Count()))
            {
                // One step per tag and Update pumps the message queue every step, so the form never stops
                // responding and the click is seen - no host thread of its own needed here.
                progressForm.Cancellable = true;
                progressForm.Note = "Cancel stops after the current tag - no tags are imported.";

                foreach (Tag tag in tags)
                {
                    string name = tag?.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = "???";
                    }

                    progressForm.Update(name);

                    // Checked after Update, because Update is what pumps the queue and so what turns a click
                    // made during the previous tag into a set flag.
                    if (progressForm.CancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }

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