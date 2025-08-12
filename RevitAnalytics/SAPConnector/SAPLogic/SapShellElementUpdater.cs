using RevitAnalytics.Core;
using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.SAPConnector.SAPLogic
{
    public static class SapShellElementUpdater
    {
        /// <summary>
        /// Updates the geometry (corner points) and section property of an existing SAP2000 shell element.
        /// Assumes that the shell element with the desired label already exists.
        /// </summary>
        /// <param name="sapModel">The SAP2000 model object</param>
        /// <param name="panelElem">The analytical panel info from Revit</param>
        public static void UpdateShellElement(cSapModel sapModel, AnalyticalPanelElementInfo panelElem)
        {
            // 1️⃣ Convert corner points (from Tuple<double, double, double>) to SapPoint objects
            List<SapPoint> sapCornerPoints = SapPoint.ConvertToSapPoints(sapModel, panelElem.CornerPoints);
            string[] newPointNames = sapCornerPoints.Select(p => p.Name).ToArray();

            int numberOfPoints = newPointNames.Length;
            string shellLabel = panelElem.Label; // The existing area object's name in SAP2000

            // 2️⃣ Change the connectivity using EditArea.ChangeConnectivity
            //    This modifies the existing area object's corner points in place
            int ret = sapModel.EditArea.ChangeConnectivity(shellLabel, numberOfPoints, ref newPointNames);

            if (ret != 0)
            {
                DebugHandler.LogWarning(
                    $"Failed to update shell connectivity for '{shellLabel}'. Error Code: {ret}"
                );
            }
            else
            {
                DebugHandler.Log(
                    $"Successfully updated shell connectivity for '{shellLabel}'."
                );
            }

            // 3️⃣ Update the section property if needed
            ret = sapModel.AreaObj.SetProperty(shellLabel, panelElem.SectionName);
            if (ret != 0)
            {
                DebugHandler.LogWarning(
                    $"Failed to update shell section property for '{shellLabel}'. Error Code: {ret}"
                );
            }
            else
            {
                DebugHandler.Log(
                    $"Successfully updated shell section property for '{shellLabel}' to '{panelElem.SectionName}'."
                );
            }
        }
    }
}
