using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAM.Analytical.Revit.UI.Properties;
using SAM.Core.Revit;
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

            Reference reference = null;

            try
            {
                reference = externalCommandData.Application.ActiveUIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
            }
            catch
            {
                return Result.Failed;
            }


            if(reference == null)
            {
                return Result.Cancelled;
            }

            Autodesk.Revit.DB.Mechanical.Space space_Revit = document.GetElement(reference) as Autodesk.Revit.DB.Mechanical.Space;
            if(space_Revit == null)
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

            Parameter parameter = document.ProjectInformation.LookupParameter("SAM_ProjectICs");
            if(parameter != null && parameter.HasValue && parameter.StorageType == StorageType.String)
            {
                string json = parameter.AsString();
                if(!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        profileLibrary = Core.Convert.ToSAM<ProfileLibrary>(json)?.FirstOrDefault();
                    }
                    catch
                    {
                        profileLibrary = null;
                    }
                }
            }

            if(profileLibrary == null)
            {
                profileLibrary = Analytical.Query.DefaultProfileLibrary();
            }

            List<Space> spaces = analyticalModel?.GetSpaces();
            if(spaces == null || spaces.Count == 0)
            {
                return Result.Failed;
            }

            Space space = spaces.Find(x => x.ElementId() == space_Revit.Id);
            if(space == null)
            {
                return Result.Failed;
            }

            //using (Windows.Forms.SpaceForm spaceForm = new Windows.Forms.SpaceForm(space, Analytical.Query.DefaultProfileLibrary(), analyticalModel.AdjacencyCluster, Core.Query.Enums(typeof(Space))))
            using (Windows.Forms.InternalConditionForm internalConditionForm = new Windows.Forms.InternalConditionForm(new Space(space), profileLibrary, analyticalModel.AdjacencyCluster))
            {
                if(internalConditionForm.ShowDialog() != DialogResult.OK)
                {
                    return Result.Cancelled;
                }

                space = internalConditionForm.Space;
                profileLibrary = internalConditionForm.ProfileLibrary;
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
                    parameter.Set(Core.Convert.ToString(profileLibrary));
                }

                Core.Revit.Modify.SetValues(space_Revit, space);
                Core.Revit.Modify.SetValues(space_Revit, space, ActiveSetting.Setting, parameters: convertSettings.GetParameters());
                InternalCondition internalCondition = space.InternalCondition;
                if (internalCondition != null)
                {
                    Core.Revit.Modify.SetValues(space_Revit, internalCondition);
                    Core.Revit.Modify.SetValues(space_Revit, internalCondition, ActiveSetting.Setting, convertSettings.GetParameters());
                }

                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }
}
