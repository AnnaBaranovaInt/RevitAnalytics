using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RevitAnalytics
{
    [Transaction(TransactionMode.Manual)]
    public class CommandWPF : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // 1) Access Document
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                // Create and show the WPF window
                DebugHandler.ClearLog();

                DebugHandler.Log("Command started", DebugHandler.LogLevel.INFO);
                UIWindow window = new UIWindow();
                bool? dialogResult = window.ShowDialog();


                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return Result.Failed;
            }
        }
    }
}
