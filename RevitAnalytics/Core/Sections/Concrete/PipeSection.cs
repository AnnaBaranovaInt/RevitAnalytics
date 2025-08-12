using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections.Concrete
{
    public class PipeSection : SectionGeometryInfo
    {
        public double OutsideDiameter { get; set; }
        public double WallThickness { get; set; }

        public PipeSection(double outsideDiameter, double wallThickness)
        {
            this.OutsideDiameter = outsideDiameter;
            this.WallThickness = wallThickness;
        }

        public override Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                { "Outside Diameter", OutsideDiameter },
                { "Wall Thickness", WallThickness }
            };
        }

        public double GetInnerDiameter()
        {
            return OutsideDiameter - 2 * WallThickness;
        }

        public override string ToString()
        {
            return $"Pipe Section - Outside Diameter: {OutsideDiameter}, Wall Thickness: {WallThickness}";
        }
    }
}
