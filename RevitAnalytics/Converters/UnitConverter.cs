using Autodesk.Revit.DB;
using System;

namespace RevitAnalytics.Converters
{
    public static class UnitConverter
    {
        public static XYZ ConvertPointFromFeetToMeters(XYZ point)
        {
            double x = UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Meters);
            double y = UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Meters);
            double z = UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Meters);
            return new XYZ(x, y, z);
        }

        public static XYZ ConvertPointFromMetersToFeet(XYZ point)
        {
            double x = UnitUtils.ConvertToInternalUnits(point.X, UnitTypeId.Meters);
            double y = UnitUtils.ConvertToInternalUnits(point.Y, UnitTypeId.Meters);
            double z = UnitUtils.ConvertToInternalUnits(point.Z, UnitTypeId.Meters);
            return new XYZ(x, y, z);
        }

        public static double ConvertNewtonPerFootMeterToKNPerM2(double value)
        {
            return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.NewtonsPerSquareMeter) / 1000;
        }

        public static double ConvertKNPerM2ToNewtonPerFootMeter(double value)
        {
            return UnitUtils.ConvertToInternalUnits(value * 1000, UnitTypeId.NewtonsPerSquareMeter);
        }
        public static double ConvertkgFt3ToKNM3(double value)
        {
            return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.KilogramsPerCubicMeter) /1000 * 9.80665;
        }
        public static double ConvertKNM3ToKgFt3(double value)
        {
            return UnitUtils.ConvertToInternalUnits(value / 9.80665 * 1000, UnitTypeId.KilogramsPerCubicMeter);
        }
        public static double ConvertNewtonPerFootMeterToMPA(double value)
        {
            return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Megapascals);
        }

        public static double ConvertToKgPerM3(double value)
        {
            return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.KilogramsPerCubicMeter);
        }
    }
}


