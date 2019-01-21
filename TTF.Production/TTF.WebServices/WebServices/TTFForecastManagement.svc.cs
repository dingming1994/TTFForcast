///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>TTFVersionTree.svc.cs</file>
///<description>
///TTFVersionTree is the file to implement the services of managing versions
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
using TTF.BusinessLogic;
using TTF.Models;
using System.ServiceModel.Activation;
using TTF.Utils;
using System.ServiceModel.Web;
using TTF.Models.OperationManagement;
using System.Web;

namespace TTF.WebServices
{
   [AspNetCompatibilityRequirements(
        RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class TTFForecastManagement : ITTFForecastManagement
    {
        
        public void InitiateForecast()
        {
            try
            {
                Logging.log.Debug("Start initialising forecast");
                ForecastManagementBL bl = new ForecastManagementBL();

                bl.InitiateForecast();
            }
            catch (Exception ex)
            {
                Logging.log.Error(ex.Message, ex);
                WebOperationContext ctx = WebOperationContext.Current;
                ctx.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                ctx.OutgoingResponse.StatusDescription = ex.Message;
            }

        }

        public void SimulateATSS()
        {
            try
            {
                ForecastManagementBL bl = new ForecastManagementBL();

                bl.SimulateATSS();
            }
            catch (Exception ex)
            {
                Logging.log.Error(ex.Message, ex);
                WebOperationContext ctx = WebOperationContext.Current;
                ctx.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                ctx.OutgoingResponse.StatusDescription = ex.Message;
            }

        }

        public void GenAccuracyReport()
        {
            try
            {
                ForecastManagementBL bl = new ForecastManagementBL();

                bl.GenerateAccuracyReport();
            }
            catch (Exception ex)
            {
                Logging.log.Error(ex.Message, ex);
                WebOperationContext ctx = WebOperationContext.Current;
                ctx.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                ctx.OutgoingResponse.StatusDescription = ex.Message;
            }

        }

        private string GetReadableATSSMessages(string line, string timestamp)
        {
            string result = "";
            //Message format is
            // [SOM][Type][Message][BCC - Check byte][EOM]
            // [SOM] - 8D
            // [Type] - A for TrainPosition B for Train Consist
            // [BCC] - 1s compliment of XOR of all messgage bytes excluding SOM EOM and BCC

            // TrainPosition message format
            // [Number of trains][TrainNo][Timetable Deviation Time][Track Circuit ID]
            Logging.log.Debug("Inside get readable message : " + line);

            if (line.Length > 0)
            {
                if (line.Substring(0, 1) == "A")
                {
                    if (line.Length > 1)
                    {
                        if (line.Substring(1, 1) == "1")
                        {
                            if (line.Length > 18)
                            {

                                string trainNo = line.Substring(2, 4); // 4 chars
                                string deviation = line.Substring(6, 7).Trim(); // 7 chars
                                string trackCircuit = line.Substring(13, 5).Trim(); //5 chars

                                CircuitData cd = (new ForecastManagementBL()).GetCircuitData(trackCircuit);

                                if (cd.PositionCode.ToUpper() == "PLATFORM")
                                {
                                    result = "A," + timestamp + "," + trainNo + "," + deviation + "," + cd.PlatformCode + "," + cd.Bound + ",ARR" + "," + trackCircuit;
                                }
                                else if (cd.PositionCode.ToUpper() == "AFTERPLATFORM")
                                {
                                    result = "A," + timestamp + "," + trainNo + "," + deviation + "," + cd.PlatformCode + "," + cd.Bound + ",DEP" + "," + trackCircuit;
                                }
                                else
                                {
                                    //result = "A," + timestamp + "," + trainNo + "," + deviation + "," + cd.PlatformCode + "," + cd.Bound + ",DEP" + "," + trackCircuit;
                                }
                            }
                            else
                            {
                                Logging.log.Debug("Length less than = 18");
                                //Invalid - just log
                                //result = "A," + line;
                            }
                        }
                        else
                        {
                            //TODO More than 1 position in same message - for now just log it
                            // result ="A," + line;
                        }
                    }
                    else
                    {
                        result = line;
                    }
                }
                if (line.Substring(0, 1) == "B")
                {
                    if (line.Length > 1)
                    {
                        if (line.Substring(1, 1) == "1")
                        {
                            if (line.Length > 15)
                            {
                                string trainNo = line.Substring(2, 4); // 4 chars
                                string numberOfEmus = line.Substring(6, 1);
                                {
                                    if (numberOfEmus == "2")
                                    {
                                        string emu1 = line.Substring(7, 4).Trim();
                                        string emu2 = line.Substring(11, 4).Trim();
                                        result = "B," + timestamp + "," + trainNo + "," + emu1 + "," + emu2;
                                    }
                                    else
                                    {

                                        if (numberOfEmus == "0")
                                        {
                                            //Ignore
                                        }
                                        else
                                        {
                                            //TODO - process > 2 emus in the same message
                                            //Ignore for now
                                            //result =line;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //Ignore for now
                                //result =line;
                            }
                        }
                        else
                        {

                            if (line.Substring(1, 1) == "0")
                            {
                                //Ignore
                            }
                            else
                            {
                                //Log for now
                                //result ="B," + line;
                            }
                        }
                    }
                    else
                    {
                        //Ignore
                        //results.Add(line);
                    }
                }
                if (line.Substring(0, 1) == "C")
                {
                    if (line.Length > 1)
                    {
                        if (line.Length >= 14)
                        {

                            string oldTrainNo = line.Substring(1, 4);
                            string newTrainNo = line.Substring(5, 4);
                            string trackCircuit = line.Substring(9, 4).Trim();
                            result = "C," + timestamp + "," + oldTrainNo + "," + newTrainNo + "," + trackCircuit;
                        }
                        else
                        {
                            //Ignore
                            //results.Add(line);
                        }
                    }
                    else
                    {
                        //Ignore
                        // results.Add(line);
                    }
                }
                if (line.Substring(0, 1) == "D")
                {

                }
            }


            return result;
        }
        public void ProcessATSSMessage(string allmessages)
        {
            Logging.log.Debug("Start processing message");
            string[] messages = allmessages.Split('|');

            foreach (string m in messages)
            {
                string originalmessage = HttpUtility.UrlDecode(m);
                try
                {
                    Logging.log.Debug("Incoming ATSS message: " + originalmessage);

                    string message = GetReadableATSSMessages(originalmessage.Substring(19), originalmessage.Substring(0, 19));

                    Logging.log.Debug("Incoming Processed ATSS message: " + message);

                    if (message != "")
                    {
                        Logging.log.Debug("Sent ATSS message for processing:" + message);
                        ForecastManagementBL.Instance().ProcessATSSInput(message);


                    }

                }
                catch (Exception ex)
                {
                     Logging.log.Error("Error processing ATSS incoming message", ex);
                }
            }
        }

       
    }
}
