using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections.Concrete
{
    public class CircularSection : SectionGeometryInfo
    {
        public double Diameter { get; set; }

        public CircularSection(double diameter)
        {
            this.Diameter = diameter;
        }

        public override Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                { "Diameter", Diameter }
            };
        }

        public override string ToString()
        {
            return $"Circular Section - Diameter: {Diameter}";
        }
    }
}
