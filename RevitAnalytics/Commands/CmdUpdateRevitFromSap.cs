using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RevitAnalytics.Core;
using RevitAnalytics.RevitConnector;
using SAP2000v1;
using System.Collections.Generic;

namespace RevitAnalytics
{
    [Transaction(TransactionMode.Manual)]
    public class CmdUpdateRevitFromSap : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            DebugHandler.ClearLog();
            DebugHandler.Log("Starting CmdUpdateRevitFromSap command.", DebugHandler.LogLevel.INFO);

            // 1️⃣ Connect to SAP2000
            cSapModel sapModel = SAPConnector.SAPFileConnectorcs.GetSapModel();
            if (sapModel == null)
            {
                DebugHandler.LogError("Unable to connect to SAP2000.", new System.Exception("SAP2000 instance is null."));
                TaskDialog.Show("Error", "Unable to connect to SAP2000.");
                return Result.Failed;
            }

            // 2️⃣ Retrieve Analytical Elements from SAP2000
            List<AnalyticalElementInfo> sapElements = SAPConnector.SapElementCollector.GetAllAnalyticalElementsFromSap(sapModel);

            if (sapElements == null || sapElements.Count == 0)
            {
                DebugHandler.Log("No analytical elements found in SAP2000.", DebugHandler.LogLevel.INFO);
                TaskDialog.Show("Info", "No analytical elements found in SAP2000.");
                return Result.Succeeded;
            }

            DebugHandler.Log($"Retrieved {sapElements.Count} analytical elements from SAP2000.", DebugHandler.LogLevel.INFO);

            // 3️⃣ Update Revit Model
            using (Transaction t = new Transaction(doc, "Update Analytical Models from SAP2000"))
            {
                t.Start();
                RevitElementUpdater.UpdateRevitElements(doc, sapElements);
                t.Commit();
                DebugHandler.Log("Transaction committed successfully.", DebugHandler.LogLevel.INFO);
            }

            TaskDialog.Show("Update Complete", "Revit analytical models have been updated based on SAP2000 data.");
            DebugHandler.Log("CmdUpdateRevitFromSap command completed successfully.", DebugHandler.LogLevel.INFO);
            DebugHandler.OpenLogFile();

            return Result.Succeeded;
        }
    }
}
