using RevitAnalytics.Core;
using RevitAnalytics.SAPConnector.SAPLogic;
using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.SAPConnector
{
    public static class SapElementUpdater
    {
        public static void UpdateSAPModelFromRevit(List<AnalyticalElementInfo> elements, cSapModel sapModel)
        {
            // 1️⃣ Retrieve all existing SAP elements
            HashSet<string> sapFrameLabels = GetAllSAPFrames(sapModel);
            HashSet<string> sapShellLabels = GetAllSAPShells(sapModel);

            // 2️⃣ Track all Revit elements
            HashSet<string> revitLabels = new HashSet<string>();
            foreach (var elem in elements)
            {
                if (!string.IsNullOrEmpty(elem.Label))
                    revitLabels.Add(elem.Label);
            }

            // 3️⃣ Update existing elements and create new ones
            foreach (var elem in elements)
            {
                if (elem is AnalyticalFrameElementInfo frameElem)
                {
                    ProcessFrameElement(sapModel, frameElem);
                }
                else if (elem is AnalyticalPanelElementInfo panelElem)
                {
                    ProcessPanelElement(sapModel, panelElem);
                }
                else
                {
                    DebugHandler.Log($"Skipping unknown element type: {elem.Label}", DebugHandler.LogLevel.WARNING);
                }
            }

            // 4️⃣ Delete missing elements
            DeleteMissingElements(sapModel, revitLabels, sapFrameLabels, sapShellLabels);
        }
        private static void DeleteMissingElements(cSapModel sapModel, HashSet<string> revitLabels, HashSet<string> sapFrameLabels, HashSet<string> sapShellLabels)
        {
            // DELETE FRAMES that exist in SAP2000 but not in Revit
            foreach (string label in sapFrameLabels)
            {
                if (!revitLabels.Contains(label))
                {
                    DebugHandler.Log($"Deleting frame '{label}' from SAP2000 (not found in Revit).", DebugHandler.LogLevel.INFO);
                    sapModel.FrameObj.Delete(label);
                }
            }

            // DELETE SHELLS that exist in SAP2000 but not in Revit
            foreach (string label in sapShellLabels)
            {
                if (!revitLabels.Contains(label))
                {
                    DebugHandler.Log($"Deleting shell '{label}' from SAP2000 (not found in Revit).", DebugHandler.LogLevel.INFO);
                    sapModel.AreaObj.Delete(label);
                }
            }
        }


        private static void ProcessFrameElement(cSapModel sapModel, AnalyticalFrameElementInfo elem)
        {
            bool exists = CheckIfFrameExists(sapModel, elem.Label);
            if (exists)
            {
                DebugHandler.Log($"Frame '{elem.Label}' exists. Updating existing frame.", DebugHandler.LogLevel.INFO);
                UpdateFrameCoordinates(sapModel, elem);
                Utils.MaterialUtils.CreateOrUpdateSapMaterial(sapModel, elem.MaterialInfo);
                UpdateFrameSection(sapModel, elem);
            }
            else
            {
                DebugHandler.Log($"Frame '{elem.Label}' does NOT exist. Creating new frame.", DebugHandler.LogLevel.INFO);
                SapElementCreator.CreateNewFrame(sapModel, elem);
            }
        }
        private static void ProcessPanelElement(cSapModel sapModel, AnalyticalPanelElementInfo elem)
        {
            bool exists = CheckIfShellExists(sapModel, elem.Label);

            if (exists)
            {
                DebugHandler.Log($"Shell '{elem.Label}' exists. Updating existing shell.", DebugHandler.LogLevel.INFO);

                // ✅ Update existing shell geometry & section
                SapShellElementUpdater.UpdateShellElement(sapModel, elem);
            }
            else
            {
                DebugHandler.Log($"Shell '{elem.Label}' does NOT exist. Creating new shell.", DebugHandler.LogLevel.INFO);

                // ✅ Ensure section exists before creating the shell
                if (!Utils.SectionUtils.SectionExists(sapModel, elem.SectionName))
                {
                    DebugHandler.Log($"Creating new shell section {elem.SectionName} in SAP2000.", DebugHandler.LogLevel.INFO);
                    new SapShellSection(sapModel, elem.MaterialName, elem.SectionName, elem.Thickness, elem.MatAngle,
                                        elem.BendingThikness, (int)elem.ShellType, elem.Color, elem.Notes, elem.GUID);
                }

                // ✅ Ensure material exists
                Utils.MaterialUtils.CreateOrUpdateSapMaterial(sapModel, elem.MaterialInfo);

                // ✅ Convert corner points & create new shell
                List<SapPoint> sapCornerPoints = SapPoint.ConvertToSapPoints(sapModel, elem.CornerPoints);
                var sapShell = new SapShellElement(sapModel, sapCornerPoints, elem.SectionName, elem.Label);
            }
        }

        private static void UpdateFrameSection(cSapModel sapModel, AnalyticalElementInfo info)
        {
            string currentSection = "";
            //sapModel.FrameObj.GetSection(info.Label, ref currentSection);

            //Log that here will be the logic to update the section
            DebugHandler.Log($"Frame {info.Label} section logic for update.", DebugHandler.LogLevel.INFO);

            if (currentSection != info.SectionName)
            {
                sapModel.FrameObj.SetSection(info.Label, info.SectionName);
                DebugHandler.Log($"Updated section for {info.Label}: {info.SectionName}.", DebugHandler.LogLevel.INFO);
            }
        }
        private static HashSet<string> GetAllSAPFrames(cSapModel sapModel)
        {
            int numFrames = 0;
            string[] frameNames = null;
            sapModel.FrameObj.GetNameList(ref numFrames, ref frameNames);

            return frameNames != null ? new HashSet<string>(frameNames) : new HashSet<string>();
        }

        private static HashSet<string> GetAllSAPShells(cSapModel sapModel)
        {
            int numShells = 0;
            string[] shellNames = null;
            sapModel.AreaObj.GetNameList(ref numShells, ref shellNames);

            return shellNames != null ? new HashSet<string>(shellNames) : new HashSet<string>();
        }

        private static bool CheckIfFrameExists(cSapModel sapModel, string label)
        {
            string startPt = "", endPt = "";
            int ret = sapModel.FrameObj.GetPoints(label, ref startPt, ref endPt);
            return (ret == 0);
        }
        private static bool CheckIfShellExists(cSapModel sapModel, string label)
        {
            int numPoints = 0;
            string[] pointNames = null;
            int ret = sapModel.AreaObj.GetPoints(label, ref numPoints, ref pointNames);
            return (ret == 0);
        }

        private static void UpdateFrameCoordinates(cSapModel sapModel, AnalyticalFrameElementInfo info)
        {
            string sp = "", ep = "";
            int ret = sapModel.FrameObj.GetPoints(info.Label, ref sp, ref ep);
            if (ret != 0) return;

            double currentStartX = 0, currentStartY = 0, currentStartZ = 0;
            double currentEndX = 0, currentEndY = 0, currentEndZ = 0;
            sapModel.PointObj.GetCoordCartesian(sp, ref currentStartX, ref currentStartY, ref currentStartZ);
            sapModel.PointObj.GetCoordCartesian(ep, ref currentEndX, ref currentEndY, ref currentEndZ);

            double tol = 1e-6;
            bool startChanged = Math.Abs(currentStartX - info.StartX) > tol ||
                                Math.Abs(currentStartY - info.StartY) > tol ||
                                Math.Abs(currentStartZ - info.StartZ) > tol;
            bool endChanged = Math.Abs(currentEndX - info.EndX) > tol ||
                              Math.Abs(currentEndY - info.EndY) > tol ||
                              Math.Abs(currentEndZ - info.EndZ) > tol;

            if (!startChanged && !endChanged)
            {
                // Coordinates are essentially identical; no update needed. //log it
                DebugHandler.Log($"Frame {info.Label} coordinates are identical; no update needed.", DebugHandler.LogLevel.INFO);
                return;
            }

            sapModel.EditPoint.ChangeCoordinates_1(sp, info.StartX, info.StartY, info.StartZ, false);
            sapModel.EditPoint.ChangeCoordinates_1(ep, info.EndX, info.EndY, info.EndZ, false);
            DebugHandler.Log($"Frame {info.Label} coordinates updated.", DebugHandler.LogLevel.INFO);
        }
    }

}
