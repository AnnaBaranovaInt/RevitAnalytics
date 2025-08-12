using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.Core.Sections.Concrete
{
    public class PrecastUGirderSection : SectionGeometryInfo
    {
        public double B1 { get; set; }
        public double B2 { get; set; }
        public double B3 { get; set; }
        public double B4 { get; set; }
        public double B5 { get; set; }
        public double B6 { get; set; }
        public double D1 { get; set; }
        public double D2 { get; set; }
        public double D3 { get; set; }
        public double D4 { get; set; }
        public double D5 { get; set; }
        public double D6 { get; set; }
        public double D7 { get; set; }

        public PrecastUGirderSection(
            double b1, double b2, double b3, double b4, double b5, double b6,
            double d1, double d2, double d3, double d4, double d5, double d6, double d7)
        {
            this.B1 = b1;
            this.B2 = b2;
            this.B3 = b3;
            this.B4 = b4;
            this.B5 = b5;
            this.B6 = b6;
            this.D1 = d1;
            this.D2 = d2;
            this.D3 = d3;
            this.D4 = d4;
            this.D5 = d5;
            this.D6 = d6;
            this.D7 = d7;
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
                { "D1", D1 },
                { "D2", D2 },
                { "D3", D3 },
                { "D4", D4 },
                { "D5", D5 },
                { "D6", D6 },
                { "D7", D7 }
            };
        }

        public override string ToString()
        {
            return $"Precast U Girder - B1: {B1}, B2: {B2}, B3: {B3}, D1: {D1}, D2: {D2}, D3: {D3}, D4: {D4}";
        }
    }
}
