using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections.Concrete
{
    public class RectangularSectionGeometry : SectionGeometryInfo
    {
        public double Width { get; set; }
        public double Depth { get; set; }

        public RectangularSectionGeometry(double width, double depth)
        {
            Width = width;
            Depth = depth;
        }

        public override Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
        {
            { "Width", Width },
            { "Depth", Depth }
        };
        }

        public override string ToString()
        {
            return $"Rectangular Section - Width: {Width}, Depth: {Depth}";
        }
    }
}


