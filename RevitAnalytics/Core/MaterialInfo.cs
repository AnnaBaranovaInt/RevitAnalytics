using Autodesk.Revit.UI;
using System;
using System.Drawing;
using System.Windows.Media;

namespace RevitAnalytics
{
    public enum IsotropyType
    {
        Isotropic,
        TransverselyIsotropic,
        Orthotropic
    }

    public class MaterialInfo
    {
        public string Name { get; set; }
        public MaterialType Type { get; set; }
        public IsotropyType Isotropy { get; set; }

        public double Density { get; set; }

        // **Poisson Ratios (For Anisotropic Materials)**
        public double PoissonRatioXY { get; set; }
        public double PoissonRatioXZ { get; set; }
        public double PoissonRatioYZ { get; set; }

        // **Young’s Modulus (x, y, z directions)**
        public double YoungModulusX { get; set; } //KN/m2
        public double YoungModulusY { get; set; } //KN/m2
        public double YoungModulusZ { get; set; }   //KN/m2

        // **Shear Modulus (xy, yz, xz directions)**
        public double ShearModulusXY { get; set; } //KN/m2
        public double ShearModulusYZ { get; set; } //KN/m2
        public double ShearModulusXZ { get; set; } //KN/m2

        // **Thermal Expansion Coefficients**
        public double ThermalExpansionCoeffX { get; set; } // 1/°C
        public double ThermalExpansionCoeffY { get; set; } // 1/°C
        public double ThermalExpansionCoeffZ { get; set; } // 1/°C

        public double MinimumYieldStress { get; set; } //KN/m2
        public double MinimumTensileStrength { get; set; } //KN/m2

        // **Concrete Properties**
        public double ConcreteCompression { get; set; } //KN/m2
        public double ExpectedCompressiveStrength { get; set; } //KN/m2
        public double ConcreteShearReinforcement { get; set; } //KN/m2 BUT DOES NOT EXIST IN UI IN REVIT HAVE NO IDEA WHAT IT IS
        public double ConcreteShearStrengthReduction { get; set; } //CHECK THIS IN UI IN REVIT
        public bool IsLightweight { get; set; }

        // **Metal Properties**
        public double MetalResistanceCalcStrength { get; set; } //KN/m2 BUT DOES NOT EXIST IN UI IN REVIT HAVE NO IDEA WHAT IT IS (SOMEHOW 0.07% smaller than the original value - Min Yield Stress)
        public double MetalReductionFactor { get; set; } //???????? DOESNT EXIST IN UI IN REVIT
        public bool MetalThermallyTreated { get; set; }


        // Common SAP2000 Properties
        public int StressStrainCurveType { get; set; } = 1; // Default: Parametric - Simple // 0 -USer defines // 2 - Mander
        public int StressStrainHysteresisType { get; set; } = 0; // Default: Elastic //  1- Kinematic  // 2 - Takeda
        public double DefaultTemperature { get; set; } = 0; // Default: 0°C

        //FUN SAP2000 Properties
        public Color Color { get; set; }
        public string Notes { get; set; }
        public string GUID { get; set; }

        // 🏗️ Concrete-Specific SAP2000 Properties
        public double StrainAtFc { get; set; } = 0.002; // Default strain at fc
        public double StrainUltimate { get; set; } = 0.0035; // Default ultimate strain
        public double FrictionAngle { get; set; } = 30.0; // Default Drucker-Prager friction angle
        public double DilatationalAngle { get; set; } = 10.0; // Default dilatation angle
        public double ExpectedTensileStress { get; set; }  // Default shear strength reduction factor

        // New properties for steel behavior
        public double StrainAtHardening { get; set; } = 0.002; // Strain at onset of strain hardening
        public double StrainAtMaxStress { get; set; } = 0.02; // Strain at maximum stress
        public double StrainAtRupture { get; set; } = 0.1; // Strain at rupture
        public double FinalSlope { get; set; } = 0.01 * 200_000; // Default: 1% of Young’s modulus. Slope multiplier on Young’s modulus
        public double ExpectedYieldStress { get; set; } 


