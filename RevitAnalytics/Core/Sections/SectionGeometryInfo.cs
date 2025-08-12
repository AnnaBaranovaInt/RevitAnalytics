using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections
{
    public abstract class SectionGeometryInfo
    {
        public abstract Dictionary<string, object> GetParameters();
    }
}
