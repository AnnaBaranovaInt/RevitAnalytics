using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections.Concrete
{
    public class TrapezoidalSectionGeometry : SectionGeometryInfo
    {
        public double Depth { get; set; }
        public double TopWidth { get; set; }
        public double BottomWidth { get; set; }

        public TrapezoidalSectionGeometry(double depth, double topWidth, double bottomWidth)
        {
            Depth = depth;
            TopWidth = topWidth;
            BottomWidth = bottomWidth;
        }

        public override Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
        {
            { "Depth", Depth },
            { "Top Width", TopWidth },
            { "Bottom Width", BottomWidth }
        };
        }

        public override string ToString()
        {
            return $"Trapezoidal Section - Depth: {Depth}, Top Width: {TopWidth}, Bottom Width: {BottomWidth}";
        }
    }

}
