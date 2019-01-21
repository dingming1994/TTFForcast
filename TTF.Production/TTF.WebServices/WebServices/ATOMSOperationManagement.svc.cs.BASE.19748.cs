using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using ATOMS.BusinessLogic;
using ATOMS.Models;
using System.ServiceModel.Activation;
using ATOMS.Utils;
using System.ServiceModel.Web;

namespace ATOMS.WebServices
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ATOMSManpowerPlanning" in code, svc and config file together.
    [AspNetCompatibilityRequirements(
        RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class ATOMSOperationManagement : IATOMSOperationManagement
    {
        public int GetUserID()
        {
            // return 1;
            int uid = 0;
            try
            {



            }
            catch (Exception ex)
            {
                Logging.log.Error("Error retrieving cookie", ex);
            }
            return uid;
        }
               
       
        /// <summary>
        /// Refresh the operation data for a give date
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public void RefreshOperationData(string date)
        {
            try
            {
                OperationManagementBL.Instance().RefreshOperationData(date);
            }
            catch (ArgumentException ex)
            {
                Logging.log.Error(ex.Message, ex);
                WebOperationContext ctx = WebOperationContext.Current;
                ctx.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Conflict;
                ctx.OutgoingResponse.StatusDescription = ex.Message;
            }
            catch (Exception ex)
            {
                Logging.log.Error(ex.Message, ex);
                WebOperationContext ctx = WebOperationContext.Current;
                ctx.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                ctx.OutgoingResponse.StatusDescription = ex.Message;
            }
        }

    }
}
