using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core
{
    public class AnalyticalElementInfo
    {
        public string Mark { get; set; }
        public string Label { get; set; }
        public string SectionName { get; set; }
        public string MaterialName { get; set; }
        public string GUID { get; set; }
        public string Notes { get; set; }
        public int Color; // ✅ Default color

        // ✅ MATERIAL INFORMATION (Full Object, not just name/type)
        public MaterialInfo MaterialInfo { get; set; }

        private MaterialType materialType = MaterialType.CONCRETE; // ✅ Default value

        public MaterialType MaterialType
        {
            get => materialType;
            set => materialType = value;  // This ensures it always has a valid value
        }
        // ✅ COMMON PROPERTIES
        public virtual bool IsFrameElement => false;  // Beams & columns
        public virtual bool IsPanelElement => false;  // Floors & walls

    }
}
