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

namespace RevitAnalytics
{
    [Transaction(TransactionMode.Manual)]
    class CmdToSap : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            RunSapTutorial();

            return Result.Succeeded;
        }
        static void RunSapTutorial()
        {
            #region SapTutorial

            bool AttachToInstance = false;
            bool SpecifyPath = false;

            // Path to SAP2000.exe (only if SpecifyPath = true)
            string ProgramPath = "C:\\Program Files\\Computers and Structures\\SAP2000 23\\SAP2000.exe";

            // Full path to the model
            string ModelDirectory = "C:\\CSiAPIexample";
            try
            {
                System.IO.Directory.CreateDirectory(ModelDirectory);
            }
            catch (Exception ex)
            {
                //log "Could not create directory: " + ModelDirectory)
                DebugHandler.LogError("Could not create directory: " + ModelDirectory, ex);
            }

            string ModelName = "API_1-001.sdb";
            string ModelPath = ModelDirectory + System.IO.Path.DirectorySeparatorChar + ModelName;

            // dimension cOAPI
            cOAPI mySapObject = null;
            int ret = 0;

            if (AttachToInstance)
            {
                // attach to running instance of SAP2000
                try
                {
                    mySapObject = (cOAPI)System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.SAP2000.API.SapObject");
                }
                catch (Exception ex)
                {
                    // log No running instance found or failed to attach
                    DebugHandler.LogError("No running instance found or failed to attach", ex);
                    return;
                }
            }
            else
            {
                // create API helper
                cHelper myHelper;
                try
                {
                    myHelper = new Helper();
                }
                catch (Exception ex)
                {
                    // log Cannot create an instance of the Helper object
                    DebugHandler.LogError("Cannot create an instance of the Helper object", ex);
                    return;
                }

                if (SpecifyPath)
                {
                    // create from specified path
                    try
                    {
                        mySapObject = myHelper.CreateObject(ProgramPath);
                    }
                    catch (Exception ex)
                    {
                        //log that cannot start from secified path
                        DebugHandler.LogError($"Cannot start from specified path - {ProgramPath}", ex);
                        return;
                    }
                }
                else
                {
                    // create from latest installed SAP2000
                    try
                    {
                        mySapObject = myHelper.CreateObjectProgID("CSI.SAP2000.API.SapObject");
                    }
                    catch (Exception ex)
                    {
                        //log that cant find sap2000
                        DebugHandler.LogError("Cannot start from latest installed SAP2000", ex);

                        return;
                    }
                }

                // start SAP2000
                ret = mySapObject.ApplicationStart();
            }

            // create SapModel
            cSapModel mySapModel = mySapObject.SapModel;

            // initialize model
            ret = mySapModel.InitializeNewModel(eUnits.kN_m_C);

            // create new blank model
            ret = mySapModel.File.NewBlank();

            // define material property
            ret = mySapModel.PropMaterial.SetMaterial("CONC", eMatType.Concrete, -1, "", "");

            // assign isotropic mechanical properties
            ret = mySapModel.PropMaterial.SetMPIsotropic("CONC", 3600, 0.2, 0.0000055, 0);

            // define rectangular frame section
            ret = mySapModel.PropFrame.SetRectangle("R1", "CONC", 12, 12, -1, "", "");

            // define section property modifiers
            double[] ModValue = new double[8];
            for (int i = 0; i <= 7; i++)
            {
                ModValue[i] = 1;
            }
            ModValue[0] = 1000;
            ModValue[1] = 0;
            ModValue[2] = 0;
            ret = mySapModel.PropFrame.SetModifiers("R1", ref ModValue);

            // switch to k-ft units
            ret = mySapModel.SetPresentUnits(eUnits.kN_cm_C);

            // example frame objects ...
            string[] FrameName = new string[3];
            string temp_string1 = FrameName[0];
            string temp_string2 = FrameName[0];

            // your frame-object creation calls go here

            // e.g., ret = mySapModel.FrameObj.AddByCoord(...)

            // additional sample objects
            SapMaterial s = new SapMaterial(mySapModel, "ff", MaterialType.CONCRETE);
            SapRectangularSection srs = new SapRectangularSection(mySapModel, s, "fouad", 12, 25, "", "", -1);
            // etc.

            // finalize example
            // ...
            // run analysis
            ret = mySapModel.File.Save(ModelPath);
            ret = mySapModel.Analyze.RunAnalysis();

            // get some results, etc.

            // close SAP2000
            mySapObject.ApplicationExit(false);
            mySapModel = null;
            mySapObject = null;


            #endregion
        }
    }
}
