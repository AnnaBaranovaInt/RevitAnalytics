using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SAP2000v1;
using System;
using RevitAnalytics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Structure;
using RevitAnalytics.Core;
using RevitAnalytics.SAPConnector;

namespace RevitAnalytics
{
    // Revit command that collects analytical elements and exports them to SAP2000
    [Transaction(TransactionMode.Manual)]
    public class ExportAnalyticalModelToSap : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            DebugHandler.ClearLog();


            try
            {
                DebugHandler.Log("Starting ExportAnalyticalModelToSap command.", DebugHandler.LogLevel.INFO);

                // 1) Get or create your SAP2000 instance and model
                DebugHandler.Log("Attempting to attach or create SAP2000 instance.", DebugHandler.LogLevel.INFO);
                cOAPI sapObject = SAPFileConnectorcs.AttachOrCreateSapObject();
                if (sapObject == null)
                {
                    DebugHandler.LogError("Could not open/attach SAP2000.", new Exception("SAP2000 instance is null."));
                    TaskDialog.Show("Error", "Could not open/attach SAP2000.");
                    return Result.Failed;
                }

                cSapModel sapModel = sapObject.SapModel;
                DebugHandler.Log("SAP2000 instance attached/created successfully.", DebugHandler.LogLevel.INFO);
                SAPConnector.SAPFileConnectorcs.InitializeSapModel(sapModel);

                // 2) Gather Analytical beams/columns from Revit
                DebugHandler.Log("Collecting analytical beams and columns from Revit.", DebugHandler.LogLevel.INFO);

                List<AnalyticalElementInfo> analyticalElems = RevitConnector.RevitAnalyticalElementCollector.GetAllAnalyticalElementsFromRevit(doc);

                DebugHandler.Log($"Collected {analyticalElems.Count} analytical elements.", DebugHandler.LogLevel.INFO);

                // 3) Create corresponding SAP frame elements
                DebugHandler.Log("Creating SAP frame elements from analytical elements.", DebugHandler.LogLevel.INFO);

                SapElementCreator.CreateSapElementsFromAnalytical(sapModel, analyticalElems);


                TaskDialog.Show("Export to SAP2000", $"Exported {analyticalElems.Count} analytical elements to SAP2000!");
                DebugHandler.Log("Export to SAP2000 completed successfully.", DebugHandler.LogLevel.INFO);

                DebugHandler.OpenLogFile();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                DebugHandler.LogError("An error occurred during the export process.", ex);
                message = ex.Message;
                DebugHandler.OpenLogFile();
                return Result.Failed;
            }
        }
    }
}
