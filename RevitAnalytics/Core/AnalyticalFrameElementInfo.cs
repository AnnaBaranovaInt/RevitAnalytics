using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core
{
    public class AnalyticalFrameElementInfo : AnalyticalElementInfo
    {
        public override bool IsFrameElement => true;

        public double StartX { get; set; }
        public double StartY { get; set; }
        public double StartZ { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double EndZ { get; set; }

        public double Depth { get; set; }  // e.g. in meters
        public double Width { get; set; }  // e.g. in meters
    }

}
