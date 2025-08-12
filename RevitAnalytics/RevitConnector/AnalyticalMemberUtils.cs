using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.RevitConnector
{
    class AnalyticalMemberUtils
    {
        public static string GetAnalyticalSectionName(AnalyticalMember analyticalMember, Document doc)
        {
            if (analyticalMember == null) return null;

            // The built-in parameter for the section type in Revit 2023+ is ANALYTICAL_MEMBER_SECTION_TYPE
            // But you need to confirm the correct BuiltInParameter or parameter GUID in your version.
            // If it is a shared parameter, you might retrieve it differently.
            BuiltInParameter bip = BuiltInParameter.ANALYTICAL_MEMBER_SECTION_TYPE;

            Parameter sectionParam = analyticalMember.get_Parameter(bip);
            if (sectionParam == null || !sectionParam.HasValue)
                return null;

            // The parameter is stored as an ElementId referencing the "type" element that defines the section.
            ElementId typeElemId = sectionParam.AsElementId();
            if (typeElemId == ElementId.InvalidElementId)
                return null;

            // Get the element that defines the section
            Element typeElem = doc.GetElement(typeElemId);
            if (typeElem == null)
                return null;

            // Typically, the element's Name property holds the name of the section (e.g., "Rect30x60")
            return typeElem.Name;
        }
        public static string GetMaterialName(AnalyticalMember analyticalMember, Document doc)
        {
            
                Element materialElem = doc.GetElement(analyticalMember.MaterialId);
                if (materialElem != null)
                {
                    Material material  = materialElem as Material;
                    //log found material name of an element
                    DebugHandler.Log($"Found material name {material.Name}.");
                    return material.Name; // Return the material name
                }
            
            return "UnknownMaterial"; // Default if not found
        }
        public static string GetMaterialName(AnalyticalPanel analyticalPanel, Document doc)
        {

            Element materialElem = doc.GetElement(analyticalPanel.MaterialId);
            if (materialElem != null)
            {
                Material material = materialElem as Material;
                //log found material name of an element
                DebugHandler.Log($"Found material name {material.Name}.");
                return material.Name; // Return the material name
            }

            return "UnknownMaterial"; // Default if not found
        }
    }

}
