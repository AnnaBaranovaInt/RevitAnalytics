using SAP2000v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAnalytics.SAPConnector
{
    public static class SapModelManager
    {
        public static cSapModel GetSapModel()
        {
            cOAPI sapObject = null;
            try
            {
                sapObject = (cOAPI)System.Runtime.InteropServices.Marshal.GetActiveObject("CSI.SAP2000.API.SapObject");
            }
            catch
            {
                cHelper myHelper = new Helper();
                sapObject = myHelper.CreateObjectProgID("CSI.SAP2000.API.SapObject");
                sapObject.ApplicationStart();
            }

            return sapObject?.SapModel;
        }
    }

}
