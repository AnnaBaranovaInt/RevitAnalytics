using Autodesk.Revit.UI;
using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Autodesk.Revit.DB;

namespace RevitAnalytics.SAPConnector.Utils
{
    class MaterialUtils
    {
        public static void CreateOrUpdateSapMaterial(cSapModel sapModel, MaterialInfo materialInfo)
        {
            if (sapModel == null || materialInfo == null)
            {
                DebugHandler.LogWarning("SAP2000 model or MaterialInfo is null. Cannot proceed.");
                TaskDialog.Show("Error", "SAP2000 model or MaterialInfo is null. Cannot proceed.");
                return;
            }

            // Step 1️⃣: Check if the material already exists
            bool materialExists = MaterialUtils.DoesMaterialExist(sapModel, materialInfo.Name);
            if (materialExists)
            {
                DebugHandler.Log($"Material '{materialInfo.Name}' already exists in SAP2000. Updating properties.");
            }
            else
            {
                // Step 2️⃣: Create the material
                int ret = sapModel.PropMaterial.SetMaterial(materialInfo.Name, (eMatType)materialInfo.Type, -1, "");
                if (ret != 0)
                {
                    DebugHandler.LogWarning($"Failed to create material '{materialInfo.Name}' in SAP2000. Error Code: {ret}");
                    TaskDialog.Show("Error", $"Failed to create material '{materialInfo.Name}' in SAP2000. Error Code: {ret}");
                    return;
                }
                DebugHandler.Log($"✅ Created new material '{materialInfo.Name}' in SAP2000.");
            }

            // Step 3️⃣: Assign mechanical properties based on isotropy
            if (materialInfo.Isotropy == IsotropyType.Isotropic)
            {
                sapModel.PropMaterial.SetMPIsotropic(
                    materialInfo.Name,                        // Name
                    materialInfo.YoungModulusX,               // E (Elastic Modulus, assuming isotropic)
                    materialInfo.PoissonRatioXY,              // U (Poisson's Ratio)
                    materialInfo.ThermalExpansionCoeffX       // A (Thermal Coefficient)
                                                              // Temp is optional and defaults to 0
                );

                DebugHandler.Log($"Updated isotropic properties for '{materialInfo.Name}' (E={materialInfo.YoungModulusX}, ν={materialInfo.PoissonRatioXY}, G={materialInfo.ShearModulusXY})");
            }
            else if (materialInfo.Isotropy == IsotropyType.Orthotropic)
            {
                double[] E = { materialInfo.YoungModulusX, materialInfo.YoungModulusY, materialInfo.YoungModulusZ };
                double[] U = { materialInfo.PoissonRatioXY, materialInfo.PoissonRatioXZ, materialInfo.PoissonRatioYZ };
                double[] A = { materialInfo.ThermalExpansionCoeffX, materialInfo.ThermalExpansionCoeffY, materialInfo.ThermalExpansionCoeffZ };
                double[] G = { materialInfo.ShearModulusXY, materialInfo.ShearModulusYZ, materialInfo.ShearModulusXZ };

                int result = sapModel.PropMaterial.SetMPOrthotropic(
                    materialInfo.Name,
                    ref E, ref U, ref A, ref G,
                    0 // Default temperature
                );

                DebugHandler.Log($"Updated orthotropic properties for '{materialInfo.Name}' (E={materialInfo.YoungModulusX}, {materialInfo.YoungModulusY}, {materialInfo.YoungModulusZ})");
            }
            else if (materialInfo.Isotropy == IsotropyType.TransverselyIsotropic)
            {
                // 🌟 Approximating as an Orthotropic Material 🌟
                // Assuming transverse direction is Z (change if necessary)
                //LOG IT the direction and assuming it is Z
                DebugHandler.LogWarning($"Transversely Isotropic material '{materialInfo.Name}' is approximated as Orthotropic in z direction. Change in the code if needed.");

                double[] E = { materialInfo.YoungModulusX, materialInfo.YoungModulusX, materialInfo.YoungModulusZ }; // E1 = E2 ≠ E3
                double[] U = { materialInfo.PoissonRatioXY, materialInfo.PoissonRatioXZ, materialInfo.PoissonRatioXZ }; // ν12 = ν13 ≠ ν23
                double[] A = { materialInfo.ThermalExpansionCoeffX, materialInfo.ThermalExpansionCoeffX, materialInfo.ThermalExpansionCoeffZ }; // A1 = A2 ≠ A3
                double[] G = { materialInfo.ShearModulusXY, materialInfo.ShearModulusXZ, materialInfo.ShearModulusXZ }; // G12 ≠ G13 ≠ G23

                int result = sapModel.PropMaterial.SetMPOrthotropic(
                    materialInfo.Name,
                    ref E, ref U, ref A, ref G,
                    0 // Default temperature
                );

                DebugHandler.Log($"🔹 Transversely Isotropic '{materialInfo.Name}' assigned as Orthotropic (E1=E2={materialInfo.YoungModulusX}, E3={materialInfo.YoungModulusZ})");
            }

            // Step 4️⃣: Assign Weight & Mass (based on Density)
            int myOption = 1; // Use Weight per Unit Volume (KN/m³)
            double densityValue = materialInfo.Density; // Already in KN/m³

            int resultDensity = sapModel.PropMaterial.SetWeightAndMass(
                materialInfo.Name,  // Material Name
                myOption,           // 1 = Weight per unit volume (KN/m³)
                densityValue,       // The actual value of density
                materialInfo.DefaultTemperature   // Default temperature (Temp is optional)
            );

            if (resultDensity == 0)
            {
                DebugHandler.Log($"✅ Assigned weight (density) for '{materialInfo.Name}' → {densityValue} KN/m³");
            }
            else
            {
                DebugHandler.Log($"⚠ ERROR: Failed to assign weight for '{materialInfo.Name}' (Error Code: {resultDensity})", DebugHandler.LogLevel.ERROR);
            }

            // Step 5️⃣: Assign material-specific properties
            // Step 5️⃣: Assign material-specific properties for concrete
            if (materialInfo.Type == MaterialType.CONCRETE)
            {
                int resultConcrete = sapModel.PropMaterial.SetOConcrete(
                    materialInfo.Name,                           // Name
                    materialInfo.ConcreteCompression,           // Fc (Concrete compressive strength)
                    materialInfo.IsLightweight,                 // IsLightweight
                    materialInfo.ConcreteShearStrengthReduction,// FcsFactor (Shear strength reduction factor)
                    materialInfo.StressStrainCurveType,         // SSType (Stress-strain curve type)
                    materialInfo.StressStrainHysteresisType,    // SSHysType (Hysteresis type)
                    materialInfo.StrainAtFc,                    // StrainAtFc (Strain at unconfined compressive strength)
                    materialInfo.StrainUltimate,                // StrainUltimate (Ultimate unconfined strain)
                    materialInfo.FrictionAngle,                 // Friction Angle (Drucker-Prager friction angle)
                    materialInfo.DilatationalAngle,             // Dilatational Angle (Drucker-Prager dilatational angle)
                    materialInfo.DefaultTemperature                                           // Temp (default to 0)
                );

                if (resultConcrete == 0)
                {
                    DebugHandler.Log($"✅ Successfully assigned concrete properties for '{materialInfo.Name}'");
                }
                else
                {
                    DebugHandler.Log($"⚠️ Failed to assign concrete properties for '{materialInfo.Name}', SAP2000 Error Code: {resultConcrete}");
                }
            }
            else if (materialInfo.Type == MaterialType.STEEL)
            {
                bool IsValidNumber(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

                if (!IsValidNumber(materialInfo.MinimumYieldStress) ||
                    !IsValidNumber(materialInfo.MinimumTensileStrength) ||
                    !IsValidNumber(materialInfo.ExpectedYieldStress) ||
                    !IsValidNumber(materialInfo.ExpectedTensileStress))
                {
                    DebugHandler.Log("⚠ ERROR: One or more steel property values are invalid (NaN or Infinity). Skipping material assignment.", DebugHandler.LogLevel.ERROR);
                    return;
                }
                // Assign steel properties using SetOSteel
                int resultMetal = sapModel.PropMaterial.SetOSteel(
                        materialInfo.Name,
                        materialInfo.MinimumYieldStress,   // Fy
                        materialInfo.MinimumTensileStrength, // Fu
                        materialInfo.ExpectedYieldStress, // eFy
                        materialInfo.ExpectedTensileStress, // eFu
                        materialInfo.StressStrainCurveType, // SSType (Parametric - Simple)
                        materialInfo.StressStrainHysteresisType, // SSHysType (Elastic)
                        materialInfo.StrainAtHardening,  // StrainAtHardening
                        materialInfo.StrainAtMaxStress,  // StrainAtMaxStress
                        materialInfo.StrainAtRupture ,   // StrainAtRupture
                        materialInfo.DefaultTemperature // Temp (default to 0)
                    );

                    if (resultMetal == 0)
                    {
                        DebugHandler.Log($"✅ Successfully assigned steel properties to '{materialInfo.Name}' (Fy={materialInfo.MinimumYieldStress}, Fu={materialInfo.MinimumTensileStrength})", DebugHandler.LogLevel.INFO);
                    }
                    else
                    {
                        DebugHandler.Log($"⚠ ERROR: Failed to assign steel properties for '{materialInfo.Name}'. Error Code: somewhere in steel command", DebugHandler.LogLevel.ERROR);
                    }

                MaterialInfo.LogMaterialInfo(materialInfo);
            }


            DebugHandler.Log($"🎯 SAP2000 Material '{materialInfo.Name}' fully created/updated.");
        }
        public static bool DoesMaterialExist(cSapModel sapModel, string materialName)
        {
            int numberOfMaterials = 0;
            string[] materialNames = null;

            // Retrieve the list of all material names in SAP2000
            sapModel.PropMaterial.GetNameList(ref numberOfMaterials, ref materialNames);

            //// Log existing materials for debugging
            //foreach (string existingMaterial in materialNames)
            //{
            //    DebugHandler.Log($"Existing material in SAP2000: {existingMaterial}", DebugHandler.LogLevel.INFO);
            //}

            // Check if the material exists
            return materialNames.Contains(materialName, StringComparer.InvariantCultureIgnoreCase);
        }

        public static string GetMaterialName(cSapModel sapModel, string label)
        {
            string sectionName = "";
            string sAuto = ""; // Additional required parameter for GetSection()

            // Get section assigned to the frame
            int result = sapModel.FrameObj.GetSection(label, ref sectionName, ref sAuto);

            if (result != 0 || string.IsNullOrEmpty(sectionName))
            {
                DebugHandler.Log($"⚠ WARNING: No section found for frame {label}. Error Code: {result}", DebugHandler.LogLevel.WARNING);
                return "UnknownMaterial";
            }

            string materialName = "";
            result = sapModel.PropFrame.GetMaterial(sectionName, ref materialName); // Get material of the section

            if (result == 0 && !string.IsNullOrEmpty(materialName))
            {
                DebugHandler.Log($"✅ Material for {label}: {materialName}.", DebugHandler.LogLevel.INFO);
                return materialName;
            }

            DebugHandler.Log($"⚠ WARNING: No material found for section {sectionName} (Frame {label}). Error Code: {result}", DebugHandler.LogLevel.WARNING);
            return "UnknownMaterial";
        }
        public static MaterialType ConvertToMaterialType(string materialTypeString)
        {
            if (Enum.TryParse(materialTypeString, true, out MaterialType materialType))
            {
                return materialType;
            }

            DebugHandler.Log($"⚠ WARNING: Unrecognized material type '{materialTypeString}', defaulting to NODESIGN.", DebugHandler.LogLevel.WARNING);
            return MaterialType.NODESIGN; // Default value if conversion fails
        }


        /// <summary>
        /// Retrieves the material type assigned to a frame element in SAP2000.
        /// </summary>
        public static string GetMaterialType(cSapModel sapModel, string materialName)
        {
            eMatType matType = eMatType.NoDesign; // Default to NoDesign
            int symType = 0;

            int result = sapModel.PropMaterial.GetTypeOAPI(materialName, ref matType, ref symType);

            if (result == 0)
            {
                DebugHandler.Log($"✅ Retrieved matType {matType} ({(int)matType}) for {materialName}.", DebugHandler.LogLevel.INFO);

                // 🔄 Explicitly map eMatType to MaterialType
                MaterialType mappedType = MaterialType.NODESIGN;
                switch (matType)
                {
                    case eMatType.Concrete:
                        mappedType = MaterialType.CONCRETE;
                        break;
                    case eMatType.Steel:
                        mappedType = MaterialType.STEEL;
                        break;
                    case eMatType.NoDesign:
                        mappedType = MaterialType.NODESIGN;
                        break;
                    default:
                        DebugHandler.Log($"❌ Unknown eMatType: {matType} ({(int)matType}) for {materialName}", DebugHandler.LogLevel.ERROR);
                        return "UnknownType";
                }

                return mappedType.ToString(); // Now we return an explicitly mapped type
            }
            else
            {
                DebugHandler.Log($"❌ Failed to retrieve material type for {materialName}. Error Code: Material Type", DebugHandler.LogLevel.ERROR);
                return "UnknownType";
            }
        }



        public static List<MaterialInfo> GetAllMaterialInfoFromSAP2000(cSapModel sapModel)
        {
            List<MaterialInfo> materialList = new List<MaterialInfo>();

            // 🔍 Step 1: Get all material names from SAP2000
            int numMaterials = 0;
            string[] materialNames = new string[1];
            int ret = sapModel.PropMaterial.GetNameList(ref numMaterials, ref materialNames);

            if (ret != 0 || materialNames.Length == 0)
            {
                DebugHandler.Log("⚠ WARNING: No materials found in SAP2000!", DebugHandler.LogLevel.WARNING);
                return materialList;
            }

            DebugHandler.Log($"✅ Found {numMaterials} materials in SAP2000.");

            // 🔍 Step 2: Iterate through each material and retrieve properties
            foreach (string materialName in materialNames)
            {
                eMatType matType = eMatType.NoDesign;
                int symType = 0;

                // Step 2.1: Get Material Type & Density
                ret = sapModel.PropMaterial.GetTypeOAPI(materialName, ref matType, ref symType);
                if (ret != 0)
                {
                    DebugHandler.Log($"⚠ WARNING: Could not retrieve type for material '{materialName}'", DebugHandler.LogLevel.WARNING);
                    continue;
                }

                // Step 2.2: Determine Material Type (Switch Statement Instead of Expression)
                MaterialType materialType = MaterialType.NODESIGN;
                switch (matType)
                {
                    case eMatType.Concrete:
                        materialType = MaterialType.CONCRETE;
                        break;
                    case eMatType.Steel:
                        materialType = MaterialType.STEEL;
                        break;
                    default:
                        materialType = MaterialType.NODESIGN;
                        break;
                }

                // Step 2.3: Determine Isotropy Type
                IsotropyType isotropyType = IsotropyType.Isotropic;
                switch (symType)
                {
                    case 0:
                        isotropyType = IsotropyType.Isotropic;
                        break;
                    case 1:
                        isotropyType = IsotropyType.Orthotropic;
                        break;
                }

                // Step 2.4: Create MaterialInfo object
                MaterialInfo materialInfo = new MaterialInfo(materialName, materialType)
                {
                    Isotropy = isotropyType,
                    Type = materialType
                };

                // Step 2.5: Retrieve Basic Material Properties
                int color = 0;
                string notes = "", guid = "";
                ret = sapModel.PropMaterial.GetMaterial(materialName, ref matType, ref color, ref notes, ref guid);

                if (ret == 0)
                {
                    materialInfo.Color = ExtractColorFromSAP(color);
                    materialInfo.Notes = notes;
                    materialInfo.GUID = guid;
                }
                else
                {
                    DebugHandler.Log($"⚠ WARNING: Could not retrieve full material properties for '{materialName}'", DebugHandler.LogLevel.WARNING);
                }

                // Step 2.6: Get Material Density
                materialInfo.Density = GetMaterialDensity(sapModel, materialName);
                // Step 3: Get Elastic Properties based on Isotropy Type
                if (isotropyType == IsotropyType.Isotropic)
                {
                    GetIsotropicProperties(sapModel, materialName, ref materialInfo);
                }
                else if (isotropyType == IsotropyType.Orthotropic)
                {
                    GetOrthotropicProperties(sapModel, materialName, ref materialInfo);
                }

                // Step 4: Retrieve Specific Properties Based on Material Type
                if (materialType == MaterialType.CONCRETE)
                {
                    GetConcreteProperties(sapModel, materialName, ref materialInfo);
                }
                else if (materialType == MaterialType.STEEL)
                {
                    GetSteelProperties(sapModel, materialName, ref materialInfo);
                }

                // Step 5: Add Material to List
                materialList.Add(materialInfo);
            }

            return materialList;
        }
        public static MaterialInfo GetMaterialInfoByName(cSapModel sapModel, string materialName)
        {
            eMatType matType = eMatType.NoDesign;
            int symType = 0;

            // Step 1: Get Material Type & Symmetry
            int ret = sapModel.PropMaterial.GetTypeOAPI(materialName, ref matType, ref symType);
            if (ret != 0)
            {
                DebugHandler.Log($"⚠ WARNING: Could not retrieve type for material '{materialName}'", DebugHandler.LogLevel.WARNING);
                return null;
            }

            // Step 2: Determine Material Type
            MaterialType materialType = MaterialType.NODESIGN;
            switch (matType)
            {
                case eMatType.Concrete:
                    materialType = MaterialType.CONCRETE;
                    break;
                case eMatType.Steel:
                    materialType = MaterialType.STEEL;
                    break;
            }

            // Step 3: Determine Isotropy Type
            IsotropyType isotropyType = symType == 1 ? IsotropyType.Orthotropic : IsotropyType.Isotropic;

            // Step 4: Create MaterialInfo object
            MaterialInfo materialInfo = new MaterialInfo(materialName, materialType)
            {
                Isotropy = isotropyType,
                Type = materialType
            };

            // Step 5: Retrieve Basic Material Properties
            int color = 0;
            string notes = "", guid = "";
            ret = sapModel.PropMaterial.GetMaterial(materialName, ref matType, ref color, ref notes, ref guid);

            if (ret == 0)
            {
                materialInfo.Color = ExtractColorFromSAP(color);
                materialInfo.Notes = notes;
                materialInfo.GUID = guid;
            }
            else
            {
                DebugHandler.Log($"⚠ WARNING: Could not retrieve full material properties for '{materialName}'", DebugHandler.LogLevel.WARNING);
            }

            // Step 6: Get Material Density
            materialInfo.Density = GetMaterialDensity(sapModel, materialName);

            // Step 7: Get Elastic Properties based on Isotropy Type
            if (isotropyType == IsotropyType.Isotropic)
            {
                GetIsotropicProperties(sapModel, materialName, ref materialInfo);
            }
            else if (isotropyType == IsotropyType.Orthotropic)
            {
                GetOrthotropicProperties(sapModel, materialName, ref materialInfo);
            }

            // Step 8: Retrieve Specific Properties Based on Material Type
            if (materialType == MaterialType.CONCRETE)
            {
                GetConcreteProperties(sapModel, materialName, ref materialInfo);
            }
            else if (materialType == MaterialType.STEEL)
            {
                GetSteelProperties(sapModel, materialName, ref materialInfo);
            }

            return materialInfo;
        }


        public static double GetMaterialDensity(cSapModel sapModel, string materialName)
        {
            double weightPerVolume = 0;
            double massPerVolume = 0;

            int ret = sapModel.PropMaterial.GetWeightAndMass(materialName, ref weightPerVolume, ref massPerVolume);

            if (ret == 0)
            {
                DebugHandler.Log($"✅ Retrieved density for '{materialName}': {weightPerVolume} kN/m³");
                return weightPerVolume; // SAP2000 returns weight per volume directly in kN/m³
            }
            else
            {
                DebugHandler.Log($"⚠ WARNING: Failed to retrieve density for '{materialName}'", DebugHandler.LogLevel.WARNING);
                return 0; // Default to 0 if retrieval fails
            }
        }

        public static bool GetIsotropicProperties(cSapModel sapModel, string materialName, ref MaterialInfo materialInfo)
        {
            double modulusElasticity = 0;
            double poissonRatio = 0;
            double thermalCoeff = 0;
            double shearModulus = 0;

            int ret = sapModel.PropMaterial.GetMPIsotropic(materialName, ref modulusElasticity, ref poissonRatio, ref thermalCoeff, ref shearModulus);

            if (ret == 0)
            {
                materialInfo.YoungModulusX = modulusElasticity;
                materialInfo.YoungModulusY = modulusElasticity;
                materialInfo.YoungModulusZ = modulusElasticity;

                materialInfo.PoissonRatioXY = poissonRatio;
                materialInfo.PoissonRatioXZ = poissonRatio;
                materialInfo.PoissonRatioYZ = poissonRatio;

                materialInfo.ThermalExpansionCoeffX = thermalCoeff;
                materialInfo.ThermalExpansionCoeffY = thermalCoeff;
                materialInfo.ThermalExpansionCoeffZ = thermalCoeff;

                materialInfo.ShearModulusXY = shearModulus;
                materialInfo.ShearModulusXZ = shearModulus;
                materialInfo.ShearModulusYZ = shearModulus;

                DebugHandler.Log($@"✅ Retrieved isotropic properties for '{materialName}':
            - Young’s Modulus: {modulusElasticity} kN/m²
            - Poisson’s Ratio: {poissonRatio}
            - Thermal Coefficient: {thermalCoeff} 1/K
            - Shear Modulus: {shearModulus} kN/m²");

                return true;
            }
            else
            {
                DebugHandler.Log($"⚠ WARNING: Failed to retrieve isotropic properties for '{materialName}'", DebugHandler.LogLevel.WARNING);
                return false;
            }
        }
        public static bool GetOrthotropicProperties(cSapModel sapModel, string materialName, ref MaterialInfo materialInfo)
        {
            double[] modulusElasticity = new double[3]; // E1, E2, E3
            double[] poissonRatios = new double[3]; // U12, U13, U23
            double[] thermalCoeffs = new double[3]; // A1, A2, A3
            double[] shearModuli = new double[3]; // G12, G13, G23

            int ret = sapModel.PropMaterial.GetMPOrthotropic(materialName, ref modulusElasticity, ref poissonRatios, ref thermalCoeffs, ref shearModuli);

            if (ret == 0)
            {
                materialInfo.YoungModulusX = modulusElasticity[0];
                materialInfo.YoungModulusY = modulusElasticity[1];
                materialInfo.YoungModulusZ = modulusElasticity[2];

                materialInfo.PoissonRatioXY = poissonRatios[0];
                materialInfo.PoissonRatioXZ = poissonRatios[1];
                materialInfo.PoissonRatioYZ = poissonRatios[2];

                materialInfo.ThermalExpansionCoeffX = thermalCoeffs[0];
                materialInfo.ThermalExpansionCoeffY = thermalCoeffs[1];
                materialInfo.ThermalExpansionCoeffZ = thermalCoeffs[2];

                materialInfo.ShearModulusXY = shearModuli[0];
                materialInfo.ShearModulusXZ = shearModuli[1];
                materialInfo.ShearModulusYZ = shearModuli[2];

                DebugHandler.Log($@"✅ Retrieved orthotropic properties for '{materialName}':
            - Young’s Modulus (E1, E2, E3): {modulusElasticity[0]}, {modulusElasticity[1]}, {modulusElasticity[2]} kN/m²
            - Poisson’s Ratios (U12, U13, U23): {poissonRatios[0]}, {poissonRatios[1]}, {poissonRatios[2]}
            - Thermal Expansion Coefficients (A1, A2, A3): {thermalCoeffs[0]}, {thermalCoeffs[1]}, {thermalCoeffs[2]} 1/K
            - Shear Moduli (G12, G13, G23): {shearModuli[0]}, {shearModuli[1]}, {shearModuli[2]} kN/m²");

                return true;
            }
            else
            {
                DebugHandler.Log($"⚠ WARNING: Failed to retrieve orthotropic properties for '{materialName}'", DebugHandler.LogLevel.WARNING);
                return false;
            }
        }

        private static void GetConcreteProperties(cSapModel sapModel, string materialName, ref MaterialInfo materialInfo)
        {
            double fc = 0, shearFactor = 1, strainAtFc = 0, strainUltimate = 0, frictionAngle = 0, dilatationalAngle = 0;
            int stressStrainType = 0, hysteresisType = 0;
            bool isLightweight = false;

            int ret = sapModel.PropMaterial.GetOConcrete(materialName, ref fc, ref isLightweight, ref shearFactor, ref stressStrainType, ref hysteresisType, ref strainAtFc, ref strainUltimate, ref frictionAngle, ref dilatationalAngle);
            if (ret == 0)
            {
                materialInfo.ConcreteCompression = fc;
                materialInfo.ConcreteShearStrengthReduction = shearFactor;//for lightweight concrete
                materialInfo.StressStrainCurveType = stressStrainType;
                materialInfo.StressStrainHysteresisType = hysteresisType;
                materialInfo.StrainAtFc = strainAtFc;
                materialInfo.StrainUltimate = strainUltimate;
                materialInfo.FrictionAngle = frictionAngle;
                materialInfo.DilatationalAngle = dilatationalAngle;
                materialInfo.IsLightweight = isLightweight;
            }

            // Get FinalSlope from GetOConcrete_1
            double finalSlope = 0;
            ret = sapModel.PropMaterial.GetOConcrete_1(materialName, ref fc, ref isLightweight, ref shearFactor, ref stressStrainType, ref hysteresisType, ref strainAtFc, ref strainUltimate, ref finalSlope, ref frictionAngle, ref dilatationalAngle);
            if (ret == 0) materialInfo.FinalSlope = finalSlope;

            // Log Concrete-Specific Properties
            DebugHandler.Log($@"
                ✅ [Concrete Material: {materialName}] Retrieved Properties:
                - Compressive Strength: {fc} kN/m²
                - Shear Strength Reduction: {shearFactor}
                - Stress-Strain Curve Type: {stressStrainType}
                - Hysteresis Type: {hysteresisType}
                - Strain at Fc: {strainAtFc}
                - Ultimate Strain: {strainUltimate}
                - Friction Angle: {frictionAngle}°
                - Dilatational Angle: {dilatationalAngle}°
                - Final Slope: {finalSlope}
                - Lightweight: {isLightweight}
                ");
        }
        public static void GetSteelProperties(cSapModel sapModel, string materialName, ref MaterialInfo materialInfo)
        {
            double Fy = 0, Fu = 0, eFy = 0, eFu = 0;
            int SSType = 0, SSHysType = 0;
            double strainAtHardening = 0, strainAtMaxStress = 0, strainAtRupture = 0;

            int ret = sapModel.PropMaterial.GetOSteel(materialName, ref Fy, ref Fu, ref eFy, ref eFu,
                                                      ref SSType, ref SSHysType,
                                                      ref strainAtHardening, ref strainAtMaxStress, ref strainAtRupture);

            if (ret != 0)
            {
                DebugHandler.Log($"⚠ WARNING: Could not retrieve steel properties for '{materialName}'", DebugHandler.LogLevel.WARNING);
                return;
            }

            // Convert values to MPa for easier comparison with SAP2000 UI
            materialInfo.MinimumYieldStress = Fy;
            materialInfo.MinimumTensileStrength = Fu;
            materialInfo.ExpectedYieldStress = eFy;
            materialInfo.ExpectedTensileStress = eFu;

            materialInfo.StressStrainCurveType = SSType;
            materialInfo.StressStrainHysteresisType = SSHysType;

            materialInfo.StrainAtHardening = strainAtHardening;
            materialInfo.StrainAtMaxStress = strainAtMaxStress;
            materialInfo.StrainAtRupture = strainAtRupture;

            DebugHandler.Log($@"
            ✅ [Steel Material: {materialName}]
            - Yield Stress: {materialInfo.MinimumYieldStress} MPa
            - Tensile Strength: {materialInfo.MinimumTensileStrength} MPa
            - Expected Yield Stress: {materialInfo.ExpectedYieldStress} MPa
            - Expected Tensile Strength: {materialInfo.ExpectedTensileStress} MPa
            - Stress-Strain Curve Type: {materialInfo.StressStrainCurveType}
            - Stress-Strain Hysteresis Type: {materialInfo.StressStrainHysteresisType}
            - Strain at Hardening: {materialInfo.StrainAtHardening}
            - Strain at Max Stress: {materialInfo.StrainAtMaxStress}
            - Strain at Rupture: {materialInfo.StrainAtRupture}
        ");
        }

        private static System.Windows.Media.Color ExtractColorFromSAP(int colorValue)
        {
            if (colorValue == 0) return System.Windows.Media.Colors.Gray; // Default color if invalid

            int red = (colorValue >> 16) & 0xFF;
            int green = (colorValue >> 8) & 0xFF;
            int blue = colorValue & 0xFF;

            return System.Windows.Media.Color.FromRgb((byte)red, (byte)green, (byte)blue);
        }

    }
}
