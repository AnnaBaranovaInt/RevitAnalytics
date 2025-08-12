using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RevitAnalytics.Core;
using System.Collections.Generic;

namespace RevitAnalytics.RevitConnector
{
    public static class RevitElementUpdater
    {
        public static void UpdateRevitElements(Document doc, List<AnalyticalElementInfo> sapElements)
        {
            try
            {
                HashSet<string> processedMaterials = new HashSet<string>(); // Track updated
                HashSet<ElementId> sapElementIds = new HashSet<ElementId>(); // Track elements that exist in SAP

                foreach (var info in sapElements)
                {
                    // 1️⃣ CHECK IF THE ELEMENT EXISTS
                    if (info.RevitId.Value == 0)
                        continue;

                    Element revitElem = doc.GetElement(info.RevitId);
                    if (revitElem == null)
                    {
                        DebugHandler.Log($"Element with RevitId {info.RevitId.Value} not found in Revit model.", DebugHandler.LogLevel.INFO);
                        continue;
                    }

                    DebugHandler.Log($"Found Revit element {info.RevitId.Value} ({revitElem.Name}).", DebugHandler.LogLevel.INFO);

                    // Ensure the element is an AnalyticalMember before casting
                    if (!(revitElem is AnalyticalMember) && !(revitElem is AnalyticalPanel))
                    {
                        DebugHandler.Log($"Element {info.RevitId.Value} is not an AnalyticalMember. Skipping update.", DebugHandler.LogLevel.WARNING);
                        continue;
                    }
                    sapElementIds.Add(info.RevitId);
                    //var elem = revitElem is AnalyticalPanel ? revitElem as AnalyticalPanel : revitElem as AnalyticalMember;


                    if (revitElem is AnalyticalPanel panel && info is AnalyticalPanelElementInfo panelInfo)
                    {
                        // 1) Update geometry
                        Updaters.GeometryUpdater.UpdateGeometry(panel, panelInfo);

                        // 2) Update material (only if not processed)
                        if (!processedMaterials.Contains(info.MaterialName))
                        {
                            UpdateMaterial(doc, panel, panelInfo);
                            processedMaterials.Add(info.MaterialName);
                        }
                        else
                        {
                            DebugHandler.Log($"🚀 Skipping material '{info.MaterialName}' (Already Updated).", DebugHandler.LogLevel.INFO);
                        }

                        // 3) Update section for panel
                        Updaters.SectionUpdater.UpdateSectionType(doc, panel, panelInfo);
                    }
                    else if (revitElem is AnalyticalMember member && info is AnalyticalFrameElementInfo frameInfo)
                    {
                        // 1) Update geometry
                        Updaters.GeometryUpdater.UpdateGeometry(member, frameInfo);

                        // 2) Update material (only if not processed)
                        if (!processedMaterials.Contains(info.MaterialName))
                        {
                            UpdateMaterial(doc, member, frameInfo);
                            processedMaterials.Add(info.MaterialName);
                        }
                        else
                        {
                            DebugHandler.Log($"🚀 Skipping material '{info.MaterialName}' (Already Updated).", DebugHandler.LogLevel.INFO);
                        }

                        // 3) Update section for frame
                        Updaters.SectionUpdater.UpdateSectionType(doc, member, frameInfo);
                    }
                }
                
            }
            catch (System.Exception ex)
            {
                DebugHandler.LogError("Error updating Revit elements.", ex);
            }
        }

        private static void UpdateMaterial(Document doc, AnalyticalMember am, AnalyticalElementInfo info)
        {
            string sapMaterialName = info.MaterialName;
            string revitMaterial = AnalyticalMemberUtils.GetMaterialName(am, doc);

            Material material = Utils.MaterialUtils.GetOrCreateMaterial(doc, info.MaterialInfo);

            Utils.MaterialUtils.SetMaterialPhysicalProperties(material, info.MaterialInfo, doc);

        }
        private static void UpdateMaterial(Document doc, AnalyticalPanel panel, AnalyticalElementInfo info)
        {
            string sapMaterialName = info.MaterialName;
            string revitMaterial = AnalyticalMemberUtils.GetMaterialName(panel, doc);

            Material material = Utils.MaterialUtils.GetOrCreateMaterial(doc, info.MaterialInfo);

            Utils.MaterialUtils.SetMaterialPhysicalProperties(material, info.MaterialInfo, doc);

        }
    }
}
