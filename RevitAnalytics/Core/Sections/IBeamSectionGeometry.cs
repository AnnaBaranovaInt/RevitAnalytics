using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections
{
    public class IBeamSectionGeometry : SectionGeometryInfo
    {
        
        public double TopFlangeWidth { get; }
        public double OutsideHeight { get; }
        public double BottomFlangeWidth { get; }
        public double BottomFlangeThickness { get; }
        public double FilletRadius { get; }
        public double WebThickness { get; }
        public double TopFlangeThickness { get; }

        public IBeamSectionGeometry(
            double height, double topFlangeWidth, double bottomFlangeWidth,
            double webThickness, double topFlangeThickness, double bottomFlangeThickness,
            double filletRadius)
        {
            OutsideHeight = height;
            TopFlangeWidth = topFlangeWidth;
            BottomFlangeWidth = bottomFlangeWidth;
            WebThickness = webThickness;
            TopFlangeThickness = topFlangeThickness;
            BottomFlangeThickness = bottomFlangeThickness;
            FilletRadius = filletRadius;
        }

        public override Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                { "Outside Height", OutsideHeight },
                { "TopFlange Width", TopFlangeWidth },
                { "BottomFlange Width", BottomFlangeWidth },
                { "Web Thickness", WebThickness },
                { "TopFlange Thickness", TopFlangeThickness },
                { "Bottom FlangeThickness", BottomFlangeThickness },
                { "Fillet Radius", FilletRadius }
            };
        }
    }
}
