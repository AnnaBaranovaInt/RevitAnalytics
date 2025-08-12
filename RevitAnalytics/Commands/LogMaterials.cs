using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAnalytics.Core;
using RevitAnalytics.SAPConnector;
using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics
{
    [Transaction(TransactionMode.Manual)]
    class LogMaterials : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            DebugHandler.ClearLog();


            try
            {
                DebugHandler.Log("Starting ExportAnalyticalModelToSap command.", DebugHandler.LogLevel.INFO);

                // 2) Gather Analytical beams/columns from Revit
                DebugHandler.Log("Collecting analytical beams and columns from Revit.", DebugHandler.LogLevel.INFO);

                List<AnalyticalElementInfo> analyticalElems = RevitConnector.RevitAnalyticalElementCollector.GetAllAnalyticalElementsFromRevit(doc);

                DebugHandler.Log($"Collected {analyticalElems.Count} analytical elements.", DebugHandler.LogLevel.INFO);


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