        // ✅ **Constructor with full anisotropic Poisson ratios**
        public MaterialInfo(
            string name, MaterialType type, IsotropyType isotropyType,
            double youngModulusX, double youngModulusY, double youngModulusZ,
            double shearModulusXY, double shearModulusYZ, double shearModulusXZ,
            double poissonRatioXY, double poissonRatioXZ, double poissonRatioYZ, double density,
            double expansionCoeffX, double expansionCoeffY, double expansionCoeffZ,
            double minimumYieldStress, double minimumTensileStrength,
            double concreteCompression, double expectedCompressiveStrength,
            double concreteShearReinforcement, double concreteShearStrengthReduction,
            double metalResistanceCalcStrength, double metalReductionFactor, bool metalThermallyTreated,
            bool isLightweight, double defaultTemperature = 0)
        {
            Name = name;
            Type = type;
            Isotropy = isotropyType;

            YoungModulusX = youngModulusX; 
            YoungModulusY = youngModulusY;
            YoungModulusZ = youngModulusZ;

            ShearModulusXY = shearModulusXY;
            ShearModulusYZ = shearModulusYZ;
            ShearModulusXZ = shearModulusXZ;

            PoissonRatioXY = poissonRatioXY;
            PoissonRatioXZ = poissonRatioXZ;
            PoissonRatioYZ = poissonRatioYZ;
            Density = density;

            ThermalExpansionCoeffX = expansionCoeffX;
            ThermalExpansionCoeffY = expansionCoeffY;
            ThermalExpansionCoeffZ = expansionCoeffZ;

            MinimumYieldStress = minimumYieldStress;
            MinimumTensileStrength = minimumTensileStrength;

            ConcreteCompression = concreteCompression;
            ExpectedCompressiveStrength = expectedCompressiveStrength;
            ConcreteShearReinforcement = concreteShearReinforcement;
            ConcreteShearStrengthReduction = concreteShearStrengthReduction;

            MetalResistanceCalcStrength = metalResistanceCalcStrength;
            MetalReductionFactor = metalReductionFactor;
            MetalThermallyTreated = metalThermallyTreated;

            IsLightweight = isLightweight;

            DefaultTemperature = defaultTemperature;
        }

