using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.RevitPnA
{
    class UpdatePars
    {
        public static void UpdateParameters(Document doc, List<RevitAnalyticalElementInfo> analyticalRevitInfos)
        {
            if (doc == null || analyticalRevitInfos == null || analyticalRevitInfos.Count == 0)
            {
                DebugHandler.Log("No document or analytical elements provided for parameter update.", DebugHandler.LogLevel.WARNING);
                return;
            }

            DebugHandler.Log($"Starting parameter update for {analyticalRevitInfos.Count} analytical elements.", DebugHandler.LogLevel.INFO);

            int updatedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < analyticalRevitInfos.Count; i++)
            {
                RevitAnalyticalElementInfo info = analyticalRevitInfos[i];
                if (info == null || info.Element == null)
                {
                    DebugHandler.Log($"Skipped null info or element at index {i}.", DebugHandler.LogLevel.WARNING);
                    skippedCount++;
                    continue;
                }

                // Resolve analytical element (may be null if not created earlier)
                Element analyticalElem = (info.AnalyticalMember != null)
                    ? (Element)info.AnalyticalMember
                    : (info.AnRevitId != null ? doc.GetElement(info.AnRevitId) : null);

                if (analyticalElem == null)
                {
                    DebugHandler.Log($"Skipped element with null analytical element. PhysicalId={info.PhRevitId?.IntegerValue}, AnalyticalId={info.AnRevitId?.IntegerValue}", DebugHandler.LogLevel.WARNING);
                    skippedCount++;
                    continue;
                }

                DebugHandler.Log($"Updating parameters for AnalyticalId={analyticalElem.Id}, PhysicalId={info.Element.Id}", DebugHandler.LogLevel.INFO);

                // -------- 1) Copy string parameters ITS_* from physical -> analytical --------
                CopyStringParam(doc, info.Element, analyticalElem, "ITS_Element Type");
                CopyStringParam(doc, info.Element, analyticalElem, "ITS_First Support Name");
                CopyStringParam(doc, info.Element, analyticalElem, "ITS_Second Support Name");
                CopyStringParam(doc, info.Element, analyticalElem, "ITS_Frame Name");
                CopyStringParam(doc, info.Element, analyticalElem, "ITS_Leading Role");

                // Optionally reflect a couple of these into info as well (convenient cache)
                string frameName = GetStringFromInstanceOrType(info.Element, "ITS_Frame Name");
                if (!string.IsNullOrEmpty(frameName))
                {
                    info.FrameName = frameName;
                    DebugHandler.Log($"Cached 'ITS_Frame Name'='{frameName}' for AnalyticalId={analyticalElem.Id}", DebugHandler.LogLevel.INFO);
                }

                string leadingRole = GetStringFromInstanceOrType(info.Element, "ITS_Leading Role");
                if (!string.IsNullOrEmpty(leadingRole))
                {
                    info.LeadingRole = leadingRole;
                    DebugHandler.Log($"Cached 'ITS_Leading Role'='{leadingRole}' for AnalyticalId={analyticalElem.Id}", DebugHandler.LogLevel.INFO);
                }

                // -------- 2) Material Type (BuiltIn: PHY_MATERIAL_PARAM_TYPE) -> info.MaterialType --------
                int matTypeInt;
                Element  material =  doc.GetElement(info.Element.GetMaterialIds(false).First());
                if (TryGetIntFromInstanceOrType(material, BuiltInParameter.PHY_MATERIAL_PARAM_TYPE, out matTypeInt))
                {
                    info.MaterialType = MapMaterialType(matTypeInt);
                    DebugHandler.Log($"Set MaterialType={info.MaterialType} (int value={matTypeInt}) for AnalyticalId={analyticalElem.Id}", DebugHandler.LogLevel.INFO);
                }

                // -------- 3) Rotation (BuiltIn: STRUCTURAL_BEND_DIR_ANGLE) -> info.Rotation --------
                // (This is typically in radians in Revit internal units.)
                // -------- 3) Rotation (BuiltIn: STRUCTURAL_BEND_DIR_ANGLE) -> info.Rotation --------
                // (This is typically in radians in Revit internal units.)
                double rotation;
                if (TryGetDoubleFromInstance(info.Element, BuiltInParameter.STRUCTURAL_BEND_DIR_ANGLE, out rotation))
                {
                    info.Rotation = rotation;
                    DebugHandler.Log($"Set Rotation={rotation} (radians) for AnalyticalId={analyticalElem.Id}", DebugHandler.LogLevel.INFO);

                    // -------- 4) Set rotation value into ANALYTICAL_MEMBER_ROTATION parameter of the analytical member --------
                    Parameter rotationParam = analyticalElem.get_Parameter(BuiltInParameter.ANALYTICAL_MEMBER_ROTATION);
                    if (rotationParam != null && rotationParam.StorageType == StorageType.Double)
                    {
                        bool setResult = rotationParam.Set(info.Rotation);
                        DebugHandler.Log($"Set ANALYTICAL_MEMBER_ROTATION={info.Rotation} for AnalyticalId={analyticalElem.Id}. Success: {setResult}", DebugHandler.LogLevel.INFO);
                    }
                    else
                    {
                        DebugHandler.Log($"Parameter ANALYTICAL_MEMBER_ROTATION not found or not double on AnalyticalId={analyticalElem.Id}.", DebugHandler.LogLevel.WARNING);
                    }
                }

                updatedCount++;
            }

            DebugHandler.Log($"Finished parameter update. Updated: {updatedCount}, Skipped: {skippedCount}.", DebugHandler.LogLevel.INFO);
        }

        /// <summary>Copies a string parameter value (by name) from source element (instance/type) to target element (instance).</summary>
        private static void CopyStringParam(Document doc, Element sourcePhysical, Element targetAnalytical, string paramName)
        {
            if (sourcePhysical == null || targetAnalytical == null || string.IsNullOrEmpty(paramName))
            {
                DebugHandler.Log($"CopyStringParam: Invalid arguments for parameter '{paramName}'.", DebugHandler.LogLevel.WARNING);
                return;
            }

            string value = GetStringFromInstanceOrType(sourcePhysical, paramName);
            if (string.IsNullOrEmpty(value))
            {
                DebugHandler.Log($"CopyStringParam: No value found for parameter '{paramName}' on PhysicalId={sourcePhysical.Id}.", DebugHandler.LogLevel.INFO);
                return;
            }

            Parameter dst = targetAnalytical.LookupParameter(paramName);
            if (dst != null && dst.StorageType == StorageType.String)
            {
                bool setResult = dst.Set(value);
                DebugHandler.Log($"Copied parameter '{paramName}'='{value}' from PhysicalId={sourcePhysical.Id} to AnalyticalId={targetAnalytical.Id}. Success: {setResult}", DebugHandler.LogLevel.INFO);
            }
            else
            {
                DebugHandler.Log($"CopyStringParam: Parameter '{paramName}' not found or not string on AnalyticalId={targetAnalytical.Id}.", DebugHandler.LogLevel.WARNING);
            }
        }

        /// <summary>Reads a string parameter by name. Checks instance first, then the type.</summary>
        private static string GetStringFromInstanceOrType(Element e, string paramName)
        {
            if (e == null || string.IsNullOrEmpty(paramName))
                return string.Empty;

            Parameter p = e.LookupParameter(paramName);
            if (p != null && p.StorageType == StorageType.String)
            {
                string v = p.AsString();
                if (!string.IsNullOrEmpty(v))
                {
                    DebugHandler.Log($"GetStringFromInstanceOrType: Found '{paramName}'='{v}' on ElementId={e.Id}.", DebugHandler.LogLevel.DEBUG);
                    return v;
                }
            }

            ElementId typeId = e.GetTypeId();
            if (typeId != null && typeId != ElementId.InvalidElementId)
            {
                Element t = e.Document.GetElement(typeId);
                if (t != null)
                {
                    Parameter tp = t.LookupParameter(paramName);
                    if (tp != null && tp.StorageType == StorageType.String)
                    {
                        string tv = tp.AsString();
                        if (!string.IsNullOrEmpty(tv))
                        {
                            DebugHandler.Log($"GetStringFromInstanceOrType: Found '{paramName}'='{tv}' on TypeId={typeId}.", DebugHandler.LogLevel.DEBUG);
                            return tv;
                        }
                    }
                }
            }

            DebugHandler.Log($"GetStringFromInstanceOrType: Parameter '{paramName}' not found on ElementId={e.Id} or its type.", DebugHandler.LogLevel.DEBUG);
            return string.Empty;
        }

        /// <summary>Try get INT from instance first; if null, try type.</summary>
        private static bool TryGetIntFromInstanceOrType(Element e, BuiltInParameter bip, out int value)
        {
            value = 0;
            if (e == null)
            {
                DebugHandler.Log($"TryGetIntFromInstanceOrType: Null element for BuiltInParameter '{bip}'.", DebugHandler.LogLevel.WARNING);
                return false;
            }

            Parameter p = e.get_Parameter(bip);
            if (p != null && p.StorageType == StorageType.Integer)
            {
                value = p.AsInteger();
                DebugHandler.Log($"TryGetIntFromInstanceOrType: Found value {value} for BuiltInParameter '{bip}' on ElementId={e.Id}.", DebugHandler.LogLevel.DEBUG);
                return true;
            }

            ElementId typeId = e.GetTypeId();
            if (typeId != null && typeId != ElementId.InvalidElementId)
            {
                Element t = e.Document.GetElement(typeId);
                if (t != null)
                {
                    Parameter tp = t.get_Parameter(bip);
                    if (tp != null && tp.StorageType == StorageType.Integer)
                    {
                        value = tp.AsInteger();
                        DebugHandler.Log($"TryGetIntFromInstanceOrType: Found value {value} for BuiltInParameter '{bip}' on TypeId={typeId}.", DebugHandler.LogLevel.DEBUG);
                        return true;
                    }
                }
            }
            DebugHandler.Log($"TryGetIntFromInstanceOrType: BuiltInParameter '{bip}' not found on ElementId={e.Id} or its type.", DebugHandler.LogLevel.DEBUG);
            return false;
        }

        /// <summary>Try get DOUBLE from instance.</summary>
        private static bool TryGetDoubleFromInstance(Element e, BuiltInParameter bip, out double value)
        {
            value = 0.0;
            if (e == null)
            {
                DebugHandler.Log($"TryGetDoubleFromInstance: Null element for BuiltInParameter '{bip}'.", DebugHandler.LogLevel.WARNING);
                return false;
            }

            Parameter p = e.get_Parameter(bip);
            if (p != null && p.StorageType == StorageType.Double)
            {
                value = p.AsDouble();
                DebugHandler.Log($"TryGetDoubleFromInstance: Found value {value} for BuiltInParameter '{bip}' on ElementId={e.Id}.", DebugHandler.LogLevel.DEBUG);
                return true;
            }
            DebugHandler.Log($"TryGetDoubleFromInstance: BuiltInParameter '{bip}' not found or not double on ElementId={e.Id}.", DebugHandler.LogLevel.DEBUG);
            return false;
        }

        /// <summary>Maps Revit's PHY_MATERIAL_PARAM_TYPE integer to your MaterialType enum.</summary>
        private static MaterialType MapMaterialType(int revitMatType)
        {
            // Your enum definition:
            // 1=STEEL, 2=CONCRETE, 3=NODESIGN, 4=ALUMINUM, 5=COLDFORMED, 6=REBAR, 7=TENDON
            switch (revitMatType)
            {
                case 1: return MaterialType.STEEL;
                case 2: return MaterialType.CONCRETE;
                case 3: return MaterialType.NODESIGN;
                case 4: return MaterialType.ALUMINUM;
                case 5: return MaterialType.COLDFORMED;
                case 6: return MaterialType.REBAR;
                case 7: return MaterialType.TENDON;
                default: return MaterialType.CONCRETE; // keep your class default if unknown
            }
        }
    }
}
