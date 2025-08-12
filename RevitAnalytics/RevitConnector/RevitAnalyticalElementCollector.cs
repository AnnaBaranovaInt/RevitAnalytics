using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RevitAnalytics.Core;
using System;
using System.Collections.Generic;
using System.Linq;
// If you need logging or debugging, reference your DebugHandler here
// using YourNamespace.DebugHandler;

namespace RevitAnalytics.RevitConnector
{
    public static class RevitAnalyticalElementCollector
    {
        /// <summary>
        /// Collects analytical beams and columns from the Revit document (Revit 2023/2024 style).
        /// Converts feet to meters. Returns a list of AnalyticalElementInfo with geometry & label.
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>A list of AnalyticalElementInfo objects</returns>
        public static List<AnalyticalElementInfo> GetAllAnalyticalElementsFromRevit(Document doc)
        {
            List<AnalyticalElementInfo> elements = new List<AnalyticalElementInfo>();

            // ✅ Collect beams and columns
            elements.AddRange(GetAllBeamsAndColumnsFromRevit(doc));

            // ✅ Collect walls and floors
            elements.AddRange(GetAllPanelsFromRevit(doc));

            DebugHandler.Log($"Collected {elements.Count} total analytical elements.", DebugHandler.LogLevel.INFO);
            return elements;
        }

        public static List<AnalyticalFrameElementInfo> GetAllBeamsAndColumnsFromRevit(Document doc)
        {
            var result = new List<AnalyticalFrameElementInfo>();

            // If you have a custom logger, you can use it here:
            DebugHandler.Log("Defining categories for analytical beams and columns.", DebugHandler.LogLevel.INFO);

            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalyticalMember))
                .WhereElementIsNotElementType();

            DebugHandler.Log($"Collecting analytical models with valid curves. Found {collector.Count()} items.", DebugHandler.LogLevel.INFO);

            // 1 foot = 0.3048 meters
            double footToMeter = 0.3048;

            foreach (var elem in collector)
            {
                var analyticalMember = elem as AnalyticalMember;
                if (analyticalMember == null) continue;

                Curve curve = analyticalMember.GetCurve();
                if (curve == null) continue;

                XYZ start = curve.GetEndPoint(0);
                XYZ end = curve.GetEndPoint(1);

                double startX_m = start.X * footToMeter;
                double startY_m = start.Y * footToMeter;
                double startZ_m = start.Z * footToMeter;
                double endX_m = end.X * footToMeter;
                double endY_m = end.Y * footToMeter;
                double endZ_m = end.Z * footToMeter;

                string sectionName = AnalyticalMemberUtils.GetAnalyticalSectionName(analyticalMember, doc);
                string materialName = AnalyticalMemberUtils.GetMaterialName(analyticalMember, doc);

                Material material = doc.GetElement(analyticalMember.MaterialId) as Material;

                MaterialInfo materialInfo = null;

                if (material != null)
                {
                    materialInfo = Utils.MaterialUtils.GetMaterialInfo(material, doc);
                    //Utils.MaterialUtils.LogAllMaterialParameters(material);
                }
                else
                {
                    //task dialog too show that material is not found for an element
                    TaskDialog.Show("Material Not Found", $"Material not found for element {elem.Id}.");
                    DebugHandler.Log($"Material not found for element {elem.Id}.", DebugHandler.LogLevel.WARNING);
                }

                // We'll build a label from the Revit ID. 
                // E.g. "Revit_123" or "Revit_123_frame"
                string label = $"Revit_{elem.Id.Value}";

                AnalyticalFrameElementInfo info = new AnalyticalFrameElementInfo
                {
                    GUID = elem.UniqueId,
                    RevitId = elem.Id,
                    StartX = startX_m,
                    StartY = startY_m,
                    StartZ = startZ_m,
                    EndX = endX_m,
                    EndY = endY_m,
                    EndZ = endZ_m,
                    Depth = 0.3,
                    Width = 0.3,
                    SectionName = sectionName,
                    MaterialName = materialName,
                    MaterialInfo = materialInfo,
                    Label = label
                };

                result.Add(info);
            }

