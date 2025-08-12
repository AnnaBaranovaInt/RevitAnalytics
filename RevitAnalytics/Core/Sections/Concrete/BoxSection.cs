using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections.Concrete
{
    public class BoxSection : SectionGeometryInfo
    {
        public double OutsideDepth { get; set; }
        public double OutsideWidth { get; set; }
        public double FlangeThickness { get; set; }
        public double WebThickness { get; set; }
        public double CornerRadius { get; set; }

        public BoxSection(double outsideDepth, double outsideWidth, double flangeThickness, double webThickness, double cornerRadius)
        {
            this.OutsideDepth = outsideDepth;
            this.OutsideWidth = outsideWidth;
            this.FlangeThickness = flangeThickness;
            this.WebThickness = webThickness;
            this.CornerRadius = cornerRadius;
        }

        public override Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                { "Outside Depth", OutsideDepth },
                { "Outside Width", OutsideWidth },
                { "Flange Thickness", FlangeThickness },
                { "Web Thickness", WebThickness },
                { "Corner Radius", CornerRadius }
            };
        }

        public override string ToString()
        {
            return $"Box Section - Depth: {OutsideDepth}, Width: {OutsideWidth}, Flange Thickness: {FlangeThickness}, Web Thickness: {WebThickness}, Corner Radius: {CornerRadius}";
        }
    }
}