        public MaterialInfo(
            string name, MaterialType type,
            double youngModulusX, 
            double shearModulusXY, 
            double poissonRatioXY,
            double density)
        {
            Name = name;
            Type = type;
            Isotropy = IsotropyType.Isotropic;

            YoungModulusX = YoungModulusY = YoungModulusZ= youngModulusX;

            ShearModulusXY = ShearModulusXZ = ShearModulusYZ = shearModulusXY;

            PoissonRatioXY = PoissonRatioXZ = PoissonRatioYZ = poissonRatioXY;
            Density = density;
        }
        public MaterialInfo(
            string name, MaterialType type)
        {
            Name = name;
            Type = type;
        }
        // **🔹 Concrete Constructor**
        public MaterialInfo(
            string name, IsotropyType isotropyType,
            double youngModulusX, double youngModulusY, double youngModulusZ,
            double shearModulusXY, double shearModulusYZ, double shearModulusXZ,
            double poissonRatioXY, double poissonRatioXZ, double poissonRatioYZ, double density,
            double expansionCoeffX, double expansionCoeffY, double expansionCoeffZ,
            double minimumYieldStress, double minimumTensileStrength,
            double concreteCompression, 
            double concreteShearReinforcement, double concreteShearStrengthReduction,
            bool isLightweight, double expectedCompressiveStrength = 0, double expectedTensileStress = 0,
            int stressStrainType = 1, int stressStrainHysteresisType = 0,
            double strainAtFc = 0.002, double strainUltimate = 0.0035,
            double frictionAngle = 30.0, double dilatationalAngle = 10.0, double defaultTemperature = 0)
        {
            Name = name;
            Type = MaterialType.CONCRETE;
            Isotropy = isotropyType;

            YoungModulusX = youngModulusX;
            YoungModulusY = youngModulusY;
            YoungModulusZ = youngModulusZ;

            ShearModulusXY = shearModulusXY;
            ShearModulusYZ = shearModulusYZ;
            ShearModulusXZ = shearModulusXZ;

            PoissonRatioXY = poissonRatioXY;
            PoissonRatioXZ = poissonRatioXZ;
            PoissonRatioYZ = poissonRatioYZ;
            Density = density;

            ThermalExpansionCoeffX = expansionCoeffX;
            ThermalExpansionCoeffY = expansionCoeffY;
            ThermalExpansionCoeffZ = expansionCoeffZ;

            MinimumYieldStress = minimumYieldStress;
            MinimumTensileStrength = minimumTensileStrength;

            ConcreteCompression = concreteCompression;
            if (expectedCompressiveStrength == 0)
            {
                expectedCompressiveStrength = concreteCompression;
            }
            ExpectedCompressiveStrength = expectedCompressiveStrength;
            ConcreteShearReinforcement = concreteShearReinforcement;
            ConcreteShearStrengthReduction = concreteShearStrengthReduction;
            IsLightweight = isLightweight;

            StressStrainCurveType = stressStrainType;
            StressStrainHysteresisType = stressStrainHysteresisType;
            StrainAtFc = strainAtFc;
            StrainUltimate = strainUltimate;
            FrictionAngle = frictionAngle;
            DilatationalAngle = dilatationalAngle;
            if (expectedTensileStress == 0)
            {
                expectedTensileStress = 0.1 * minimumTensileStrength; ///!!!!!!!!!!!!!!
            }
            ExpectedTensileStress = expectedTensileStress;

            DefaultTemperature = defaultTemperature;
        }

        // **🔹 Steel Constructor**
        public MaterialInfo(
            string name, IsotropyType isotropyType,
            double youngModulusX, double youngModulusY, double youngModulusZ,
            double shearModulusXY, double shearModulusYZ, double shearModulusXZ,
            double poissonRatioXY, double poissonRatioXZ, double poissonRatioYZ, double density,
            double expansionCoeffX, double expansionCoeffY, double expansionCoeffZ,
            double minimumYieldStress, double minimumTensileStrength,
            double metalResistanceCalcStrength, double metalReductionFactor, bool metalThermallyTreated,
            double expectedYieldStress = 0, int stressStrainType = 1, int stressStrainHysteresisType = 0,
            double strainAtHardening = 0.002, double strainAtMaxStress = 0.02,
            double strainAtRupture = 0.1, double finalSlope = 0.01 * 200_000_000, double defaultTemperature = 0)
        {
            Name = name;
            Type = MaterialType.STEEL;
            Isotropy = isotropyType;

            YoungModulusX = youngModulusX;
            YoungModulusY = youngModulusY;
            YoungModulusZ = youngModulusZ;

            ShearModulusXY = shearModulusXY;
            ShearModulusYZ = shearModulusYZ;
            ShearModulusXZ = shearModulusXZ;

            PoissonRatioXY = poissonRatioXY;
            PoissonRatioXZ = poissonRatioXZ;
            PoissonRatioYZ = poissonRatioYZ;
            Density = density;

            ThermalExpansionCoeffX = expansionCoeffX;
            ThermalExpansionCoeffY = expansionCoeffY;
            ThermalExpansionCoeffZ = expansionCoeffZ;

            MinimumYieldStress = minimumYieldStress;
            MinimumTensileStrength = minimumTensileStrength;
            MetalResistanceCalcStrength = metalResistanceCalcStrength;
            MetalReductionFactor = metalReductionFactor;
            MetalThermallyTreated = metalThermallyTreated;
            if (expectedYieldStress == 0)
            {
                expectedYieldStress = 1.1 * minimumYieldStress; ///!!!!!!!!!!!!!!
            }
            ExpectedYieldStress = expectedYieldStress;
            StressStrainCurveType = stressStrainType;
            StressStrainHysteresisType = stressStrainHysteresisType;
            StrainAtHardening = strainAtHardening;
            StrainAtMaxStress = strainAtMaxStress;
            StrainAtRupture = strainAtRupture;
            FinalSlope = finalSlope;

            DefaultTemperature = defaultTemperature;
        }


