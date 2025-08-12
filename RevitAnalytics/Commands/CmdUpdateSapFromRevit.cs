using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAnalytics.Core;
using SAP2000v1;
using System;
using System.Collections.Generic;

namespace RevitAnalytics
{
    [Transaction(TransactionMode.Manual)]
    public class CmdUpdateSapFromRevit : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            DebugHandler.ClearLog();
            DebugHandler.Log("Starting CmdUpdateSapFromRevit command.", DebugHandler.LogLevel.INFO);

            try
            {
                // 1️⃣ Collect Revit analytical elements
                List<AnalyticalElementInfo> revitElements =
                    RevitConnector.RevitAnalyticalElementCollector.GetAllAnalyticalElementsFromRevit(doc);

                if (revitElements.Count == 0)
                {
                    DebugHandler.Log("No Revit analytical elements found.", DebugHandler.LogLevel.INFO);
                    TaskDialog.Show("Update SAP", "No Revit analytical elements found.");
                    return Result.Succeeded;
                }

                // 2️⃣ Connect to or create SAP2000 instance
                cSapModel sapModel = GetSapModel();
                if (sapModel == null)
                {
                    DebugHandler.LogError("Could not connect to SAP2000.", new Exception("SAP2000 instance is null."));
                    TaskDialog.Show("Error", "Could not connect to SAP2000.");
                    return Result.Failed;
                }

                // 3️⃣ Update or create elements in SAP2000
                SAPConnector.SapElementUpdater.UpdateSAPModelFromRevit(revitElements, sapModel);

                DebugHandler.Log("SAP2000 updated from Revit successfully.", DebugHandler.LogLevel.INFO);
                DebugHandler.OpenLogFile();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        

        private cSapModel GetSapModel()
        {
            cOAPI sapObject = null;
            try
            {
                // Try attaching to an active SAP2000 instance
                sapObject = (cOAPI)System.Runtime.InteropServices.Marshal
                    .GetActiveObject("CSI.SAP2000.API.SapObject");
            }
            catch
            {
                // If none running, create new
                cHelper myHelper = new Helper();
                sapObject = myHelper.CreateObjectProgID("CSI.SAP2000.API.SapObject");
                sapObject.ApplicationStart();
            }

            return sapObject?.SapModel;
        }
    }
}
