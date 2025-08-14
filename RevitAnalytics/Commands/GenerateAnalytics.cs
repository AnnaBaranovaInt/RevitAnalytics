using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAnalytics.Core;
using RevitAnalytics.RevitPnA;
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
    class GenerateAnalytics : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            DebugHandler.ClearLog();

            try
            {
                DebugHandler.Log("Starting Generate Analytics command.", DebugHandler.LogLevel.INFO);

                DebugHandler.Log("Collecting analytical elements...", DebugHandler.LogLevel.INFO);
                using (Transaction tx = new Transaction(doc, "Generate Analytics"))
                {
                    tx.Start();
                    List<RevitAnalyticalElementInfo> analyticalRevitInfos = CollectAnalytics.CollectAnalyticalElements(doc);
                    DebugHandler.Log($"Collected {analyticalRevitInfos?.Count ?? 0} analytical elements.", DebugHandler.LogLevel.INFO);

                    if (analyticalRevitInfos == null || analyticalRevitInfos.Count == 0)
                    {
                        DebugHandler.Log("No analytical elements found. Aborting command.", DebugHandler.LogLevel.WARNING);
                        DebugHandler.OpenLogFile();
                        return Result.Cancelled;
                    }

                    DebugHandler.Log("Connecting physical and analytical elements...", DebugHandler.LogLevel.INFO);
                    PnAConnector.ConnectPhysicalAndAnalytical(doc, analyticalRevitInfos);
                    DebugHandler.Log("Physical and analytical elements connected.", DebugHandler.LogLevel.INFO);

                    DebugHandler.Log("Updating parameters on analytical elements...", DebugHandler.LogLevel.INFO);
                    UpdatePars.UpdateParameters(doc, analyticalRevitInfos);
                    DebugHandler.Log("Parameters updated on analytical elements.", DebugHandler.LogLevel.INFO);

                    DebugHandler.Log("Trimming and extending analytical elements...", DebugHandler.LogLevel.INFO);
                    TrimExtend.TrimAndExtendAnalyticalElements(doc, analyticalRevitInfos);
                    DebugHandler.Log("Trim/extend operations completed on analytical elements.", DebugHandler.LogLevel.INFO);

                    tx.Commit();
                    DebugHandler.Log("Transaction committed successfully.", DebugHandler.LogLevel.INFO);
                }

                DebugHandler.OpenLogFile();
                DebugHandler.Log("Generate Analytics command completed successfully.", DebugHandler.LogLevel.INFO);
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
