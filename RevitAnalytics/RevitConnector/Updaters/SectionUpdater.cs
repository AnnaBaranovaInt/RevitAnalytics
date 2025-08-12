using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitAnalytics.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.RevitConnector.Updaters
{
    class SectionUpdater
    {
        public static void UpdateSectionType(Document doc, AnalyticalMember am, AnalyticalElementInfo info)
        {
            //log that we detected analitical member for section update
            DebugHandler.Log($"Detected AnalyticalMember for section update.", DebugHandler.LogLevel.INFO);
            try
            {
                string sapSection = info.SectionName;
                string revitSection = AnalyticalMemberUtils.GetAnalyticalSectionName(am, doc);

                if (!revitSection.Equals(sapSection, System.StringComparison.OrdinalIgnoreCase))
                {
                    ElementId sectionTypeId = GetSectionTypeIdByName(doc, sapSection);
                    if (sectionTypeId != ElementId.InvalidElementId)
                    {
                        am.SectionTypeId = sectionTypeId;
                        DebugHandler.Log($"Updated section type from '{revitSection}' to '{sapSection}' for element {info.RevitId.Value}.", DebugHandler.LogLevel.INFO);
                    }
                    else
                    {
                        DebugHandler.Log($"⚠ WARNING: Section type '{sapSection}' not found in Revit!", DebugHandler.LogLevel.WARNING);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugHandler.LogError($"Could not update section type for element {info.RevitId.Value}.", ex);
            }
            }

        public static void UpdateSectionType(Document doc, AnalyticalPanel panel, AnalyticalElementInfo info)
        {
            //log that we detected anaytical panel for section update
            DebugHandler.Log($"Detected AnalyticalPanel for section update.", DebugHandler.LogLevel.INFO);
            try
            {
                if (info is AnalyticalPanelElementInfo panelInfo)
                {
                    UpdatePanelThickness(panel, panelInfo);
                }
            }
            catch (System.Exception ex)
            {
                DebugHandler.LogError($"Could not update panel thickness for element {info.RevitId.Value}.", ex);
            }
        }
        public static void UpdatePanelThickness(AnalyticalPanel panel, AnalyticalPanelElementInfo info)
        {
            try
            {
                panel.Thickness = UnitUtils.ConvertToInternalUnits(info.Thickness, UnitTypeId.Meters);
                DebugHandler.Log($"Updated panel thickness for element {info.RevitId.Value} to {info.Thickness}m.", DebugHandler.LogLevel.INFO);
            }
            catch (System.Exception ex)
            {
                DebugHandler.LogError($"Could not update panel thickness for element {info.RevitId.Value}.", ex);
            }
        }

        public static ElementId GetSectionTypeIdByName(Document doc, string sectionName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming);

            foreach (Element element in collector)
            {
                if (element is FamilySymbol symbol && symbol.Name.Equals(sectionName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return symbol.Id;
                }
            }

            return ElementId.InvalidElementId;
        }
    }
}
