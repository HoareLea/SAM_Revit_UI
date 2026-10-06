// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.Revit.UI
{
    [Description("Geometry Calculation Method")]
    public enum GeometryCalculationMethod
    {
        [Description("Undefined")] Undefined,
        [Description("SAM")] SAM,
        [Description("OCCT")] OCCT,
        [Description("gbXML")] gbXML,
    }
}
