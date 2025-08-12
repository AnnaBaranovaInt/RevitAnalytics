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
    class GenerateAnalytics : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            DebugHandler.ClearLog();


            try
            {
                DebugHandler.Log("Starting Generate Analytics command.", DebugHandler.LogLevel.INFO);

                //collect all elements of a few specific categories

                //get their geometry, by element type (make an enumerationof elements)

                //Convert geometry into analytics

                //Connect each psysical element to its analytical counterpart

                //I suggest update parameters method here, so later we can use it, where we copy this method to make another, update coommand


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
