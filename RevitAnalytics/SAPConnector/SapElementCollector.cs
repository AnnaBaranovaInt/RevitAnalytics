using Autodesk.Revit.DB;
using RevitAnalytics.Core;
using SAP2000v1;
using System;
using System.Collections.Generic;

namespace RevitAnalytics.SAPConnector
{
    public static class SapElementCollector
    {
        /// <summary>
        /// Retrieves all frame objects from the given SAP2000 model
        /// and converts them to AnalyticalElementInfo. 
        /// If the frame name starts with "Revit_", extracts the Revit ID.
        /// </summary>
        /// 
        public static List<AnalyticalElementInfo> GetAllAnalyticalElementsFromSap(cSapModel sapModel)
        {
            List<AnalyticalElementInfo> elements = new List<AnalyticalElementInfo>();

            // ✅ Collect frames (beams & columns)
            elements.AddRange(GetAllFramesFromSap(sapModel));

            // ✅ Collect panels (walls & floors)
            elements.AddRange(GetAllPanelsFromSap(sapModel));

            DebugHandler.Log($"Total {elements.Count} analytical elements retrieved from SAP2000.", DebugHandler.LogLevel.INFO);
            return elements;
        }

        public static List<AnalyticalElementInfo> GetAllPanelsFromSap(cSapModel sapModel)
        {
            var elements = new List<AnalyticalElementInfo>();
            int numberOfPanels = 0;
            string[] labels = null;

            sapModel.AreaObj.GetNameList(ref numberOfPanels, ref labels);
            DebugHandler.Log($"Retrieved {numberOfPanels} panel names from SAP2000.", DebugHandler.LogLevel.INFO);

            foreach (var label in labels)
            {
                // Retrieve corner points of shell
                int numPoints = 0;
                string[] pointNames = new string[20];  // SAP2000 supports up to 20 nodes per shell
                sapModel.AreaObj.GetPoints(label, ref numPoints, ref pointNames);

                var cornerPoints = new List<Tuple<double, double, double>>();
                for (int i = 0; i < numPoints; i++)
                {
                    double x = 0, y = 0, z = 0;
                    sapModel.PointObj.GetCoordCartesian(pointNames[i], ref x, ref y, ref z);
                    cornerPoints.Add(new Tuple<double, double, double>(x, y, z));
                }

                // Extract Revit ID
                string mark = label;

                // Retrieve shell properties
                int shellType = 0;
                string materialName = "";
                double matAngle = 0, thickness = 0, bending = 0;
                int color = -1;
                string notes = "", guid = "";

                sapModel.PropArea.GetShell(label, ref shellType, ref materialName, ref matAngle, ref thickness, ref bending, ref color, ref notes, ref guid);
                var materialInfo = Utils.MaterialUtils.GetMaterialInfoByName(sapModel, materialName);

                var shellElement = new AnalyticalPanelElementInfo
                {
                    Mark = mark,
                    CornerPoints = cornerPoints,
                    Label = label,
                    MaterialName = materialName,
                    MaterialInfo = materialInfo,
                    Thickness = thickness,
                    MatAngle = matAngle,
                    BendingThikness = bending,
                    ShellType = (ShellTypeEnum)shellType, // Convert int to enum
                    Color = color,
                    Notes = notes,
                    GUID = guid
                };

                AnalyticalPanelElementInfo.LogAnalyticalPanelElementInfo(shellElement);

                elements.Add(shellElement);
                DebugHandler.Log($"Added SHELL: {label}.", DebugHandler.LogLevel.INFO);
            }

            DebugHandler.Log($"Total {elements.Count} panels retrieved from SAP2000.", DebugHandler.LogLevel.INFO);
            return elements;
        }

        public static List<AnalyticalFrameElementInfo> GetAllFramesFromSap(cSapModel sapModel)
        {
            var elements = new List<AnalyticalFrameElementInfo>();

            int numberOfFrames = 0;
            string[] labels = null;

            sapModel.FrameObj.GetNameList(ref numberOfFrames, ref labels);
            DebugHandler.Log($"Retrieved {numberOfFrames} frame names from SAP2000.", DebugHandler.LogLevel.INFO);

            foreach (var label in labels)
            {
                string startPointName = "", endPointName = "";
                sapModel.FrameObj.GetPoints(label, ref startPointName, ref endPointName);
                DebugHandler.Log($"Points for frame {label}: Start={startPointName}, End={endPointName}.", DebugHandler.LogLevel.INFO);

                double startX = 0, startY = 0, startZ = 0;
                double endX = 0, endY = 0, endZ = 0;
                sapModel.PointObj.GetCoordCartesian(startPointName, ref startX, ref startY, ref startZ);
                sapModel.PointObj.GetCoordCartesian(endPointName, ref endX, ref endY, ref endZ);

                // Extract Revit ID from label if it starts with "Revit_"
                string mark = label;
                var materialName = Utils.MaterialUtils.GetMaterialName(sapModel, label);
                var materialInfo = Utils.MaterialUtils.GetMaterialInfoByName(sapModel, materialName);

                var element = new AnalyticalFrameElementInfo
                {
                    Mark = mark,
                    StartX = startX,
                    StartY = startY,
                    StartZ = startZ,
                    EndX = endX,
                    EndY = endY,
                    EndZ = endZ,
                    Label = label,
                    MaterialName = materialName,
                    MaterialInfo = materialInfo,
                    MaterialType = materialInfo.Type, 
                    SectionName = Utils.SectionUtils.GetSectionName(sapModel, label),
                };

                elements.Add(element);
                DebugHandler.Log($"Added AnalyticalElementInfo for frame {label}.", DebugHandler.LogLevel.INFO);
            }

            DebugHandler.Log($"Total {elements.Count} frames retrieved from SAP2000.", DebugHandler.LogLevel.INFO);
            return elements;
        }

        /// <summary>
        /// Retrieves the section name assigned to a frame element in SAP2000.
        /// </summary>
        public static string GetSectionName(cSapModel sapModel, string label)
        {
            string sectionName = "";
            string sAuto = ""; // Required third parameter

            int result = sapModel.FrameObj.GetSection(label, ref sectionName, ref sAuto);

            if (!string.IsNullOrEmpty(sectionName))
            {
                DebugHandler.Log($"Section name for {label}: {sectionName}.", DebugHandler.LogLevel.INFO);
            }
            else
            {
                DebugHandler.Log($"Section name for {label} is empty or undefined.", DebugHandler.LogLevel.WARNING);
            }

            return sectionName;
        }

    }
}
