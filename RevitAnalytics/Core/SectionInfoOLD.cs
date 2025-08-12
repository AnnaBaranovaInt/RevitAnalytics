using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core
{
    class SectionInfoOLD
    {
        // Common Properties for All Sections
        // 🔹 Common Properties for All Sections
        public string MaterialName { get; set; }     // Material Name
        public string Notes { get; set; }            // Optional: Section Notes
        public string Color { get; set; }            // Optional: Display Color
        public SectionType Type { get; set; }        // Enum for section type
        public string SectionName { get; set; }
        public string SectionNotes { get; set; }
        public string PropertyModifiers { get; set; } // This can store any custom modifiers applied to the section

        // --- Concrete Sections ---
        // Rectangular Section
        public double? Depth { get; set; }  // t3
        public double? Width { get; set; }  // t2

        // Circular Section
        public double? Diameter { get; set; }  // t3

        // Pipe Section
        public double? OutsideDiameter { get; set; } // t3
        public double? WallThickness { get; set; } // tw

        // Box/Tube Section
        public double? OutsideDepth { get; set; } // t3
        public double? OutsideWidth { get; set; } // t2
        public double? FlangeThickness { get; set; } // tf
        public double? WebThickness { get; set; } // tw

        

        // 🔹 Circular Section (SetCircle)
        public double Diameter_t3 { get; set; }      // Diameter (t3)

        // 🔹 Pipe Section (SetPipe)
        public double OutsideDiameter_t3 { get; set; }  // Outside Diameter (t3)
        public double WallThickness_tw { get; set; }    // Wall Thickness (tw)

        // 🔹 Box/Tube Section (SetBox)
        public double OutsideDepth_t3 { get; set; }     // Outside Depth (t3)
        public double OutsideWidth_t2 { get; set; }     // Outside Width (t2)
        public double FlangeThickness_tf { get; set; }  // Flange Thickness (tf)
        public double WebThickness_tw { get; set; }     // Web Thickness (tw)
        public double CornerRadius { get; set; }        // Corner Radius (optional)

        // 🔹 Trapezoidal Section (SetTrapezoidal)
        public double BottomWidth_t2b { get; set; }     // Bottom Width (t2b)
        public double TopWidth_t2 { get; set; }         // Top Width (t2)
        public double TrapezoidDepth_t3 { get; set; }   // Depth (t3)

        // 🔹 Precast I-Section (SetPrecastI)
        public double B1 { get; set; }   // Top Flange Width
        public double B2 { get; set; }   // Bottom Flange Width
        public double B3 { get; set; }
        public double B4 { get; set; }
        public double D1 { get; set; }   // Heights
        public double D2 { get; set; }
        public double D3 { get; set; }
        public double D4 { get; set; }
        public double D5 { get; set; }
        public double D6 { get; set; }
        public double D7 { get; set; }
        public double T1 { get; set; }   // Flange Thickness
        public double T2 { get; set; }
        public double C1 { get; set; }   // Chamfer

        // 🔹 Precast U Girder (SetPrecastU)
        public double B5 { get; set; }
        public double B6 { get; set; }

        // 🔹 Precast Super-T Girder (SetPrecastSuperT)
        public bool ClosedFlange { get; set; }      // Closed Flange Option
        public bool ChamferRadius { get; set; }     // Chamfer or Radius Option
        public double BLmax { get; set; }           // Define BLmax
        public double BRmax { get; set; }           // Define BRmax

        // 🔹 Precast Box Girder (SetPrecastBox)
        public double Width_B { get; set; }
        public double Depth_D { get; set; }
        public double TopFlangeThickness_tf { get; set; }
        public double BottomFlangeThickness_tfb { get; set; }
        public double LeftWebThickness_tw { get; set; }
        public double RightWebThickness_twr { get; set; }
        public double Chamfer { get; set; }


        
        public double? TopWidth { get; set; } // t2
        public double? BottomWidth { get; set; } // t2b

        // Precast I (Bulb Tee Girder)
        public double? C2 { get; set; }

        // Precast Super-T
        public double? BL { get; set; }
        public double? BR { get; set; }

        // Precast Box Girder
        public double? DepthD { get; set; }
        public double? TopFlangeThickness { get; set; } // tf
        public double? BottomFlangeThickness { get; set; } // tfb
        public double? LeftWebThickness { get; set; } // tw
        public double? RightWebThickness { get; set; } // twr

        // --- Steel Sections ---

        // I / Wide Flange Section
        public double? OutsideHeight { get; set; } // t3
        public double? TopFlangeWidth { get; set; } // t2
        public double? BottomFlangeWidth { get; set; } // t2b
        public double? FilletRadius { get; set; }

        // Channel Section
        public double? OutsideFlangeWidth { get; set; } // t2

        // Angle Section
        public double? OutsideVerticalLeg { get; set; } // t3
        public double? OutsideHorizontalLeg { get; set; } // t2
        public double? HorizontalLegThickness { get; set; } // tf
        public double? VerticalLegThickness { get; set; } // tw

        // Double Angle Section
        public double? TotalDepth { get; set; } // t3
        public double? WidthOfSingleAngle { get; set; }
        public double? BackToBackDistance { get; set; } // dis

        // Double Channel Section
        public double? WidthOfSingleChannel { get; set; }

        // Steel Pipe Section (same as pipe in concrete)
        public double? PipeOutsideDiameter { get; set; } // t3
        public double? PipeWallThickness { get; set; } // tw

        // Steel Box/Tube Section (same as box/tube in concrete)
        public double? SteelBoxOutsideDepth { get; set; } // t3
        public double? SteelBoxOutsideWidth { get; set; } // t2
        public double? SteelBoxFlangeThickness { get; set; } // tf
        public double? SteelBoxWebThickness { get; set; } // tw
        public double? SteelBoxCornerRadius { get; set; }

        // Placeholder for any additional steel sections like Tee, Joists, etc.

        // Constructor
        public SectionInfoOLD(string sectionName)
        {
            SectionName = sectionName;
        }

        public SectionInfoOLD(string sectionName, SectionType type)
        {
            SectionName = sectionName;
            Type = type;
        }
    }
}
