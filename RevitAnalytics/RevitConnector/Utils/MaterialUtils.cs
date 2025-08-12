using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace RevitAnalytics.RevitConnector.Utils
{
    public static class MaterialUtils
    {
        public static Material GetOrCreateMaterial(Document doc, MaterialInfo materialInfo)
        {
            // Check if material already exists
            Material existingMaterial = FindMaterialByName(doc, materialInfo.Name);
            if (existingMaterial != null)
            {
                DebugHandler.Log($"Material '{materialInfo.Name}' already exists in Revit.");

                return existingMaterial;
            }

            DebugHandler.Log($"Material '{materialInfo.Name}' not found. Creating new material.", DebugHandler.LogLevel.INFO);

            // Create new Revit material
            return CreateMaterial(doc, materialInfo);
        }

        public static Material FindMaterialByName(Document doc, string materialName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Material));

            foreach (Element element in collector)
            {
                if (element is Material material && material.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase))
                {
                    return material;
                }
            }
            return null;
        }

        public static Material CreateMaterial(Document doc, MaterialInfo materialInfo)
        {
            Material newMaterial = null;

            try
            {
                ElementId materialId = Material.Create(doc, materialInfo.Name);
                newMaterial = doc.GetElement(materialId) as Material;

                if (newMaterial != null)
                {
                    DebugHandler.Log($"Successfully created material: {newMaterial.Name}", DebugHandler.LogLevel.INFO);

                    AssignMaterialClass(newMaterial, materialInfo.Type);


                }
                else
                {
                    DebugHandler.LogError($"Failed to create material: {materialInfo.Name}", new Exception("Material creation returned null."));
                }
            }
            catch (Exception ex)
            {
                DebugHandler.LogError("Error creating material.", ex);

            }


            return newMaterial;
        }

        public static Material GetOrCreateBasicSteel(Document doc, string name = "Steel")
        {
            Material existingMaterial = FindMaterialByName(doc, name);
            if (existingMaterial != null)
            {
                DebugHandler.Log($"Material '{name}' already exists in Revit.");
                return existingMaterial;
            }

            DebugHandler.Log($"Material '{name}' not found. Creating a new Basic Steel material.", DebugHandler.LogLevel.INFO);

            MaterialInfo steelInfo = MaterialInfo.CreateBaseSteel(name);
            return CreateMaterial(doc, steelInfo);
        }

        public static Material GetOrCreateBasicConcrete(Document doc, string name = "Concrete")
        {
            Material existingMaterial = FindMaterialByName(doc, name);
            if (existingMaterial != null)
            {
                DebugHandler.Log($"Material '{name}' already exists in Revit.");
                return existingMaterial;
            }

            DebugHandler.Log($"Material '{name}' not found. Creating a new Basic Concrete material.", DebugHandler.LogLevel.INFO);

            MaterialInfo concreteInfo = MaterialInfo.CreateBaseConcrete(name);
            return CreateMaterial(doc, concreteInfo);
        }

        private static void AssignMaterialClass(Material material, MaterialType materialType)
        {
            Parameter classParam = material.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_CLASS);
            if (classParam != null)
            {
                switch (materialType)
                {
                    case MaterialType.CONCRETE:
                        classParam.Set("Concrete");
                        break;
                    case MaterialType.STEEL:
                        classParam.Set("Metal");
                        break;
                    case MaterialType.REBAR:
                        classParam.Set("Rebar");
                        break;
                    default:
                        classParam.Set("Generic");
                        break;
                }
                DebugHandler.Log($"Assigned Material Class: {classParam.AsString()} for {material.Name}");
            }
        }

        

        public static void SetMaterialPhysicalProperties(Material material, MaterialInfo materialInfo, Document doc)
        {

            if (material == null)
            {
                DebugHandler.Log($"⚠ WARNING: Provided Material is null. Returning default Concrete.", DebugHandler.LogLevel.WARNING);
                TaskDialog.Show("Material Not Found", $"Material not found for element.");
            }


            DebugHandler.Log($"🔹 Processing material {material.Name}");

            // ✅ **STEP 1: Check if PropertySetElement Exists**
            // 🔍 Step 1: Create a New Structural
            string uniqueAssetName = $"{material.Name}_Asset";//_{DateTime.Now:yyyyMMddHHmmss}";
            StructuralAsset structuralAsset = new StructuralAsset(uniqueAssetName,
                materialInfo.Type == MaterialType.CONCRETE ? StructuralAssetClass.Concrete : StructuralAssetClass.Metal);

            // 🔍 Step 42: Extract Properties from StructuralAsset
            if (structuralAsset.Behavior == StructuralBehavior.Isotropic)
            {
                try
                {
                    structuralAsset.SetYoungModulus(Converters.UnitConverter.ConvertKNPerM2ToNewtonPerFootMeter(materialInfo.YoungModulusX));
                    //Log the ratio that is set and the ratio we try to pass
                    DebugHandler.Log($"'Set' Young Modulus to {materialInfo.YoungModulusX} for {material.Name}. Actual value - {structuralAsset.YoungModulus}");
                    structuralAsset.SetPoissonRatio(materialInfo.PoissonRatioXY);
                    //Log the ratio that is set and the ratio we try to pass
                    DebugHandler.Log($"'Set' Poisson Ratio to {materialInfo.PoissonRatioXY} for {material.Name}. Actual value - {structuralAsset.PoissonRatio}");
                    structuralAsset.SetShearModulus(Converters.UnitConverter.ConvertKNPerM2ToNewtonPerFootMeter(materialInfo.ShearModulusXY));
                    //Log the ratio that is set and the ratio we try to pass
                    DebugHandler.Log($"'Set' Shear Modulus to {materialInfo.ShearModulusXY} for {material.Name}. Actual value - {structuralAsset.ShearModulus}");
                    structuralAsset.SetThermalExpansionCoefficient(materialInfo.ThermalExpansionCoeffX);
                    //Log the ratio that is set and the ratio we try to pass
                    DebugHandler.Log($"'Set' Thermal Expansion Coefficient to {materialInfo.ThermalExpansionCoeffX} for {material.Name}. Actual value - {structuralAsset.ThermalExpansionCoefficient}");

                    DebugHandler.Log($"✅ Successfully assigned isotropic properties for {material.Name}");
                }
                catch (Exception ex)
                {
                    DebugHandler.LogError($"⚠ Error setting isotropic properties for {material.Name}.", ex);
                }
            }

            // **Handling Non-Isotropic Materials (Full XYZ Values)**
            else
            {
                try
                {
                    structuralAsset.YoungModulus = new XYZ(materialInfo.YoungModulusX, materialInfo.YoungModulusY, materialInfo.YoungModulusZ);
                    structuralAsset.ShearModulus = new XYZ(materialInfo.ShearModulusXY, materialInfo.ShearModulusYZ, materialInfo.ShearModulusXZ);
                    structuralAsset.PoissonRatio = new XYZ(materialInfo.PoissonRatioXY, materialInfo.PoissonRatioYZ, materialInfo.PoissonRatioXZ);
                    structuralAsset.ThermalExpansionCoefficient = new XYZ(materialInfo.ThermalExpansionCoeffX, materialInfo.ThermalExpansionCoeffY, materialInfo.ThermalExpansionCoeffZ);

                    DebugHandler.Log($"✅ Assigned Non-Isotropic Material Properties for {material.Name}");
                }
                catch (Exception ex)
                {
                    DebugHandler.LogError($"Error setting non-isotropic properties for {material.Name}.", ex);
                }
            }

            try
            {
                structuralAsset.Density = Converters.UnitConverter.ConvertKNM3ToKgFt3(materialInfo.Density);
                structuralAsset.MinimumYieldStress = Converters.UnitConverter.ConvertKNPerM2ToNewtonPerFootMeter(materialInfo.MinimumYieldStress);
                structuralAsset.MinimumTensileStrength = Converters.UnitConverter.ConvertKNPerM2ToNewtonPerFootMeter(materialInfo.MinimumTensileStrength);

                DebugHandler.Log($"✅ Assigned Common Properties for {material.Name} (Density, Yield Stress, Tensile Strength)");
            }
            catch (Exception ex)
            {
                DebugHandler.LogError($"Error setting Common Properties for {material.Name}.", ex);
            }

            // 🔍 Step 3: **Assign Concrete-Specific Properties**
            if (materialInfo.Type == MaterialType.CONCRETE)
            {
                try
                {
                    structuralAsset.ConcreteCompression = Converters.UnitConverter.ConvertKNPerM2ToNewtonPerFootMeter(materialInfo.ConcreteCompression);
                    structuralAsset.ConcreteShearReinforcement = materialInfo.ConcreteShearReinforcement;
                    structuralAsset.ConcreteShearStrengthReduction = materialInfo.ConcreteShearStrengthReduction;
                    structuralAsset.Lightweight = materialInfo.IsLightweight;

                    DebugHandler.Log($"✅ Assigned Concrete Properties for {material.Name}");
                }
                catch (Exception ex)
                {
                    DebugHandler.LogError($"Error setting Concrete Properties for {material.Name}.", ex);
                }
            }

            // 🔍 Step 4: **Assign Steel-Specific Properties**
            else if (materialInfo.Type == MaterialType.STEEL)
            {
                try
                {
                    structuralAsset.MetalResistanceCalculationStrength = Converters.UnitConverter.ConvertKNPerM2ToNewtonPerFootMeter(materialInfo.MetalResistanceCalcStrength);
                    structuralAsset.MetalReductionFactor = materialInfo.MetalReductionFactor;
                    structuralAsset.MetalThermallyTreated = materialInfo.MetalThermallyTreated;

                    DebugHandler.Log($"✅ Assigned Steel Properties for {material.Name}");
                }
                catch (Exception ex)
                {
                    DebugHandler.LogError($"Error setting Steel Properties for {material.Name}.", ex);
                }
            }
            //STEP 4.2 DELETE OLD SET
            ElementId oldPropertySetId = material.StructuralAssetId;
            if (oldPropertySetId != ElementId.InvalidElementId)
            {
                PropertySetElement oldPropertySet = doc.GetElement(oldPropertySetId) as PropertySetElement;
                if (oldPropertySet != null)
                    try
                    {
                        oldPropertySet.GetStructuralAsset().Dispose();
                        doc.Delete(oldPropertySetId);
                        DebugHandler.Log($"🗑️ Deleted old PropertySetElement for {material.Name}");
                    }
                    catch (Exception ex)
                    {
                        DebugHandler.LogError($"⚠ Error deleting old PropertySetElement for {material.Name}.", ex);
                    }
            }
            //log the attempt to set the material
            try
            {
                // 🔍 Step 5: Create a New PropertySetElement
                PropertySetElement propertySet = PropertySetElement.Create(doc, structuralAsset);
                DebugHandler.Log($"✅ Created PropertySetElement for {material.Name}");

                // 🔍 Step 6: Assign the PropertySet to the Material
                material.SetMaterialAspectByPropertySet(MaterialAspect.Structural, propertySet.Id);
                //material.StructuralAssetId = propertySet.Id; // ✅ Now explicitly assign it
                DebugHandler.Log($"Set physical properties for {material.Name}");
            }
            catch (Exception ex)
            {
                DebugHandler.LogError($"Error setting physical properties for {material.Name}.", ex);
            }

            DebugHandler.Log($"Set physical properties for {material.Name}");
        }
        public static MaterialInfo GetMaterialInfoByName(Document doc, string materialName)
        {
            Material material = FindMaterialByName(doc, materialName);
            if (material == null)
            {
                DebugHandler.Log($"⚠ WARNING: Material '{materialName}' not found in Revit. Returning default Concrete.", DebugHandler.LogLevel.WARNING);
                return MaterialInfo.CreateBaseConcrete("DefaultConcrete");
            }
            return GetMaterialInfo(material, doc);
        }

        private static bool HasMaterialParameter(PropertySetElement propertySet, BuiltInParameter param)
        {
            Parameter revitParam = propertySet.get_Parameter(param);
            return revitParam != null && revitParam.HasValue;
        }
        public static StructuralBehavior ConvertToRevitStructuralBehavior(IsotropyType isotropyType)
        {
            switch (isotropyType)
            {
                case IsotropyType.Isotropic:
                    return StructuralBehavior.Isotropic;
                case IsotropyType.TransverselyIsotropic:
                    return StructuralBehavior.TransverseIsotropic;
                case IsotropyType.Orthotropic:
                    return StructuralBehavior.Orthotropic;
                default:
                    DebugHandler.Log($"⚠ WARNING: Unknown IsotropyType '{isotropyType}'. Defaulting to Isotropic.", DebugHandler.LogLevel.WARNING);
                    return StructuralBehavior.Isotropic;
            }
        }


        public static MaterialInfo GetMaterialInfo(Material material, Document doc)
        {
            if (material == null)
            {
                DebugHandler.Log($"⚠ WARNING: Provided Material is null. Returning default Concrete.", DebugHandler.LogLevel.WARNING);
                return MaterialInfo.CreateBaseConcrete("DefaultConcrete");
            }

            // 🔍 Step 1: Get the StructuralAssetId
            ElementId assetId = material.StructuralAssetId;
            if (assetId == ElementId.InvalidElementId)
            {
                DebugHandler.Log($"⚠ WARNING: Material '{material.Name}' has no Structural Asset assigned.", DebugHandler.LogLevel.WARNING);
                return MaterialInfo.CreateBaseConcrete("DefaultConcrete");
            }

            // 🔍 Step 2: Retrieve the PropertySetElement
            PropertySetElement propertySet = doc.GetElement(assetId) as PropertySetElement;
            if (propertySet == null)
            {
                DebugHandler.Log($"⚠ WARNING: Could not retrieve PropertySetElement for Material '{material.Name}'.", DebugHandler.LogLevel.WARNING);
                return MaterialInfo.CreateBaseConcrete("DefaultConcrete");
            }

            // 🔍 Step 3: Retrieve the StructuralAsset
            StructuralAsset structuralAsset = propertySet.GetStructuralAsset();
            if (structuralAsset == null)
            {
                DebugHandler.Log($"⚠ WARNING: Could not retrieve StructuralAsset for Material '{material.Name}'.", DebugHandler.LogLevel.WARNING);
                return MaterialInfo.CreateBaseConcrete("DefaultConcrete");
            }

            // 🔍 Step 4: Extract Properties from StructuralAsset

            IsotropyType isotropyType = DetectIsotropyType(structuralAsset.Behavior.ToString());

            DebugHandler.Log($"✅ [Material: {material.Name}] - Isotropic: {isotropyType}");

            // 🔹 Step 5: Initialize MaterialInfo **without constructor madness**

            // 📌 Determine Material Type
            MaterialType materialType = MaterialType.NODESIGN;
            var materialAssetType = structuralAsset.StructuralAssetClass;

            switch (materialAssetType)
            {
                case StructuralAssetClass.Concrete: materialType = MaterialType.CONCRETE; break;
                case StructuralAssetClass.Metal: materialType = MaterialType.STEEL; break;
            }

            MaterialInfo materialInfo = new MaterialInfo(material.Name, materialType);
            materialInfo.Isotropy = isotropyType;

            // 🔹 Step 6: Assign Mechanical Properties
            materialInfo.YoungModulusX = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.YoungModulus.X);
            materialInfo.YoungModulusY = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.YoungModulus.Y);
            materialInfo.YoungModulusZ = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.YoungModulus.Z);

            materialInfo.ShearModulusXY = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.ShearModulus.X);
            materialInfo.ShearModulusYZ = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.ShearModulus.Y);
            materialInfo.ShearModulusXZ = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.ShearModulus.Z);

            materialInfo.ThermalExpansionCoeffX = structuralAsset.ThermalExpansionCoefficient.X;
            materialInfo.ThermalExpansionCoeffY = structuralAsset.ThermalExpansionCoefficient.Y;
            materialInfo.ThermalExpansionCoeffZ = structuralAsset.ThermalExpansionCoefficient.Z;

            materialInfo.PoissonRatioXY = structuralAsset.PoissonRatio.X;
            materialInfo.PoissonRatioXZ = structuralAsset.PoissonRatio.Y;
            materialInfo.PoissonRatioYZ = structuralAsset.PoissonRatio.Z;
            materialInfo.Density = Converters.UnitConverter.ConvertkgFt3ToKNM3(structuralAsset.Density);
            materialInfo.MinimumYieldStress = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.MinimumYieldStress);
            materialInfo.MinimumTensileStrength = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.MinimumTensileStrength);

            if (materialType == MaterialType.CONCRETE)
            {
                materialInfo.ConcreteCompression = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.ConcreteCompression);
                materialInfo.ExpectedCompressiveStrength = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.ConcreteCompression);
                materialInfo.ConcreteShearReinforcement = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.ConcreteShearReinforcement);
                materialInfo.ConcreteShearStrengthReduction = structuralAsset.ConcreteShearStrengthReduction;

                // SAP2000-Specific Properties
                materialInfo.StressStrainCurveType = 1; // Parametric - Simple
                materialInfo.StressStrainHysteresisType = 0; // Elastic
                materialInfo.StrainAtFc = 0.002;
                materialInfo.StrainUltimate = 0.0035;
                materialInfo.FrictionAngle = 30.0;
                materialInfo.DilatationalAngle = 10.0;
                materialInfo.ExpectedTensileStress = 0.1 * materialInfo.MinimumTensileStrength;
                materialInfo.IsLightweight = structuralAsset.Lightweight;
            }
            else if (materialType == MaterialType.STEEL)
            {
                materialInfo.MetalResistanceCalcStrength = Converters.UnitConverter.ConvertNewtonPerFootMeterToKNPerM2(structuralAsset.MetalResistanceCalculationStrength);
                materialInfo.MetalReductionFactor = structuralAsset.MetalReductionFactor;
                materialInfo.MetalThermallyTreated = structuralAsset.MetalThermallyTreated;

                // SAP2000-Specific Properties
                materialInfo.StressStrainCurveType = 1; // Parametric - Simple
                materialInfo.StressStrainHysteresisType = 0; // Elastic
                materialInfo.StrainAtHardening = 0.002;
                materialInfo.StrainAtMaxStress = 0.02;
                materialInfo.StrainAtRupture = 0.1;
                materialInfo.FinalSlope = 0.01 * materialInfo.YoungModulusX;
                materialInfo.ExpectedTensileStress = 0.1 * materialInfo.MinimumTensileStrength;
                materialInfo.ExpectedYieldStress = 1.1 * materialInfo.MinimumYieldStress;
            }

            // 📌 LOG ALL FOUND VALUES 🔍
            MaterialInfo.LogMaterialInfo(materialInfo);

            // 📌 Return MaterialInfo instance with the updated constructor
            return materialInfo;
        }




        public static IsotropyType DetectIsotropyType(string behavior)
        {
            switch (behavior)
            {
                case "Isotropic":
                    return IsotropyType.Isotropic;
                case "TransverselyIsotropic":
                    return IsotropyType.TransverselyIsotropic;
                case "Orthotropic":
                    return IsotropyType.Orthotropic;
                default:
                    DebugHandler.Log($"⚠ WARNING: Unknown StructuralBehavior '{behavior}'. Defaulting to Isotropic.", DebugHandler.LogLevel.WARNING);
                    TaskDialog.Show("Warning", $"Unknown StructuralBehavior '{behavior}'. Defaulting to Isotropic.");
                    return IsotropyType.Isotropic;
            }
        }



        private static void SetMaterialParameter(Material material, BuiltInParameter parameter, double value)
        {
            Parameter param = material.get_Parameter(parameter);
            if (param != null && param.StorageType == StorageType.Double)
            {
                param.Set(value);
                DebugHandler.Log($"Set {parameter} to {value} for {material.Name}");
            }
        }

        private static double GetMaterialParameter(PropertySetElement material, BuiltInParameter paramType)
        {
            Parameter param = material.get_Parameter(paramType);

            if (param == null)
            {
                DebugHandler.Log(
                    $"[Material: {material.Name}] [Parameter: {paramType}] => ❌ NULL (Parameter does not exist)",
                    DebugHandler.LogLevel.WARNING
                );
                return 0.0;
            }

            if (!param.HasValue)
            {
                DebugHandler.Log(
                    $"[Material: {material.Name}] [Parameter: {paramType}] => ❌ NULL (No assigned value)",
                    DebugHandler.LogLevel.WARNING
                );
                return 0.0;
            }

            // Define main StorageType for informational purposes
            StorageType mainStorageType = param.StorageType;

            // Initialize all possible values
            string storageTypeName = mainStorageType.ToString();
            string doubleValue = "NULL";
            string integerValue = "NULL";
            string stringValue = "NULL";
            string valueString = "NULL";  // AsValueString() might contain extra info
            string elementIdValue = "NULL";

            double returnValue = 0.0; // The main value to return

            // Always try to get ALL possible values and log any issues
            try
            {
                returnValue = param.AsDouble();
                doubleValue = returnValue.ToString();
            }
            catch (Exception ex)
            {
                DebugHandler.Log(
                    $"[Material: {material.Name}] [Parameter: {paramType}] => ⚠ Exception in AsDouble(): {ex.Message}",
                    DebugHandler.LogLevel.ERROR
                );
            }

            try
            {
                returnValue = param.AsInteger();
                integerValue = returnValue.ToString();
            }
            catch (Exception ex)
            {
                DebugHandler.Log(
                    $"[Material: {material.Name}] [Parameter: {paramType}] => ⚠ Exception in AsInteger(): {ex.Message}",
                    DebugHandler.LogLevel.ERROR
                );
            }

            try
            {
                stringValue = param.AsString() ?? "NULL";
            }
            catch (Exception ex)
            {
                DebugHandler.Log(
                    $"[Material: {material.Name}] [Parameter: {paramType}] => ⚠ Exception in AsString(): {ex.Message}",
                    DebugHandler.LogLevel.ERROR
                );
            }

            try
            {
                valueString = param.AsValueString() ?? "NULL";
            }
            catch (Exception ex)
            {
                DebugHandler.Log(
                    $"[Material: {material.Name}] [Parameter: {paramType}] => ⚠ Exception in AsValueString(): {ex.Message}",
                    DebugHandler.LogLevel.ERROR
                );
            }

            try
            {
                elementIdValue = param.AsElementId().ToString();
            }
            catch (Exception ex)
            {
                DebugHandler.Log(
                    $"[Material: {material.Name}] [Parameter: {paramType}] => ⚠ Exception in AsElementId(): {ex.Message}",
                    DebugHandler.LogLevel.ERROR
                );
            }

            // Log all retrieved data in **one single line**
            DebugHandler.Log(
                $"[Material: {material.Name}] [Parameter: {paramType}] => " +
                $"🔹 StorageType: {storageTypeName} | " +
                $"🔹 Double: {doubleValue} | 🔸 Integer: {integerValue} | " +
                $"🔹 String: {stringValue} | 🔸 ValueString: {valueString} | " +
                $"🔹 ElementId: {elementIdValue}",
                DebugHandler.LogLevel.INFO
            );

            return returnValue;
        }

        public static void LogAllMaterialParameters(Material material)
        {
            DebugHandler.Log($"📌 Debugging Material: {material.Name}", DebugHandler.LogLevel.INFO);

            foreach (Parameter param in material.Parameters)
            {
                string paramName = param.Definition.Name;
                string paramValue = "NULL";

                if (param.HasValue)
                {
                    switch (param.StorageType)
                    {
                        case StorageType.Double:
                            paramValue = param.AsDouble().ToString();
                            break;
                        case StorageType.Integer:
                            paramValue = param.AsInteger().ToString();
                            break;
                        case StorageType.String:
                            paramValue = param.AsString();
                            break;
                        default:
                            paramValue = "UNKNOWN TYPE";
                            break;
                    }
                }

                DebugHandler.Log($"🔍 Parameter: {paramName} | Value: {paramValue} | StorageType {param.StorageType}", DebugHandler.LogLevel.DEBUG);
            }
        }

    }
}
