using System.ComponentModel;

namespace SAM.Analytical.Revit.UI
{
    [Description("Geometry Calculation Method")]
    public enum GeometryCalculationMethod
    {
        [Description("Undefined")] Undefined,
        [Description("SAM")] SAM,
        [Description("gbXML")] gbXML,
    }
}