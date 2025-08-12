using Autodesk.Revit.DB;
using RevitAnalytics.Core;
using RevitAnalytics.SAPConnector.SAPLogic;
using SAP2000v1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.SAPConnector
{
    class SapElementCreator
    {
        public static void CreateNewFrame(cSapModel sapModel, AnalyticalFrameElementInfo info)
        {
            string sp = "RevitStartPoint", ep = "RevitStartPoint";
            sapModel.PointObj.AddCartesian(info.StartX, info.StartY, info.StartZ, ref sp);
            sapModel.PointObj.AddCartesian(info.EndX, info.EndY, info.EndZ, ref ep);

            string assignedLabel = info.Label;
            string assignedName = info.SectionName;
            sapModel.FrameObj.AddByPoint(sp, ep, ref assignedLabel, assignedName, info.Label);
        }
        public static void CreateSapElementsFromAnalytical(cSapModel sapModel, List<AnalyticalElementInfo> elements)
        {
            DebugHandler.Log("Creating SAP elements (frames & shells) from collected analytical elements.", DebugHandler.LogLevel.INFO);

            foreach (var elem in elements)
            {
                string sectionName = elem.SectionName;
                string materialName = elem.MaterialName;

                // Ensure material exists or create it
                if (!Utils.MaterialUtils.DoesMaterialExist(sapModel, materialName))
                {
                    DebugHandler.Log($"Material {materialName} does not exist. Creating...", DebugHandler.LogLevel.INFO);
                    Utils.MaterialUtils.CreateOrUpdateSapMaterial(sapModel, elem.MaterialInfo);
                }

                elem.MaterialType = elem.MaterialInfo.Type;

                if (elem.IsFrameElement) // ✅ FRAME (Beams/Columns)
                {
                    var frameElem = elem as AnalyticalFrameElementInfo;
                    SapPoint point1 = new SapPoint(sapModel, frameElem.StartX, frameElem.StartY, frameElem.StartZ);
                    SapPoint point2 = new SapPoint(sapModel, frameElem.EndX, frameElem.EndY, frameElem.EndZ);

                    // Check if section exists
                    if (!Utils.SectionUtils.SectionExists(sapModel, sectionName))
                    {
                        int sectionColor = Colors.GetNextSectionColor(); // Genera el color solo una vez
                        //log the selectd color

                        DebugHandler.Log($"Creating new frame section {sectionName} in SAP2000. With a color {sectionColor}", DebugHandler.LogLevel.INFO);
                        //new SapRectangularSection(sapModel, materialName, sectionName, frameElem.Width, frameElem.Depth, frameElem.Notes, frameElem.GUID, frameElem.Color);

                        // Determine action based on Material Type
                        switch (elem.MaterialType)
                        {
                            case MaterialType.CONCRETE:
                                DebugHandler.Log($"Creating new CONCRETE rectangular section '{sectionName}'.", DebugHandler.LogLevel.INFO);
                                // Create concrete rectangular section
                                new SapRectangularSection(
                                    sapModel,
                                    materialName,
                                    sectionName,
                                    frameElem.Width,
                                    frameElem.Depth,
                                    $"Created: {DateTime.Today.ToShortDateString()}; Author: INTNextGen ({Environment.UserName})",
                                    frameElem.GUID,
                                    sectionColor
                                );
                                break;

                            case MaterialType.STEEL:
                                DebugHandler.Log($"Importing new STEEL section '{sectionName}' from database.", DebugHandler.LogLevel.INFO);
                                // Steel sections should be imported from the database
                                string databaseFile = "INT_Europe_Updated.xml";//"ArcelorMittal_Europe.xml"; // Adjust based on your SAP2000 installation/database
                                //Can you please cehck if this file really exist in this path?
                                string fullPath =  PathManager.GetFilePath(databaseFile);
                                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                                {
                                    DebugHandler.LogError($"Database file '{databaseFile}' not found at path: {fullPath}", new FileNotFoundException($"Database file '{databaseFile}' not found."));
                                    continue; // Skip this section creation
                                }
                                string dbSectionName = sectionName;    // Assumes your section name matches database section name
                                int ret = sapModel.PropFrame.ImportProp(
                                    sectionName,        // NewPropName in SAP model
                                    materialName,       // Material
                                    fullPath,       // FileName (e.g., "Sections8.pro", "AISC14.xml")
                                    dbSectionName,      // SectionNameInFile
                                    sectionColor,    // Optional color
                                    $"Created {DateTime.Today.ToShortDateString()}; Author: INTNextGen ({Environment.UserName}); Database: {PathManager.GetBaseFolderPath()}/{databaseFile}",    // Optional notes
                                    frameElem.GUID      // Optional GUID
                                );

                                if (ret == 0)
                                {
                                    DebugHandler.Log($"Section '{sectionName}' was found and successfully imported from the database '{databaseFile}'.", DebugHandler.LogLevel.INFO);
                                }
                                else
                                {
                                    DebugHandler.LogWarning($"Section '{sectionName}' was NOT found in the database '{databaseFile}' or import failed. Error code: {ret}");
                                }

                                break;

                            default:
                                DebugHandler.LogWarning($"⚠ Unsupported material type '{elem.MaterialType}' for section '{sectionName}'.");
                                break;
                        }
                    }

                    // ✅ Create Frame Element
                    var sapFrame = new SapFrameElement(sapModel, point1, point2, sectionName, frameElem.Label, frameElem.Label);
                    
                }
                else if (elem.IsPanelElement) // ✅ SHELL (Walls/Floors)
                {

                    var panelElem = elem as AnalyticalPanelElementInfo;

                    // Check if section exists
                    if (!Utils.SectionUtils.SectionExists(sapModel, sectionName))
                    {
                        DebugHandler.Log($"Creating new shell section {sectionName} in SAP2000.", DebugHandler.LogLevel.INFO);
                        new SapShellSection(sapModel, materialName, sectionName, panelElem.Thickness, panelElem.MatAngle, panelElem.BendingThikness, (int)panelElem.ShellType, panelElem.Color, panelElem.Notes, panelElem.GUID);
                    }

                    // ✅ Create Shell ElementCould not retrieve existing
                    List<SapPoint> sapCornerPoints = SapPoint.ConvertToSapPoints(sapModel, panelElem.CornerPoints);

                    var sapShell = new SapShellElement(sapModel, sapCornerPoints, sectionName, panelElem.Label);
                }
                else
                {
                    DebugHandler.Log($"Skipping unknown element type: {elem.Label}", DebugHandler.LogLevel.WARNING);
                }
            }

            DebugHandler.Log("SAP elements created successfully.", DebugHandler.LogLevel.INFO);
        }

    }
}
