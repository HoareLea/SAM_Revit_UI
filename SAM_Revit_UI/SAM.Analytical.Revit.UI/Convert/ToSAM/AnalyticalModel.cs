using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using SAM.Core.Revit;
using SAM.Geometry.Revit;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Revit.UI
{
    public static partial class Convert
    {
        public static AnalyticalModel ToSAM(this Document document, GeometryCalculationMethod geometryCalculationMethod, out Dictionary<Guid, ElementId> dictionary)
        {
            dictionary = null;

            if (document == null)
            {
                return null;
            }

            AnalyticalModel result = null;

            List<Panel> panels_Temp = null;
            AdjacencyCluster adjacencyCluster_Temp = null;
            IEnumerable<Panel> panels = null;
            List<Shell> shells = null;
            IEnumerable<Space> spaces = null;
            ConvertSettings convertSettings = new ConvertSettings(true, true, true);

            switch (geometryCalculationMethod)
            {
                case GeometryCalculationMethod.gbXML:
                    dictionary = new Dictionary<Guid, ElementId>();
                    using (Transaction transaction = new Transaction(document, "Convert Model"))
                    {
                        transaction.Start();

                        result = Revit.Convert.ToSAM_AnalyticalModel(document, new ConvertSettings(true, true, false));
                        panels_Temp = result?.GetPanels();
                        if (panels_Temp != null)
                        {
                            foreach (Panel panel in panels_Temp)
                            {
                                EnergyAnalysisSurface energyAnalysisSurface = Core.Revit.Query.Element<EnergyAnalysisSurface>(document, panel);
                                HostObject hostObject = Core.Revit.Query.Element(document, energyAnalysisSurface?.CADObjectUniqueId, energyAnalysisSurface?.CADLinkUniqueId) as HostObject;
                                if (hostObject != null)
                                {
                                    dictionary[panel.Guid] = hostObject.Id;
                                }

                                List<Aperture> apertures = panel.Apertures;
                                if (apertures != null)
                                {
                                    foreach (Aperture aperture in apertures)
                                    {
                                        EnergyAnalysisOpening energyAnalysisOpening = Core.Revit.Query.Element<EnergyAnalysisOpening>(document, aperture);
                                        FamilyInstance familyInstance = Core.Revit.Query.Element(energyAnalysisOpening) as FamilyInstance;
                                        if (familyInstance != null)
                                        {
                                            dictionary[aperture.Guid] = familyInstance.Id;
                                        }
                                    }
                                }
                            }
                        }

                        transaction.RollBack();
                    }
                    break;

                case GeometryCalculationMethod.SAM:
                    using (Transaction transaction = new Transaction(document, "Convert Model"))
                    {
                        transaction.Start();

                        result = Revit.Convert.ToSAM_AnalyticalModel(document, new ConvertSettings(true, true, false));

                        transaction.RollBack();
                    }

                    panels = Revit.Convert.ToSAM<Panel>(document, convertSettings);

                    shells = Analytical.Query.Shells(panels, 0.1, Core.Tolerance.MacroDistance);
                    if (shells == null || shells.Count == 0)
                    {
                        return null;
                    }

                    spaces = Revit.Convert.ToSAM<Space>(document, convertSettings);

                    adjacencyCluster_Temp = Analytical.Create.AdjacencyCluster(shells, spaces, panels, false, true, 0.01, Core.Tolerance.MacroDistance, 0.01, 0.0872664626, Core.Tolerance.MacroDistance, Core.Tolerance.Distance, Core.Tolerance.Angle);
                    panels_Temp = adjacencyCluster_Temp.GetPanels();
                    if (panels_Temp != null && panels_Temp.Count != 0)
                    {
                        List<Aperture> apertures = new List<Aperture>();
                        foreach (Panel panel in panels)
                        {
                            List<Aperture> apertures_Temp = panel?.Apertures;
                            if (apertures_Temp != null)
                            {
                                apertures.AddRange(apertures_Temp);
                            }
                        }

                        foreach (Panel panel_Temp in panels_Temp)
                        {
                            List<Aperture> apertures_Temp = panel_Temp?.Apertures;
                            if (apertures_Temp != null)
                            {
                                for (int i = 0; i < apertures_Temp.Count; i++)
                                {
                                    Aperture aperture_Temp = apertures_Temp[i];

                                    Point3D point3D = aperture_Temp?.Face3D?.InternalPoint3D(Core.Tolerance.MacroDistance);
                                    if (point3D == null)
                                    {
                                        continue;
                                    }

                                    Aperture aperture = apertures.InRange(point3D, new Core.Range<double>(0, Core.Tolerance.MacroDistance), true, 1, Core.Tolerance.Distance)?.FirstOrDefault();
                                    if (aperture == null)
                                    {
                                        continue;
                                    }

                                    ElementId elementId = aperture.ElementId();
                                    if (elementId != null && elementId != ElementId.InvalidElementId)
                                    {
                                        apertures_Temp[i].SetValue(ElementParameter.RevitId, Geometry.Revit.Query.IntegerId(document.GetElement(elementId)));
                                        panel_Temp.RemoveAperture(apertures_Temp[i].Guid);
                                        panel_Temp.AddAperture(apertures_Temp[i]);
                                    }
                                }

                                adjacencyCluster_Temp.AddObject(panel_Temp);
                            }
                        }
                    }

                    break;

                case GeometryCalculationMethod.Topologic:
                    using (Transaction transaction = new Transaction(document, "Convert Model"))
                    {
                        transaction.Start();

                        result = Revit.Convert.ToSAM_AnalyticalModel(document, new ConvertSettings(true, true, false));

                        transaction.RollBack();
                    }

                    panels = Revit.Convert.ToSAM<Panel>(document, convertSettings);

                    shells = Analytical.Query.Shells(panels, 0.1, Core.Tolerance.MacroDistance);
                    if (shells == null || shells.Count == 0)
                    {
                        return null;
                    }

                    spaces = Revit.Convert.ToSAM<Space>(document, convertSettings);
                    adjacencyCluster_Temp = Topologic.Create.AdjacencyCluster(spaces, panels, out List<global::Topologic.Topology> topologies, out List<Panel> redundantPanels);
                    panels_Temp = adjacencyCluster_Temp.GetPanels();
                    if (panels_Temp != null && panels_Temp.Count != 0)
                    {
                        List<Aperture> apertures = new List<Aperture>();
                        foreach (Panel panel in panels)
                        {
                            List<Aperture> apertures_Temp = panel?.Apertures;
                            if (apertures_Temp != null)
                            {
                                apertures.AddRange(apertures_Temp);
                            }
                        }

                        foreach (Panel panel_Temp in panels_Temp)
                        {
                            List<Aperture> apertures_Temp = panel_Temp?.Apertures;
                            if (apertures_Temp != null)
                            {
                                for (int i = 0; i < apertures_Temp.Count; i++)
                                {
                                    Aperture aperture_Temp = apertures_Temp[i];

                                    Point3D point3D = aperture_Temp?.Face3D?.InternalPoint3D(Core.Tolerance.MacroDistance);
                                    if (point3D == null)
                                    {
                                        continue;
                                    }

                                    Aperture aperture = apertures.InRange(point3D, new Core.Range<double>(0, Core.Tolerance.MacroDistance), true, 1, Core.Tolerance.Distance)?.FirstOrDefault();
                                    if (aperture == null)
                                    {
                                        continue;
                                    }

                                    ElementId elementId = aperture.ElementId();
                                    if(elementId != null && elementId != ElementId.InvalidElementId)
                                    {
                                        apertures_Temp[i].SetValue(ElementParameter.RevitId, Geometry.Revit.Query.IntegerId(document.GetElement(elementId)));
                                        panel_Temp.RemoveAperture(apertures_Temp[i].Guid);
                                        panel_Temp.AddAperture(apertures_Temp[i]);
                                    }
                                }

                                adjacencyCluster_Temp.AddObject(panel_Temp);
                            }
                        }
                    }

                    break;

                case GeometryCalculationMethod.Undefined:
                    using (Transaction transaction = new Transaction(document, "Convert Model"))
                    {
                        transaction.Start();

                        result = Revit.Convert.ToSAM_AnalyticalModel(document, new ConvertSettings(true, true, false));

                        transaction.RollBack();
                    }

                    panels = Revit.Convert.ToSAM<Panel>(document, convertSettings);

                    spaces = Revit.Convert.ToSAM<Space>(document, convertSettings);

                    adjacencyCluster_Temp = new AdjacencyCluster();
                    panels?.ToList().ForEach(x => adjacencyCluster_Temp.AddObject(x));
                    spaces?.ToList().ForEach(x => adjacencyCluster_Temp.AddObject(x));

                    break;
            }

            if(spaces != null)
            {
                foreach (Space space in spaces)
                {
                    ElementId elementId = null;
                    if (dictionary == null || !dictionary.TryGetValue(space.Guid, out elementId))
                    {
                        elementId = null;
                    }

                    if (elementId == null)
                    {
                        elementId = space.ElementId();
                    }

                    if(elementId == null)
                    {
                        continue;
                    }

                    Autodesk.Revit.DB.Mechanical.Space space_Revit = document.GetElement(elementId) as Autodesk.Revit.DB.Mechanical.Space;
                    if(space_Revit == null)
                    {
                        continue;
                    }

                    IEnumerable<ElementId> elementIds = space_Revit.GetDependentElements(new ElementCategoryFilter(BuiltInCategory.OST_MEPSpaceTags));
                    if(elementIds != null)
                    {
                        foreach(ElementId elementId_Temp in elementIds)
                        {
                            Autodesk.Revit.DB.Mechanical.SpaceTag spaceTag = document.GetElement(elementId_Temp) as Autodesk.Revit.DB.Mechanical.SpaceTag;
                            Tag tag = Geometry.Revit.Convert.ToSAM(spaceTag, convertSettings);
                            if(tag == null)
                            {
                                continue;
                            }

                            adjacencyCluster_Temp.AddObject(tag);
                            adjacencyCluster_Temp.AddRelation(space, tag);
                        }
                    }
                }
            }

            if(adjacencyCluster_Temp == null || result == null)
            {
                return null;
            }

            result = new AnalyticalModel(result, adjacencyCluster_Temp);

            return result;
        }
    }
}