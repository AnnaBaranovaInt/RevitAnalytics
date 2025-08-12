using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.SAPConnector.Utils
{
    class SectionUtils
    {
        public static bool SectionExists(cSapModel sapModel, string sectionName)
        {
            // 1️⃣ Check frame section properties
            int numFrameProps = 0;
            string[] framePropNames = null;
            sapModel.PropFrame.GetNameList(ref numFrameProps, ref framePropNames);

            if (framePropNames != null)
            {
                foreach (string prop in framePropNames)
                {
                    DebugHandler.Log($"Existing FRAME section in SAP2000: {prop}", DebugHandler.LogLevel.INFO);
                    if (prop.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // 2️⃣ Check area (shell) section properties
            int numAreaProps = 0;
            string[] areaPropNames = null;
            sapModel.PropArea.GetNameList(ref numAreaProps, ref areaPropNames);

            if (areaPropNames != null)
            {
                foreach (string prop in areaPropNames)
                {
                    DebugHandler.Log($"Existing AREA (shell) section in SAP2000: {prop}", DebugHandler.LogLevel.INFO);
                    if (prop.Equals(sectionName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // If not found in either list, section does not exist
            return false;
        }

        /// <summary>
        /// Updates or creates a section property in SAP2000.
        /// </summary>
        public static void UpdateSectionProperties(cSapModel sapModel, string sectionName, string materialName, double depth, double width)
        {
            bool exists = SectionExists(sapModel, sectionName);

            if (exists)
            {
                DebugHandler.Log($"Updating section properties for {sectionName}.", DebugHandler.LogLevel.INFO);

                // Update existing section properties
                int ret = sapModel.PropFrame.SetRectangle(sectionName, materialName, depth, width);
                if (ret == 0)
                {
                    DebugHandler.Log($"✅ Section {sectionName} updated successfully.", DebugHandler.LogLevel.INFO);
                }
                else
                {
                    DebugHandler.Log($"❌ ERROR: Failed to update section {sectionName}.", DebugHandler.LogLevel.ERROR);
                }
            }
            else
            {
                DebugHandler.Log($"Creating new section {sectionName}.", DebugHandler.LogLevel.INFO);

                // Create a new rectangular section
                int ret = sapModel.PropFrame.SetRectangle(sectionName, materialName, depth, width);
                if (ret == 0)
                {
                    DebugHandler.Log($"✅ New section {sectionName} created successfully.", DebugHandler.LogLevel.INFO);
                }
                else
                {
                    DebugHandler.Log($"❌ ERROR: Failed to create section {sectionName}.", DebugHandler.LogLevel.ERROR);
                }
            }
        }


        public static string GetSectionName(cSapModel sapModel, string label)
        {
            string sectionName = "", sAuto = "";
            int result = sapModel.FrameObj.GetSection(label, ref sectionName, ref sAuto);

            if (!string.IsNullOrEmpty(sectionName))
            {
                DebugHandler.Log($"✅ Section name for {label}: {sectionName}.", DebugHandler.LogLevel.INFO);
                return sectionName;
            }

            DebugHandler.Log($"⚠ WARNING: Section name for {label} is empty or undefined.", DebugHandler.LogLevel.WARNING);
            return "UnknownSection";
        }
    }
}