        // ✅ **Base Concrete Constructor with updated Poisson ratios**
        public static MaterialInfo CreateBaseConcrete(string name = "Concrete")
        {
            return new MaterialInfo(
                name, IsotropyType.Isotropic,
                youngModulusX: 30_000 * 1000, youngModulusY: 25_000 * 1000, youngModulusZ: 25_000 * 1000, // kN/m²
                shearModulusXY: 12_000 * 1000, shearModulusYZ: 10_000 * 1000, shearModulusXZ: 10_000 * 1000, // kN/m²
                poissonRatioXY: 0.2, poissonRatioXZ: 0.2, poissonRatioYZ: 0.2, // Isotropic values
                density: 2400 * 0.00981, // kg/m³ → kN/m³
                expansionCoeffX: 0.000010, expansionCoeffY: 0.000008, expansionCoeffZ: 0.000008, // Thermal Exp Coeff
                minimumYieldStress: 40 * 1000, minimumTensileStrength: 3 * 1000, // kN/m²
                concreteCompression: 40 * 1000,
                concreteShearReinforcement: 126 * 1000,
                concreteShearStrengthReduction: 1, // Shear strength reduction factor
                isLightweight: false,

                // **Optional Parameters (Handled in Constructor Defaults)**
                expectedCompressiveStrength: 40 * 1000,  // Defaults to `concreteCompression`
                expectedTensileStress: 3 * 1000,  // Defaults to `0.1 * minimumTensileStrength`

                // **SAP2000 Concrete Parameters**
                stressStrainType: 1,  // Parametric - Simple
                stressStrainHysteresisType: 0,  // Elastic
                strainAtFc: 0.002,  // Default unconfined strain at fc
                strainUltimate: 0.0035,  // Ultimate strain
                frictionAngle: 30.0,  // Drucker-Prager friction angle
                dilatationalAngle: 10.0  // Dilatational angle
            );
        }


        // ✅ **Base Steel Constructor with anisotropic Poisson ratios**
        public static MaterialInfo CreateBaseSteel(string name = "Steel")
        {
            return new MaterialInfo(
                name, IsotropyType.Isotropic,
                youngModulusX: 200_000 * 1000, youngModulusY: 200_000 * 1000, youngModulusZ: 200_000 * 1000, // kN/m²
                shearModulusXY: 77_000 * 1000, shearModulusYZ: 77_000 * 1000, shearModulusXZ: 77_000 * 1000, // kN/m²
                poissonRatioXY: 0.3, poissonRatioXZ: 0.3, poissonRatioYZ: 0.3, // Isotropic values
                density: 7850 * 0.00981, // kg/m³ → kN/m³
                expansionCoeffX: 0.000012, expansionCoeffY: 0.000012, expansionCoeffZ: 0.000012, // Thermal Exp Coeff
                minimumYieldStress: 345 * 1000, minimumTensileStrength: 450 * 1000, // kN/m²

                // **Steel-Specific Properties**
                metalResistanceCalcStrength: 105 * 1000,
                metalReductionFactor: 1.66,
                metalThermallyTreated: true,

                // **Optional SAP2000 Parameters**
                expectedYieldStress: 450 * 1000,  // Defaults to `1.1 * minimumYieldStress`
                stressStrainType: 1,  // Parametric - Simple
                stressStrainHysteresisType: 0,  // Elastic
                strainAtHardening: 0.002,  // Default strain at hardening onset
                strainAtMaxStress: 0.02,  // Strain at max stress
                strainAtRupture: 0.1,  // Strain at rupture
                finalSlope: 0.01 * 200_000 * 1000  // Slope multiplier on Young’s modulus
            );
        }


