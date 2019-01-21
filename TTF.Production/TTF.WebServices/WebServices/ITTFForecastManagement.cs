///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>ITTFVersionTree.cs</file>
///<description>
///ITTFVersionTree is the interfaces for the services of managing versions
///</description>
///

///
///<created>
///<author>Dr. Liu Qizhang</author>
///<date>21-11-2013</date>
///</created>
///


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using TTF.Models;

namespace TTF.WebServices
{
    [ServiceContract]
    public interface ITTFForecastManagement
    {
        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.WrappedRequest,
            UriTemplate = "InitiateForecast")]
        void InitiateForecast();

        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.WrappedRequest,
            UriTemplate = "SimulateATSS")]
        void SimulateATSS();

        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.WrappedRequest,
            UriTemplate = "GenAccuracyReport")]
        void GenAccuracyReport();

        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.WrappedRequest,
            UriTemplate = "ProcessATSSMessage")]
        void ProcessATSSMessage(string message);

        
    }


}
