using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections
{
    public class SectionInfo
    {
        public string SectionName { get; set; }
        public SectionType Type { get; set; }
        public MaterialInfo Material { get; set; }
        public SectionGeometryInfo Geometry { get; set; }  // Stores geometry-specific properties

        public SectionInfo(string sectionName, SectionType type, MaterialInfo material, SectionGeometryInfo geometry)
        {
            SectionName = sectionName;
            Type = type;
            Material = material;
            Geometry = geometry;
        }
    }

}
