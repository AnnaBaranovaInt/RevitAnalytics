using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace RevitAnalytics
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                string tabName = "RevitAnalytics";
                application.CreateRibbonTab(tabName);

                string panelName = "RevitAnalytics";
                RibbonPanel panel = application.CreateRibbonPanel(tabName, panelName);


                string imagePath1 = PathManager.GetImagePath("1.png");
                // Get the path to this assembly
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                AddPushButton(panel, "TriggerSAP", "Trigger\nSAP 2000", "RevitAnalytics.CmdToSap", imagePath1, "Trigger SAP 2000.");
                AddPushButton(panel, "ImportToSAP", "Import to\nSAP 2000", "RevitAnalytics.ExportAnalyticalModelToSap", imagePath1, "Create the model in SAP 2000.");
                AddPushButton(panel, "UpdateFromSAP", "Update From\nSAP 2000", "RevitAnalytics.CmdUpdateRevitFromSap", imagePath1, "Update the model from SAP 2000.");
                AddPushButton(panel, "UpdateSAPFromRevit", "Update SAP\nFrom Revit", "RevitAnalytics.CmdUpdateSapFromRevit", imagePath1, "Update SAP 2000 from Revit.");
                AddPushButton(panel, "GenerateAnalytics", "Generate\nAnalytics", "RevitAnalytics.GenerateAnalytics", imagePath1, "GenerateAnalytics.");

                AddPushButton(panel, "LogMaterials", "Log\nMaterails", "RevitAnalytics.LogMaterials", imagePath1, "Log the materials.");

                //CmdUpdateSapFromRevit

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Cleanup if needed.
            return Result.Succeeded;
        }


        private void AddPushButton(RibbonPanel panel, string buttonName, string buttonText, string className, string imagePath, string tooltip)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData(buttonName, buttonText, assemblyPath, className);

            // Set the icon for the button
            BitmapImage largeImage = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            buttonData.LargeImage = largeImage;

            PushButton pushButton = panel.AddItem(buttonData) as PushButton;
            pushButton.ToolTip = tooltip;
        }
    }
}
