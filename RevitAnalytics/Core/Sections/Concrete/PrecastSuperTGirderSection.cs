using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections.Concrete
{
    public class PrecastSuperTGirderSection : SectionGeometryInfo
    {
        public double B1 { get; set; }
        public double B2 { get; set; }
        public double B3 { get; set; }
        public double B4 { get; set; }
        public double B5 { get; set; }
        public double B6 { get; set; }
        public double BL { get; set; }
        public double BR { get; set; }
        public double D1 { get; set; }
        public double D2 { get; set; }
        public double D3 { get; set; }
        public double D4 { get; set; }
        public double D5 { get; set; }
        public double T1 { get; set; }
        public double T2 { get; set; }
        public double C1 { get; set; }
        public double C2 { get; set; }
        public bool ClosedFlange { get; set; }
        public bool Chamfer { get; set; }
        public bool Radius { get; set; }

        public PrecastSuperTGirderSection(
            double b1, double b2, double b3, double b4, double b5, double b6,
            double bl, double br, double d1, double d2, double d3, double d4, double d5,
            double t1, double t2, double c1, double c2,
            bool closedFlange, bool chamfer, bool radius)
        {
            this.B1 = b1;
            this.B2 = b2;
            this.B3 = b3;
            this.B4 = b4;
            this.B5 = b5;
            this.B6 = b6;
            this.BL = bl;
            this.BR = br;
            this.D1 = d1;
            this.D2 = d2;
            this.D3 = d3;
            this.D4 = d4;
            this.D5 = d5;
            this.T1 = t1;
            this.T2 = t2;
            this.C1 = c1;
            this.C2 = c2;
            this.ClosedFlange = closedFlange;
            this.Chamfer = chamfer;
            this.Radius = radius;
        }

        public override Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                { "B1", B1 },
                { "B2", B2 },
                { "B3", B3 },
                { "B4", B4 },
                { "B5", B5 },
                { "B6", B6 },
                { "BL", BL },
                { "BR", BR },
                { "D1", D1 },
                { "D2", D2 },
                { "D3", D3 },
                { "D4", D4 },
                { "D5", D5 },
                { "T1", T1 },
                { "T2", T2 },
                { "C1", C1 },
                { "C2", C2 },
                { "ClosedFlange", ClosedFlange },
                { "Chamfer", Chamfer },
                { "Radius", Radius }
            };
        }

        public override string ToString()
        {
            return $"Precast Super-T Girder - B1: {B1}, B2: {B2}, D1: {D1}, D2: {D2}, T1: {T1}, ClosedFlange: {ClosedFlange}";
        }
    }
}

