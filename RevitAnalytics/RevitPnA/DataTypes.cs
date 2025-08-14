using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.RevitPnA
{
    public class Frame
    {
        public string FrameName { get; set; }
        public List<RevitAnalyticalElementInfo> Elements { get; set; } = new List<RevitAnalyticalElementInfo>();
    }
    public class RevitAnalyticalElementInfo
    {
        public ElementId PhRevitId { get; set; }
        public Element Element { get; set; } // Reference to the Revit Element
        public AnalyticalMember AnalyticalMember { get; set; } // Reference to the AnalyticalMember (if applicable)
        public ElementId AnRevitId { get; set; } // ✅ Revit Element ID
        public Frame Frame { get; set; } // Reference to the Frame this element belongs to, if applicable
        public string Mark { get; set; }
        public string FrameName { get; set; } // Type Mark of the element, if available
        public string FirstSupportName { get; set; } // Type Mark of the first support, if available
        public string SecondSupportName { get; set; } // Type Mark of the second support, if available
        public string LeadingRole { get; set; } // Type Mark of the element, if available
        public double Rotation { get; set; } // Type Mark of the element, if available
        public AnalyticalElementType AnalyticalElementType { get; set; } // Type of the analytical element (e.g., Beam, Column, Wall, etc.)
        public FamilySymbol FamilyType { get; set; }
        public Material Material { get; set; } // Reference to the Revit Material
        public string Label { get; set; }
        public string SectionName { get; set; }
        public string MaterialName { get; set; }
        public string GUID { get; set; }
        public string Notes { get; set; }
        public int Color; // ✅ Default color

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
