using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitAnalytics.SAPConnector.SAPLogic
{
    public class SapShellElement
    {
        public string Label { get; private set; }

        public SapShellElement(cSapModel sapModel, List<SapPoint> cornerPoints, string sectionName, string desiredLabel)
        {
            Label = desiredLabel;
            int numPoints = cornerPoints.Count;

            if (numPoints < 3)
            {
                DebugHandler.LogWarning($"SAP2000 requires at least 3 points to define a shell element, but got {numPoints}.");
                return;
            }

            // Convert corner points to SAP2000 format
            string[] pointNames = cornerPoints.Select(p => p.Name).ToArray();

            // Use a temporary variable to capture SAP2000’s auto-assigned label
            string autoAssignedLabel = "";

            // Create the shell element (SAP2000 assigns its own label automatically)
            int ret = sapModel.AreaObj.AddByPoint(numPoints, ref pointNames, ref autoAssignedLabel);

            if (ret != 0)
            {
                DebugHandler.LogError($"SAP2000 failed to create shell element with desired label '{desiredLabel}'. Error Code: {ret}",
                    new Exception($"Failed creation, SAP returned error code: {ret}"));
                return;
            }
            else
            {
                DebugHandler.Log($"Created shell element with SAP2000-assigned label: '{autoAssignedLabel}'.", DebugHandler.LogLevel.INFO);
            }

            // Rename the SAP2000 shell element to your intended label if needed
            if (!autoAssignedLabel.Equals(desiredLabel, StringComparison.InvariantCultureIgnoreCase))
            {
                ret = sapModel.AreaObj.ChangeName(autoAssignedLabel, desiredLabel);
                if (ret == 0)
                {
                    DebugHandler.Log($"Successfully renamed shell from '{autoAssignedLabel}' to intended label '{desiredLabel}'.", DebugHandler.LogLevel.INFO);
                    Label = desiredLabel;
                }
                else
                {
                    DebugHandler.LogWarning($"Failed to rename shell from '{autoAssignedLabel}' to '{desiredLabel}'. Error Code: {ret}. Keeping original label '{autoAssignedLabel}'.");
                    Label = autoAssignedLabel; // fallback
                }
            }

            // Assign the section property to the shell element
            ret = sapModel.AreaObj.SetProperty(Label, sectionName);
            if (ret != 0)
            {
                DebugHandler.LogWarning($"Failed to assign section '{sectionName}' to shell '{Label}'. Error Code: {ret}");
            }
            else
            {
                DebugHandler.Log($"Assigned section '{sectionName}' to shell '{Label}'.", DebugHandler.LogLevel.INFO);
            }
        }
    }
}
