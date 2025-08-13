using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.RevitPnA
{
    class PnAConnector
    {
        private static string GetElementMarkString(Element e)
        {
            if (e == null) return string.Empty;

            // 1) Instance "Mark"
            Parameter instMark = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            if (instMark != null && instMark.StorageType == StorageType.String)
            {
                string v = instMark.AsString();
                if (!string.IsNullOrEmpty(v)) return v;
            }

            // 2) Fallback to Type "Type Mark" (sometimes used as the identifying code)
            ElementId typeId = e.GetTypeId();
            if (typeId != null && typeId != ElementId.InvalidElementId)
            {
                Element typeElem = e.Document.GetElement(typeId);
                if (typeElem != null)
                {
                    Parameter typeMark = typeElem.get_Parameter(BuiltInParameter.DOOR_NUMBER);
                    if (typeMark != null && typeMark.StorageType == StorageType.String)
                    {
                        string tv = typeMark.AsString();
                        if (!string.IsNullOrEmpty(tv)) return tv;
                    }
                }
            }

            // 3) Last resort: by-name lookup (localized UIs still map correctly if param was created as "Mark")
            Parameter named = e.LookupParameter("Mark");
            if (named != null && named.StorageType == StorageType.String)
            {
                string nv = named.AsString();
                if (!string.IsNullOrEmpty(nv)) return nv;
            }

            return string.Empty;
        }

        // No transactions inside, as requested.
        public static void ConnectPhysicalAndAnalytical(Document doc, List<RevitAnalyticalElementInfo> analyticalRevitInfos)
        {
            if (doc == null || analyticalRevitInfos == null || analyticalRevitInfos.Count == 0)
            {
                DebugHandler.Log("No document or analytical elements provided for connection.", DebugHandler.LogLevel.WARNING);
                return;
            }

            DebugHandler.Log($"Starting connection of {analyticalRevitInfos.Count} analytical elements to physical elements.", DebugHandler.LogLevel.INFO);

            AnalyticalToPhysicalAssociationManager manager =
                AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc);

            int connectedCount = 0;
            int skippedCount = 0;

            foreach (RevitAnalyticalElementInfo info in analyticalRevitInfos)
            {
                if (info == null || info.Element == null || info.AnRevitId == null || info.PhRevitId == null)
                {
                    DebugHandler.Log("Skipped an entry due to missing element or IDs.", DebugHandler.LogLevel.WARNING);
                    skippedCount++;
                    continue;
                }
                if (info.AnRevitId == ElementId.InvalidElementId || info.PhRevitId == ElementId.InvalidElementId)
                {
                    DebugHandler.Log($"Skipped element with invalid IDs: AnalyticalId={info.AnRevitId}, PhysicalId={info.PhRevitId}", DebugHandler.LogLevel.WARNING);
                    skippedCount++;
                    continue;
                }

                // Avoid exception if already associated
                ElementId analyticalAssoc = manager.GetAssociatedElementId(info.AnRevitId);
                ElementId physicalAssoc = manager.GetAssociatedElementId(info.PhRevitId);

                bool analyticalFree = (analyticalAssoc == null || analyticalAssoc == ElementId.InvalidElementId);
                bool physicalFree = (physicalAssoc == null || physicalAssoc == ElementId.InvalidElementId);

                string markValue = GetElementMarkString(info.Element);

                DebugHandler.Log(
                    $"Attempting to connect AnalyticalId={info.AnRevitId}, PhysicalId={info.PhRevitId}, Mark='{markValue}'. " +
                    $"Analytical already associated: {!(analyticalFree)}, Physical already associated: {!(physicalFree)}.",
                    DebugHandler.LogLevel.INFO);

                if (analyticalFree && physicalFree)
                {
                    manager.AddAssociation(info.AnRevitId, info.PhRevitId);
                    DebugHandler.Log(
                        $"Connected AnalyticalId={info.AnRevitId} <-> PhysicalId={info.PhRevitId} (Mark='{markValue}').",
                        DebugHandler.LogLevel.INFO);
                    connectedCount++;
                }
                else
                {
                    DebugHandler.Log(
                        $"Skipped connection for AnalyticalId={info.AnRevitId} and PhysicalId={info.PhRevitId} because one or both are already associated.",
                        DebugHandler.LogLevel.INFO);
                    skippedCount++;
                }



                // Set Mark on analytical element if available
                if (!string.IsNullOrEmpty(markValue))
                {
                    Element analyticalElem = (info.AnalyticalMember != null)
                        ? (Element)info.AnalyticalMember
                        : doc.GetElement(info.AnRevitId);


                    if (analyticalElem != null)
                    {
                        IList<Parameter> allParams = analyticalElem.Parameters.Cast<Parameter>().ToList();
                        foreach (Parameter param in allParams)
                        {
                            string paramName = param.Definition.Name;
                            string paramType = param.StorageType.ToString();
                            string paramValue = "";
                            try
                            {
                                switch (param.StorageType)
                                {
                                    case StorageType.String:
                                        paramValue = param.AsString();
                                        break;
                                    case StorageType.Double:
                                        paramValue = param.AsDouble().ToString();
                                        break;
                                    case StorageType.Integer:
                                        paramValue = param.AsInteger().ToString();
                                        break;
                                    case StorageType.ElementId:
                                        paramValue = param.AsElementId().ToString();
                                        break;
                                }
                            }
                            catch { paramValue = "(error reading value)"; }

                            DebugHandler.Log(
                                $"[DEBUG] AnalyticalId={info.AnRevitId}: Parameter '{paramName}' (Type={paramType}) Value='{paramValue}'",
                                DebugHandler.LogLevel.DEBUG);
                        }

                        Parameter physRepParam = analyticalElem.LookupParameter("ITS_Physical Representation");
                        if (physRepParam != null && physRepParam.StorageType == StorageType.String)
                        {
                            bool setResult = physRepParam.Set(markValue);
                            DebugHandler.Log(
                                $"Set 'ITS_Physical Representation' to '{markValue}' for AnalyticalId={info.AnRevitId}. Success: {setResult}",
                                DebugHandler.LogLevel.INFO);
                        }
                        else
                        {
                            DebugHandler.Log(
                                $"Parameter 'ITS_Physical Representation' not found or not string for AnalyticalId={info.AnRevitId}.",
                                DebugHandler.LogLevel.WARNING);
                        }
                    }
                    else
                    {
                        DebugHandler.Log(
                            $"Analytical element not found for AnalyticalId={info.AnRevitId}.",
                            DebugHandler.LogLevel.WARNING);
                    }
                }
            }

            DebugHandler.Log($"Finished connecting analytical and physical elements. Connected: {connectedCount}, Skipped: {skippedCount}.", DebugHandler.LogLevel.INFO);
        }


    }

}
