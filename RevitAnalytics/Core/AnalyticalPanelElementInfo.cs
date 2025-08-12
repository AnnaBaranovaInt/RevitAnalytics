using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core
{
    public class AnalyticalPanelElementInfo : AnalyticalElementInfo
    {
        public override bool IsPanelElement => true;

        // ✅ Store an array of points (X, Y, Z) to define shape
        public List<Tuple<double, double, double>> CornerPoints { get; set; } = new List<Tuple<double, double, double>>();

        public double Thickness { get; set; }  // ✅ Walls and floors need thickness
        public ShellTypeEnum ShellType { get; set; } = ShellTypeEnum.ShellThin;
        public double MatAngle { get; set; } = 0; // ✅ Default material orientation angle
        public int GetSapShellType() => (int)ShellType;
        

        public double BendingThikness { get; set; } = 0.0; // ✅ Default bending thickness
        public static void LogAnalyticalPanelElementInfo(AnalyticalPanelElementInfo panelElement)
        {
            DebugHandler.Log($"Logging AnalyticalPanelElementInfo for {panelElement.Label}:", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"RevitId: {panelElement.RevitId}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"Label: {panelElement.Label}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"SectionName: {panelElement.SectionName}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"MaterialName: {panelElement.MaterialName}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"Thickness: {panelElement.Thickness}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"MatAngle: {panelElement.MatAngle}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"BendingThikness: {panelElement.BendingThikness}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"ShellType: {panelElement.ShellType}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"Color: {panelElement.Color}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"Notes: {panelElement.Notes}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log($"GUID: {panelElement.GUID}", DebugHandler.LogLevel.INFO);
            DebugHandler.Log("CornerPoints:", DebugHandler.LogLevel.INFO);
            foreach (var point in panelElement.CornerPoints)
            {
                DebugHandler.Log($"Point: ({point.Item1}, {point.Item2}, {point.Item3})", DebugHandler.LogLevel.INFO);
            }
        }
    }
    public enum ShellTypeEnum
    {
        ShellThin = 1,       // 1 = Shell - thin
        ShellThick = 2,      // 2 = Shell - thick
        PlateThin = 3,       // 3 = Plate - thin
        PlateThick = 4,      // 4 = Plate - thick
        Membrane = 5,        // 5 = Membrane
        ShellLayered = 6     // 6 = Shell layered/nonlinear
    }



}
