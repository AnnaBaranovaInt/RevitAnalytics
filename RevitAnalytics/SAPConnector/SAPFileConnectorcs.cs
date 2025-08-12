using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.SAPConnector
{
    class SAPFileConnectorcs
    {
        public static cOAPI AttachOrCreateSapObject()
        {
            cOAPI mySapObject = null;
            try
            {
                DebugHandler.Log("Trying to attach to a running instance of SAP2000.", DebugHandler.LogLevel.INFO);
                mySapObject = (cOAPI)System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.SAP2000.API.SapObject");
            }
            catch
            {
                DebugHandler.Log("No running instance found. Creating a new instance of SAP2000.", DebugHandler.LogLevel.INFO);
                cHelper myHelper = new Helper();
                mySapObject = myHelper.CreateObjectProgID("CSI.SAP2000.API.SapObject");
                mySapObject.ApplicationStart();
            }
            return mySapObject;
        }

        public static void InitializeSapModel(cSapModel sapModel)
        {
            int ret = 0;
            DebugHandler.Log("Initializing new SAP2000 model.", DebugHandler.LogLevel.INFO);
            ret = sapModel.InitializeNewModel(eUnits.kN_m_C);
            ret = sapModel.File.NewBlank();
            DebugHandler.Log("SAP2000 model initialized successfully.", DebugHandler.LogLevel.INFO);
        }

        public static cSapModel GetSapModel()
        {
            cOAPI sapObject = null;
            cSapModel sapModel = null;

            try
            {
                DebugHandler.Log("Trying to attach to an active SAP2000 instance.", DebugHandler.LogLevel.INFO);
                // Try to attach to an active SAP2000 instance
                sapObject = (cOAPI)System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.SAP2000.API.SapObject");
            }
            catch
            {
                DebugHandler.Log("No running instance found. Creating a new instance of SAP2000.", DebugHandler.LogLevel.INFO);
                // None running, so create a new instance using the ProgID
                cHelper myHelper = new Helper();
                sapObject = myHelper.CreateObjectProgID("CSI.SAP2000.API.SapObject");
                sapObject.ApplicationStart();
            }

            if (sapObject != null)
            {
                sapModel = sapObject.SapModel;
                DebugHandler.Log("SAP2000 instance attached/created successfully.", DebugHandler.LogLevel.INFO);
            }
            else
            {
                DebugHandler.LogError("Failed to attach/create SAP2000 instance.", new Exception("SAP2000 instance is null."));
            }

            return sapModel;
        }
    }
}
