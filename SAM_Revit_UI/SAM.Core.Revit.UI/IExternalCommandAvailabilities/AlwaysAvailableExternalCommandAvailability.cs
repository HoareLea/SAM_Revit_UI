using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace SAM.Core.Revit.UI
{
    public class AlwaysAvailableExternalCommandAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }
}
