using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.SAPConnector.SAPLogic
{
    public class SapShellSection
    {
        public string Name { get; }
        public string Material { get; }
        public double Thickness { get; }
        public double MatAngle { get; }
        public double Bending { get; }
        public int Color { get; }
        public string Notes { get; }
        public string GUID { get; }
        public int ShellType { get; }


        public SapShellSection(cSapModel sapModel, string materialName, string sectionName, double thickness, double matAngle, double bending, int type)
        {
            Name = sectionName;
            Material = materialName;
            Thickness = thickness;
            MatAngle = matAngle;
            Bending = bending;

            // Add section to SAP2000
            int ret = sapModel.PropArea.SetShell(sectionName, type, materialName, matAngle, thickness, bending);
            if (ret != 0)
            {
                DebugHandler.Log($"Failed to create shell section {sectionName}.", DebugHandler.LogLevel.ERROR);
            }
        }
        public SapShellSection(
         cSapModel sapModel, string materialName, string sectionName,
         double thickness, double matAngle, double bending,
         int type, int color, string notes, string guid)
        {
            Name = sectionName;
            Material = materialName;
            Thickness = thickness;
            MatAngle = matAngle;
            Bending = bending;

            // ✅ Retrieve existing properties if new ones are empty
            string existingNotes = "";
            string existingGUID = "";
            int existingColor = -1;  // SAP2000 default for color

            //int ret = sapModel.PropArea.GetShell(sectionName, ref type, ref materialName, ref matAngle, ref thickness, ref bending, ref existingColor, ref existingNotes, ref existingGUID);

            //if (ret != 0)
            //{
            //    DebugHandler.Log($"Warning: Could not retrieve existing shell section properties for {sectionName}.");
            //}

            // ✅ Use existing values if new ones are empty
            Color = (color != -1) ? color : existingColor;
            Notes = !string.IsNullOrEmpty(notes) ? notes : existingNotes;
            GUID = !string.IsNullOrEmpty(guid) ? guid : existingGUID;

            // ✅ Add/Update section in SAP2000
            int ret = sapModel.PropArea.SetShell(sectionName, type, materialName, matAngle, thickness, bending, Color, Notes, GUID);
            if (ret != 0)
            {
                DebugHandler.Log($"Failed to create or update shell section {sectionName}.", DebugHandler.LogLevel.ERROR);
            }
        }
    }

}
