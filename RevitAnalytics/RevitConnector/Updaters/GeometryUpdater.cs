using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using RevitAnalytics.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.RevitConnector.Updaters
{
    class GeometryUpdater
    {
        public static void UpdateGeometry(AnalyticalMember am, AnalyticalElementInfo info)
        {
            if (info is AnalyticalFrameElementInfo frameElem)
            {
                UpdateFrameElement(am, frameElem);
            }
        }
        public static void UpdateGeometry(AnalyticalPanel am, AnalyticalElementInfo info)
        {
            if (info is AnalyticalPanelElementInfo panelElem)
            {
                UpdatePanelGeometry(am, panelElem);
            }
        }

        private static void UpdateFrameElement(AnalyticalMember am, AnalyticalFrameElementInfo info)
        {
            XYZ start = Converters.UnitConverter.ConvertPointFromMetersToFeet(new XYZ(info.StartX, info.StartY, info.StartZ));
            XYZ end = Converters.UnitConverter.ConvertPointFromMetersToFeet(new XYZ(info.EndX, info.EndY, info.EndZ));
            Curve newCurve = Line.CreateBound(start, end);

            try
            {
                am.SetCurve(newCurve);
                DebugHandler.Log($"Updated analytical curve for element {info.RevitId.Value}.", DebugHandler.LogLevel.INFO);
            }
            catch (System.Exception ex)
            {
                DebugHandler.LogError($"Could not update analytical curve for element {info.RevitId.Value}.", ex);
            }
        }

        /// ✅ **Update Geometry for Panel Elements (Polygons)**
        public static void UpdatePanelGeometry(AnalyticalPanel panel, AnalyticalPanelElementInfo info)
        {
            try
            {
                List<XYZ> revitPoints = new List<XYZ>();
                foreach (var point in info.CornerPoints)
                {
                    XYZ revitPoint = Converters.UnitConverter.ConvertPointFromMetersToFeet(new XYZ(point.Item1, point.Item2, point.Item3));
                    revitPoints.Add(revitPoint);
                }
                try
                {
                    // Convert List of XYZ points to CurveLoop
                    CurveLoop panelBoundary = new CurveLoop();

                    // Ensure we have at least 3 points (panels must be a closed loop)
                    if (revitPoints.Count < 3)
                    {
                        DebugHandler.LogError($"Not enough points to define panel boundary for {info.RevitId.Value}.");
                        return;
                    }

                    // Create curves from points
                    for (int i = 0; i < revitPoints.Count; i++)
                    {
                        XYZ start = revitPoints[i];
                        XYZ end = revitPoints[(i + 1) % revitPoints.Count]; // Loop back to the start
                        Line edge = Line.CreateBound(start, end);
                        panelBoundary.Append(edge);
                    }

                    // ✅ Set the boundary for the Analytical Panel using SetOuterContour
                    panel.SetOuterContour(panelBoundary);

                    DebugHandler.Log($"Updated panel boundary for element {info.RevitId.Value}.", DebugHandler.LogLevel.INFO);
                }
                catch (System.Exception ex)
                {
                    DebugHandler.LogError($"Could not update panel boundary for element {info.RevitId.Value}.", ex);
                }
                DebugHandler.Log($"Updated panel boundary for element {info.RevitId.Value}.", DebugHandler.LogLevel.INFO);
            }
            catch (System.Exception ex)
            {
                DebugHandler.LogError($"Could not update panel boundary for element {info.RevitId.Value}.", ex);
            }
        }
    }
}
