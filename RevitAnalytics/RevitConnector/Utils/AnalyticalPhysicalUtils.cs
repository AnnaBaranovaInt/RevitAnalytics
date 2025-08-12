using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.RevitConnector.Utils
{
    class AnalyticalPhysicalUtils
    {
        /// <summary>
        /// Retrieves the AnalyticalPanel associated with a given Physical Element (Wall, Floor, etc.).
        /// </summary>
        public static AnalyticalPanel GetAnalyticalElement(Document doc, Element physicalElement)
        {
            if (physicalElement == null) return null;

            var associationManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc);

            // Get the associated Analytical Element ID
            if (associationManager.HasAssociation(physicalElement.Id))
            {
                ElementId analyticalId = associationManager.GetAssociatedElementId(physicalElement.Id);
                return doc.GetElement(analyticalId) as AnalyticalPanel;
            }

            return null; // No associated analytical element
        }

        /// <summary>
        /// Retrieves the Physical Element associated with a given AnalyticalPanel.
        /// </summary>
        public static Element GetPhysicalElement(Document doc, AnalyticalPanel analyticalElement)
        {
            if (analyticalElement == null) return null;

            var associationManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc);

            // Get the associated Physical Element ID
            if (associationManager.HasAssociation(analyticalElement.Id))
            {
                ElementId physicalId = associationManager.GetAssociatedElementId(analyticalElement.Id);
                return doc.GetElement(physicalId);
            }

            return null; // No associated physical element
        }
    }
}
