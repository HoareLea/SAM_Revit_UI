using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace SAM.Analytical.Revit.UI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ModifyInternalCondition : Core.Revit.UI.PushButtonExternalCommand
    {
        public override string RibbonPanelName => "Analytical";

        public override int Index => 8;

        public override BitmapSource BitmapSource => Core.Windows.Convert.ToBitmapSource(Resources.SAM_Small);

        public override string Text => "Modify Internal\nCondition";

        public override string ToolTip => "Modify Internal\nCondition";

        public override string AvailabilityClassName => null;

        public override Result Execute(ExternalCommandData externalCommandData, ref string message, ElementSet elementSet)
        {
            Document document = externalCommandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                return Result.Failed;
            }

            IList<Reference> references = null;

            try
            {
                references = externalCommandData.Application.ActiveUIDocument.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element);
            }
            catch
            {
                return Result.Failed;
            }


            if(references == null)
            {
                return Result.Cancelled;
            }

            List<Autodesk.Revit.DB.Mechanical.Space> spaces_Revit = new List<Autodesk.Revit.DB.Mechanical.Space>();
            foreach(Reference reference in references)
            {
                Autodesk.Revit.DB.Mechanical.Space space_Revit = document.GetElement(reference) as Autodesk.Revit.DB.Mechanical.Space;
                if (space_Revit == null)
                {
                    continue; ;
                }

                spaces_Revit.Add(space_Revit);
            }

            if(spaces_Revit == null || spaces_Revit.Count == 0)
            {
                return Result.Failed;
            }

            AnalyticalModel analyticalModel = null;

            using (Transaction transaction = new Transaction(document, "Convert Model"))
            {
                transaction.Start();

                analyticalModel = Convert.ToSAM_AnalyticalModel(document, new ConvertSettings(true, true, false));

                transaction.RollBack();
            }

            ProfileLibrary profileLibrary = null;
            if(profileLibrary == null)
            {
                profileLibrary = Analytical.Query.DefaultProfileLibrary();
            }

            InternalConditionLibrary internalConditionLibrary = null;

            Parameter parameter = document.ProjectInformation.LookupParameter("SAM_ProjectICs");
            if (parameter != null && parameter.HasValue && parameter.StorageType == StorageType.String)
            {
                string json = parameter.AsString();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        internalConditionLibrary = Core.Convert.ToSAM<InternalConditionLibrary>(json)?.FirstOrDefault();
                        if(internalConditionLibrary != null)
                        {
                            foreach(InternalCondition internalCondition in internalConditionLibrary.GetInternalConditions())
                            {
                                analyticalModel?.AddInternalCondition(internalCondition);
                            }
                        }
                    }
                    catch
                    {
                        internalConditionLibrary = null;
                    }
                }
            }

            List<Space> spaces = analyticalModel?.GetSpaces();
            if(spaces == null || spaces.Count == 0)
            {
                return Result.Failed;
            }

            List<Tuple<Space, Autodesk.Revit.DB.Mechanical.Space>> tuples = new List<Tuple<Space, Autodesk.Revit.DB.Mechanical.Space>>();
            foreach(Autodesk.Revit.DB.Mechanical.Space space_Revit in spaces_Revit)
            {
                Space space = spaces.Find(x => x.ElementId() == space_Revit.Id);
                if (space == null)
                {
                    continue;
                }

                tuples.Add(new Tuple<Space, Autodesk.Revit.DB.Mechanical.Space>(space, space_Revit));
            }

            if(tuples.Count == 1)
            {
                using (Windows.Forms.InternalConditionForm internalConditionForm = new Windows.Forms.InternalConditionForm(new Space(tuples[0].Item1), profileLibrary, analyticalModel.AdjacencyCluster))
                {
                    if (internalConditionForm.ShowDialog() != DialogResult.OK)
                    {
                        return Result.Cancelled;
                    }

                    tuples[0] = new Tuple<Space, Autodesk.Revit.DB.Mechanical.Space>(internalConditionForm.Space, tuples[0].Item2);
                    profileLibrary = internalConditionForm.ProfileLibrary;

                    analyticalModel = new AnalyticalModel(analyticalModel, internalConditionForm.AdjacencyCluster);
                }
            }
            else
            {
                using (Windows.Forms.SpacesForm spacesForm = new Windows.Forms.SpacesForm(tuples.ConvertAll(x => x.Item1), analyticalModel.AdjacencyCluster, profileLibrary))
                {
                    if (spacesForm.ShowDialog() != DialogResult.OK)
                    {
                        return Result.Cancelled;
                    }

                    List<Space> spaces_Temp = spacesForm.Spaces?.ToList();

                    for(int i =0; i < tuples.Count; i++)
                    {
                        Space space_Temp = spaces_Temp.Find(x => x.Guid == tuples[i].Item1.Guid);
                        if(space_Temp == null)
                        {
                            continue;
                        }

                        tuples[i] = new Tuple<Space, Autodesk.Revit.DB.Mechanical.Space>(space_Temp, tuples[i].Item2);
                    }

                    profileLibrary = spacesForm.ProfileLibrary;

                    analyticalModel = new AnalyticalModel(analyticalModel, spacesForm.AdjacencyCluster);
                }
            }

            ConvertSettings convertSettings = new ConvertSettings(false, true, false);
            convertSettings.AddParameter("AdjacencyCluster", analyticalModel.AdjacencyCluster);
            convertSettings.AddParameter("AnalyticalModel", analyticalModel);

            using (Transaction transaction = new Transaction(document, "Modify Internal Condition"))
            {
                transaction.Start();

                parameter = document.ProjectInformation.LookupParameter("SAM_ProjectICs");
                if(parameter != null && parameter.StorageType == StorageType.String)
                {
                    parameter.Set(Core.Convert.ToString(internalConditionLibrary));
                }

                foreach(Tuple<Space, Autodesk.Revit.DB.Mechanical.Space> tuple in tuples)
                {
                    Space space = tuple.Item1;
                    Autodesk.Revit.DB.Mechanical.Space space_Revit = tuple.Item2;

                    Core.Revit.Modify.SetValues(space_Revit, space);
                    Core.Revit.Modify.SetValues(space_Revit, space, ActiveSetting.Setting, parameters: convertSettings.GetParameters());
                    InternalCondition internalCondition = space.InternalCondition;
                    if (internalCondition != null)
                    {
                        Core.Revit.Modify.SetValues(space_Revit, internalCondition);
                        Core.Revit.Modify.SetValues(space_Revit, internalCondition, ActiveSetting.Setting, convertSettings.GetParameters());
                    }
                }

                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }
}