            DebugHandler.Log($"Collected {result.Count} analytical elements with valid curves.", DebugHandler.LogLevel.INFO);
            return result;
        }

        public static List<AnalyticalPanelElementInfo> GetAllPanelsFromRevit(Document doc)
        {
            var result = new List<AnalyticalPanelElementInfo>();
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalyticalPanel))
                .WhereElementIsNotElementType();

            double footToMeter = 0.3048;

            foreach (var elem in collector)
            {
                if (!(elem is AnalyticalPanel analyticalPanel)) continue;


                string materialName = AnalyticalMemberUtils.GetMaterialName(analyticalPanel, doc);

                Material material = doc.GetElement(analyticalPanel.MaterialId) as Material;
                MaterialInfo materialInfo = material != null ? Utils.MaterialUtils.GetMaterialInfo(material, doc) : null;

                AnalyticalPanel panel = elem as AnalyticalPanel;

                double thickness = panel.Thickness * footToMeter;

                //log the found thickness of an element
                DebugHandler.Log($"Found thickness {thickness} m for element {elem.Id}.");

                if (thickness == 0 || thickness == null)
                {
                    DebugHandler.Log($"Analytical panel {elem.Id} has zero thickness. We're attempting to get info from the physical representation..", DebugHandler.LogLevel.WARNING);

                    Element representation = Utils.AnalyticalPhysicalUtils.GetPhysicalElement(doc, analyticalPanel);

                    if (representation != null)
                    {
                        DebugHandler.Log($"Analytical panel {elem.Id} has a physical representation: {representation.Name}.", DebugHandler.LogLevel.INFO);
                        if (representation is Wall)
                        {
                            Wall wall = representation as Wall;
                            thickness = wall.Width * footToMeter;
                        }
                        else if (representation is Floor)
                        {
                            Floor floor = representation as Floor;
                            FloorType floorType = floor.Document.GetElement(floor.GetTypeId()) as FloorType;

                            if (floorType != null)
                            {
                                CompoundStructure structure = floorType.GetCompoundStructure();
                                if (structure != null)
                                {
                                    double totalThickness = 0;
                                    foreach (CompoundStructureLayer layer in structure.GetLayers())
                                    {
                                        totalThickness += layer.Width;  // Sum up the width of all layers
                                    }
                                    thickness = totalThickness * footToMeter;

                                    DebugHandler.Log($"Floor {floor.Id} has calculated thickness: {thickness}m.", DebugHandler.LogLevel.INFO);
                                }
                                else
                                {
                                    DebugHandler.Log($"⚠ Warning: Floor {floor.Id} has no compound structure. Using default thickness.", DebugHandler.LogLevel.WARNING);
                                }
                            }
                            else
                            {
                                DebugHandler.Log($"⚠ Warning: Floor {floor.Id} has no floor type. Using default thickness.", DebugHandler.LogLevel.WARNING);
                            }
                        }
                    }
                    else
                    {
                        //log that we couldnt find the physical representation
                        TaskDialog.Show("Physical Representation not found", $"Physical representation not found for element {elem.Id}.");
                        DebugHandler.Log($"⚠ Warning: Analytical panel {elem.Id} has no physical representation. Using default thickness - 200 mm.", DebugHandler.LogLevel.WARNING);
                        thickness = 0.2;
                    }
                }
                else
                {
                    //log the thicnkess
                    DebugHandler.Log($"Analytical panel {elem.Id} has thickness {thickness*100} cm.", DebugHandler.LogLevel.INFO);
                }


                string sectionName = $"{thickness * 100} cm | {material.Name}"; // Or another way to define section

                string label = $"Revit_{elem.Id.Value}";

                // ✅ Convert boundary to a list of points (XYZ -> meters)
                List<Tuple<double, double, double>> cornerPoints = new List<Tuple<double, double, double>>();
                foreach (Curve curve in analyticalPanel.GetOuterContour())
                {
                    XYZ pt = curve.GetEndPoint(0);
                    cornerPoints.Add(Tuple.Create(pt.X * footToMeter, pt.Y * footToMeter, pt.Z * footToMeter));
                }

                AnalyticalPanelElementInfo info = new AnalyticalPanelElementInfo
                {
                    Label = label,
                    GUID = elem.UniqueId,
                    RevitId = elem.Id,
                    CornerPoints = cornerPoints,
                    Thickness = thickness,
                    SectionName = sectionName,
                    MaterialName = materialName,
                    MaterialInfo = materialInfo,
                    ShellType = ShellTypeEnum.ShellThin, // Default ShellType
                    MatAngle = 0,
                    BendingThikness = 0
                };

                AnalyticalPanelElementInfo.LogAnalyticalPanelElementInfo(info);

                result.Add(info);
            }

            DebugHandler.Log($"Collected {result.Count} analytical panels (walls/floors).", DebugHandler.LogLevel.INFO);
            return result;

        }
    }
}