        public static void LogMaterialInfo(MaterialInfo materialInfo)
        {
            DebugHandler.Log($"✅ [Material: {materialInfo.Name}] - Extracted Properties:");

            DebugHandler.Log($@"
                📌 General Properties:
                - Type: {materialInfo.Type}
                - Isotropy: {materialInfo.Isotropy}
                - Density: {materialInfo.Density} kN/m³, {(materialInfo.Density) / 9.80665 * 1000} kg/m³

                🔍 Mechanical Properties:
                - Young Modulus (X, Y, Z): {materialInfo.YoungModulusX} kN/m² ({materialInfo.YoungModulusX / 1000} MPa)
                - Shear Modulus (XY, YZ, XZ): {materialInfo.ShearModulusXY} kN/m² ({materialInfo.ShearModulusXY / 1000} MPa)
                - Poisson Ratio (XY, XZ, YZ): {materialInfo.PoissonRatioXY}, {materialInfo.PoissonRatioXZ}, {materialInfo.PoissonRatioYZ}
                - Thermal Expansion Coeff (X, Y, Z): {materialInfo.ThermalExpansionCoeffX}, {materialInfo.ThermalExpansionCoeffY}, {materialInfo.ThermalExpansionCoeffZ} 1/K

                🔍 Strength Properties:
                - Min Yield Stress: {materialInfo.MinimumYieldStress} kN/m² ({materialInfo.MinimumYieldStress / 1000} MPa)
                - Min Tensile Strength: {materialInfo.MinimumTensileStrength} kN/m² ({materialInfo.MinimumTensileStrength / 1000} MPa)
            ");

            if (materialInfo.Type == MaterialType.CONCRETE)
            {
                DebugHandler.Log($@"
            🏗️ Concrete-Specific Properties:
            - Compression Strength: {materialInfo.ConcreteCompression} kN/m² ({materialInfo.ConcreteCompression / 1000} MPa)
            - Expected Compressive Strength: {materialInfo.ExpectedCompressiveStrength} kN/m² ({materialInfo.ExpectedCompressiveStrength / 1000} MPa)
            - Shear Reinforcement: {materialInfo.ConcreteShearReinforcement} kN/m² ({materialInfo.ConcreteShearReinforcement / 1000} MPa)
            - Shear Strength Reduction Factor: {materialInfo.ConcreteShearStrengthReduction}
            - Lightweight: {materialInfo.IsLightweight}
            - Stress-Strain Type: {materialInfo.StressStrainCurveType}
            - Stress-Strain Hysteresis Type: {materialInfo.StressStrainHysteresisType}
            - Strain at Fc: {materialInfo.StrainAtFc}
            - Ultimate Strain: {materialInfo.StrainUltimate}
            - Friction Angle: {materialInfo.FrictionAngle}
            - Dilatational Angle: {materialInfo.DilatationalAngle}
        ");
            }
            else if (materialInfo.Type == MaterialType.STEEL)
            {
                DebugHandler.Log($@"
            🔩 Steel-Specific Properties:
            - Metal Resistance Calculation Strength: {materialInfo.MetalResistanceCalcStrength} kN/m² ({materialInfo.MetalResistanceCalcStrength / 1000} MPa)
            - Metal Reduction Factor: {materialInfo.MetalReductionFactor}
            - Thermally Treated: {materialInfo.MetalThermallyTreated}
            - Stress-Strain Type: {materialInfo.StressStrainCurveType}
            - Stress-Strain Hysteresis Type: {materialInfo.StressStrainHysteresisType}
            - Strain at Hardening: {materialInfo.StrainAtHardening}
            - Strain at Max Stress: {materialInfo.StrainAtMaxStress}
            - Strain at Rupture: {materialInfo.StrainAtRupture}
            - Final Slope: {materialInfo.FinalSlope}
            - Expected Yield Strength: {materialInfo.ExpectedYieldStress} kN/m² ({materialInfo.ExpectedYieldStress / 1000} MPa)
            - Expected Tensile Stress: {materialInfo.ExpectedTensileStress} kN/m
        ");
            }
        }
    }
}
