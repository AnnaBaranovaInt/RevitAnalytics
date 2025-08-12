using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections
{
    public enum SectionType
    {
        // --- Concrete Sections ---
        Rectangular,
        Circular,
        Pipe,
        Box,
        Trapezoidal,
        PrecastI,         // Bulb Tee Girder
        PrecastU,
        PrecastSuperT,
        PrecastBox,

        // --- Steel Sections ---
        WideFlange,       // I / Wide Flange Section
        Channel,
        Tee,
        Angle,
        DoubleAngle,
        DoubleChannel,
        SteelPipe,
        SteelBox,

        // Additional placeholders for future extension
        SteelJoist
    }
}
