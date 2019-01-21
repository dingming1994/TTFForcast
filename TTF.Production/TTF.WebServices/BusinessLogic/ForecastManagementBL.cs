///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>ForecastManagementBL.cs</file>
///<description>
///ForecastManagementBL is the class that provides interfaces to manage train travel time forecast.
///</description>
///

///
///<created>
///<author>Dr. Liu Qizhang</author>
///<date>04-10-2014</date>
///</created>
///

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel.Web;
using System.Web;
using TTF.DataLayer;
using TTF.Models;
using TTF.Utils;
using System.Data.SqlClient;
using System.Configuration;
using TTF.Models.OperationManagement;
using Amazon.SQS;
using System.Net;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using TTF.Models.SQS;

namespace TTF.BusinessLogic
{
    public class ForecastManagementBL
    {
        private static ForecastManagementBL _instance;
        private static DateTime OperationDate = DateTime.Now;
    //    private DateTime currTime = DateTime.Now;
        private DateTime currTime = DateTime.Now;
        public static int dateType = 0;

        public ForecastManagementBL()
        {
        }

        public static ForecastManagementBL Instance()
        {
            if (_instance == null)
            {
                _instance = new ForecastManagementBL();
            }
            return _instance;
        }

       

        #region Historical station to station travel time

        /// <summary>
        /// Get the list of travel time between give stations from the database
        /// </summary>
        /// <param name="stn1"></param>
        /// <param name="stn2"></param>
        /// <returns></returns>
        public List<StnToStnTravelTime> GetHistoricalTravelTime(string fromPlat, string toPlat)
        {
            return (new ForecastManagementDL()).GetHistoricalTravelTime(fromPlat, toPlat);
        }

        /// <summary>
        /// Add StnToStnTravelTime record into database
        /// </summary>
        /// <param name="stn1"></param>
        /// <param name="stn2"></param>
        /// <param name="arrivaltime"></param>
        /// <param name="traveltime"></param>
        /// <returns></returns>
        public int AddStnToStnTravelTime(string fromPlat, string toPlat, DateTime arrivalTime, int travelTime)
        {
            return (new ForecastManagementDL()).AddStnToStnTravelTime(fromPlat, toPlat, arrivalTime, travelTime);
        }
        #endregion

        #region Manage forecasted arrival time at stations
        /// <summary>
        /// Add the forecast arrival time at a station along a bound
        /// </summary>
        /// <param name="emuNo"></param>
        /// <param name="trainNo"></param>
        /// <param name="trainNoAlias"></param>
        /// <param name="stn"></param>
        /// <param name="bound"></param>
        /// <param name="time"></param>
        public void AddForecastedArrivalTime(string emuNo, string trainNo, string trainNoAlias, short stn, short bound, DateTime time, int? plannedStationTime, string platform)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    if (emuNo == null)
                    {
                        (new ForecastManagementDL()).AddForecastedArrivalTime(new SqlCommand("", conn, t), emuNo, trainNo, trainNoAlias, stn, bound, time, plannedStationTime, true, platform);
                    }
                    else
                    {
                        (new ForecastManagementDL()).DeleteForecastedTimeByEMUandStn(new SqlCommand("", conn, t), emuNo, stn, bound);
                        if(plannedStationTime != null)
                            (new ForecastManagementDL()).DeleteForecastedTimeByPlannedST(new SqlCommand("", conn, t), plannedStationTime.Value);
           //             (new ForecastManagementDL()).DeleteForecastedTimeByTime(new SqlCommand("", conn, t),time, stn,bound);
                        (new ForecastManagementDL()).AddForecastedArrivalTime(new SqlCommand("", conn, t), emuNo, trainNo, trainNoAlias, stn, bound, time, plannedStationTime, true, platform);
                    }
                    t.Commit();
                }
            }
        }
        #endregion

        #region Forecast procedure
        /// <summary>
        /// Get the beginning of the day, initiate the forecast by clearing old data and use planned TT for the initial forecast
        /// </summary>
        public void InitiateForecast()
        {
            OperationDate = DateTime.Now;
         //   OperationDate = DateTime.Parse("2017-11-03");
            ArchiveData();
             
            ForecastManagementBL.dateType = (new UtilityDL()).GetTimeTableDateTypeID(OperationDate.ToString("yyyy-MM-dd"));
            (new ForecastManagementDL()).InitiateForecast();
            (new ForecastManagementDL()).InitiateTravelTimeWithTT();
            (new ForecastManagementDL()).GenerateTrainNoToBound();
            (new ForecastManagementDL()).ClearTrainNoandEMUMapping();
            (new ForecastManagementDL()).ClearTerminalMovement();

            (new ForecastManagementDL()).CalculateAvg15MinsTravelTime(OperationDate);
          //  (new ForecastManagementDL()).CalculateAvgTravelTime(ForecastManagementBL.dateType);
        }

        public void ArchiveData()
        {
            Logging.log.Debug("Start archiving");
            DateTime oldDate = DateTime.Now.Date.AddDays(-1);
            if (OperationDate != null)              
            {
                oldDate = OperationDate.AddDays(-1);
            }
             
            try
            {
                (new OperationManagementDL()).ArchiveOperationData(oldDate);
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error encountered while archiving operation data " + ex.Message);
                return;
            }
            Logging.log.Debug("Archiving is done");
        }

        public void SimulateATSS()
        {
            ForecastManagementBL.dateType = (new UtilityDL()).GetTimeTableDateTypeID(DateTime.Now.ToString("yyyy-MM-dd"));

            List<string> list = new List<string>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string cmdText = @"select ATSSSignal from ATSSInputLog where timestamp > '2015-09-15' order by timestamp";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {

                    SqlDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {
                        list.Add(r.GetString(0));
                    }

                    r.Close();
                }
            }


            foreach (string line in list)
            {

                ProcessATSSInput(line);
            }

        }

//        public void SimulateATSS()
//        {
//            System.IO.StreamReader reader = new StreamReader("C:\\TTF\\050815_Devaition Log.txt");

//            string line = reader.ReadLine();

//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
//            {
//                conn.Open();

//                while ((line = reader.ReadLine()) != null)
//                {
//                    string[] elements = line.Split(new char[] {'\t' });

//                    if (elements[7] == "")
//                    {
//                        continue;
//                    }

//                    string cmdText = @"Insert into RealATSSTime (Platform, time, scheduleno, type) values
//                                        (@platform,@time,@scheduleNo,@type)";
//                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
//                    {
//                        cmd.Parameters.AddWithValue("platform", elements[2]);
//                        DateTime time = DateTime.ParseExact(elements[0], "HH:mm:ss dd/MM/yy",System.Globalization.CultureInfo.InvariantCulture);
//                        cmd.Parameters.AddWithValue("time", time);
//                        cmd.Parameters.AddWithValue("scheduleNo", elements[7]);
//                        if(elements[6] == "Departure")
//                            cmd.Parameters.AddWithValue("type", "DEP");
//                        else
//                            cmd.Parameters.AddWithValue("type", "ARR");

//                        cmd.ExecuteNonQuery();
//                    }
                   
//                }

//                reader.Close();

//            }

            

//        }

        private void PrepareCommandParameters(SqlCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters != null && parameters.Count > 0)
            {
                foreach (string keyObj in parameters.Keys)
                {
                    SqlParameter param = cmd.CreateParameter();
                    param.Direction = ParameterDirection.Input;
                    param.ParameterName = keyObj;
                    param.Value = parameters[keyObj];
                    cmd.Parameters.Add(param);
                }
            }
        }

        public string ProcessATSSInput(string line)
        {
            #region format the parameters with the variables from client
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["@signal"] = line;
            parameters["@method"] = 2;
            //parameters["@time"] = record.time;
            //parameters["@type"] = record.type;
            //parameters["@emnuNo"] = record.emuNo;
            //parameters["@delay"] = record.delay;
            //parameters["@delayTime"] = record.delayTime;
            //parameters["@method"] = method;
            #endregion

            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandTimeout = 900000;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "ProcessATSSSignal";
                    PrepareCommandParameters(cmd, parameters);

                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = cmd;
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);

                    if (ds.Tables.Count > 0)
                    {
                        DataTable table = ds.Tables[0];


                        if (table.Columns.Count == 2) //error
                        {
                            string error = (string)table.Rows[0]["Message"];
                            Logging.log.Error("Error in processing TDS signal " + line + " with error: " + error);
                        }
                        else
                        {
                            Logging.log.Debug("The number of new forecast records is " + table.Rows.Count);
                            List<ForecastedTrainArrivalTime> list = new List<ForecastedTrainArrivalTime>();
                            foreach (DataRow row in table.Rows)
                            {
                                ForecastedTrainArrivalTime forecast = new ForecastedTrainArrivalTime();
                                forecast.Bound = (string)row["Bound"];
                                forecast.EmuNo = (string)row["EMUNo"];
                                forecast.Time = DateTime.Parse((string)row["Time"]);
                                forecast.PlannedST = Convert.ToInt32((string)row["PlannedStationTime"]);
                                forecast.Platform = (string)row["Platform"];
                                forecast.TrainNo = (string)row["TrainNo"];
                                list.Add(forecast);
                            }
                            SendMessagesNew(list);
                        }
                    }
                    else
                    {
                        throw new ApplicationException("No results returned from DB");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error in processing " + line + " with error: " + ex.Message, ex);
            }

            ////,System.IO.StreamWriter writer
            //try
            //{
            //    string[] elements = line.Split(new char[] { ',' });

            //    string type = elements[0];

            //    DateTime time = currTime;   //It should be time = DateTime.Now

            //    DateTime threshold1 = DateTime.ParseExact("2015-09-15 03:00:00", "yyyy-MM-dd HH:mm:ss",
            //                                                           new CultureInfo("en-US"),
            //                                                           DateTimeStyles.None);

            //    DateTime threshold2 = DateTime.ParseExact("2015-09-16 02:00:00", "yyyy-MM-dd HH:mm:ss",
            //                                                               new CultureInfo("en-US"),
            //                                                               DateTimeStyles.None);


            //    try
            //    {
            //        time = DateTime.ParseExact(elements[1], "yyyy-MM-dd HH:mm:ss",
            //                                                           new CultureInfo("en-US"),
            //                                                           DateTimeStyles.None);

            //        // time = Convert.ToDateTime(elements[1]);
            //    }
            //    catch (Exception e)
            //    {
            //    }

            //    if (time < threshold1 || time > threshold2)
            //        return null;

            //    currTime = time;
            //    //Added temporarily
            //    DataManager.Instance().NetworkVersion = (new VersionTreeDL()).GetEffectiveVersion("NetworkSettings", currTime.ToString("yyyy-MM-dd"));  //Need to be iniaited

            //    //To be removed
            //    //if (time < Convert.ToDateTime("2014-03-28 02:00:00"))
            //    //{
            //    //    return;
            //    //}

            //    if (type == "A")
            //    {
            //         ProcessASignal(line);
            //        //, writer
            //        ;
            //    }

            //    if (type == "B")
            //    {
            //        ProcessBSignal(line);
            //        return "";
            //    }

            //    if (type == "C")
            //    {
            //        ProcessCSignal(line);
            //        return "";
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logging.log.Error("Error in processing " + line + " with error: " + ex.Message, ex);
            //}
            return "";
        }

        public void ProcessASignal(string line)
        {
            Logging.log.Error("Receving A signal: " + line);
            //, System.IO.StreamWriter writer
            string[] elements = line.Split(new char[] { ',' });
            string type = elements[6];

            DateTime time = DateTime.Now;
            try
            {
                time = DateTime.ParseExact(elements[1], "yyyy-MM-dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None);
            }
            catch (Exception ex)
            {
                Logging.log.Error("Wrong datetime formate: " + line);
                return;
            }

            string trainno = elements[2].Trim();
            string platform = elements[4].Trim();


            int deviation = 0;

            if (elements[3].Trim() != "")
            {
                int sign = -1;
                if (elements[3].Substring(0, 1) == "-")
                    sign = 1;
                string strHr = elements[3].Substring(1, 2);
                if (strHr.Substring(0, 1) == "0")
                    strHr = strHr.Substring(1, 1);
                string strMin = elements[3].Substring(3, 2);
                if (strMin.Substring(0, 1) == "0")
                    strMin = strMin.Substring(1, 1);

                string strSec = elements[3].Substring(5, 2);
                if (strSec.Substring(0, 1) == "0")
                    strSec = strSec.Substring(1, 1);

                deviation = sign * (Convert.ToInt32(strHr) * 3600 + Convert.ToInt32(strMin) * 60 + Convert.ToInt32(strSec));
            }

            DateTime actualTime = time.AddSeconds(deviation);

            if (DataManager.Instance().MapPlatform == null || DataManager.Instance().MapPlatform.Count == 0)
            {
                DataManager.Instance().NetworkVersion = (new VersionTreeDL()).GetEffectiveVersion("NetworkSettings", DateTime.Now.ToString("yyyy-MM-dd"));
                LoadNetworkData();
            }


            short stnID = DataManager.Instance().MapPlatform[platform].StationID;
            short lineID = DataManager.Instance().MapStation[stnID].lnId;
            string lineCode = DataManager.Instance().MapStation[stnID].LnName;
            short platformID = DataManager.Instance().MapPlatform[platform].ID;

            OperationManagementDL dl = new OperationManagementDL();

            #region Handling train number
            if (trainno.Trim().Length < 3)
            {
                Logging.log.Error("Error in train number format for ATSS message : " + trainno);
                return;
            }

            int firstTrainNoDigit = 0;

            try
            {
                firstTrainNoDigit = Convert.ToInt16(trainno.Substring(0, 1));
            }
            catch (Exception ex)
            {
                Logging.log.Error("Wrong train number format: " + line);
                return;
            }


            string emuNo = dl.GetEMUNoByTrainNo(trainno, platform, time);

            TrainMovement lastStop = (new ForecastManagementDL()).GetLastTrainMovement(emuNo,platform);  //get the last stop of this train

            string trainNoAlias = "";

            if (firstTrainNoDigit != 1 && firstTrainNoDigit != 2 && firstTrainNoDigit != 7)
            {
                if (lineCode == "NSL")
                    trainNoAlias = "2" + trainno.Substring(1, 2);
                else
                    trainNoAlias = "1" + trainno.Substring(1, 2);
            }
            else
            {
                trainNoAlias = trainno;
            }

            #endregion

            #region Based on the current actual arrival time, get the corresponding planned station time (it could be null)
            Station currStn = (new NetworkManagementDL()).GetStnByPlatform(platform, DataManager.Instance().NetworkVersion.ID);

            if (currStn == null)
            {
                Logging.log.Error("No station defined for the given platform: " + line);
                return;
            }

            if (DataManager.Instance().ListTerminalPlatforms.Contains(platform)) //For terminal stops, need to check departure time carefully
            {
                if (!(new OperationManagementDL()).IsValidMovement(emuNo, platform, time, type))
                    return;
            }

            DailyTrainStationTime plannedST = (new OperationManagementDL()).GetPlannedStationTimeByATSSData(trainNoAlias, platform, actualTime, type, currStn);
            long? plannedSTID = null;
            if (plannedST != null)
                plannedSTID = plannedST.ID;

            DateTime newtime = time;
            if (plannedST != null)
            {
                newtime = plannedST.Time.AddSeconds(-1 * deviation);
            }


            //if ((new OperationManagementDL()).CheckIfDuplicateATSSData(trainno, platform, time, type, deviation))
            //{
            //    return;
            //}


            //(new OperationManagementDL()).AddATSSData(trainno, newtime, plannedSTID, type, emuNo, platform, trainNoAlias, OperationDate);

            if (type == "DEP")  //we only handle arrival signals
                return;

            if (firstTrainNoDigit >= 7)
            {
                (new ForecastManagementDL()).DeleteForecastedTimeByEMU(emuNo);
                (new ForecastManagementDL()).DeleteTrainMovement(emuNo);
                return;
            }



            bool isValid = (new OperationManagementDL()).IsValidMovement(trainNoAlias, platform, time, type);
            if (!isValid)
                return;


            #endregion

            #region If the current stop is not an adjacent stop tp the previous stop, we assume the train starts a new service

            int travelTime = 0;

            if (lastStop != null)
            {
                TimeSpan span = newtime - lastStop.LastSignalTime;
                travelTime = (int)span.TotalSeconds;
            }


            if (travelTime > 3600) //split duty
            {
                lastStop = null;
            }


            short? bound = null;
            if (lastStop != null)
            {
                if (lastStop.Bound == null)
                    bound = (new NetworkManagementDL()).GetBoundByStnPair(lastStop.LastStn, currStn.ID);
                else
                    bound = lastStop.Bound.Value;

                if (bound == null)
                {
                    lastStop = null;
                    (new ForecastManagementDL()).DeleteForecastedTimeByEMU(emuNo);
                }
                else
                {
                    try
                    {
                        if (travelTime > 40)
                        {
                            (new ForecastManagementDL()).AddStnToStnTravelTimeToday(lastStop.Platform, platform, travelTime, lastStop.LastSignalTime);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.log.Error("Travel time between stations is too large: " + line);
                        return;
                    }
                }
            }

            
            #endregion


            #region Case 1. There is no past record about this train
            if (lastStop == null)
            {
                #region Case 1.1. The train is non-service train. Don't do anything
                if (firstTrainNoDigit >= 8)
                {
                    return;
                }
                #endregion


                #region Case 1.2. The train is a ghost train
                if (plannedST == null)
                {
                    //In the future, may add platform-bound relationship to determine the bound based on platform
                    //and then do the forecast
                    if ((firstTrainNoDigit == 1 || firstTrainNoDigit == 2) && time.Hour < 5)
                    {
                        //This is the case that the train will arrive at main line early
                    }
                    else
                    {
                        (new ForecastManagementDL()).AddTrainMovement(emuNo, trainno, currStn.ID, null, newtime, trainNoAlias, platform);
                    }
                    return;
                }
                #endregion

                #region Case 1.3. The train starts a trip as scheduled
                else
                {
                    (new ForecastManagementDL()).AddTrainMovement(emuNo, trainno, currStn.ID, null, newtime, trainNoAlias, platform);
                    //Don't do forecast for the first stop of the train, because very often at early morning, the train will arrive
                    //at the station early and stay there for long time before departure.
                }
                #endregion
            }
            #endregion

            #region Case 2. There is a past record about this train
            else
            {
                #region Case 2.1. The train runs on schedule
                if (plannedST != null)
                {
                    (new ForecastManagementDL()).AddTrainMovement(emuNo, trainno, currStn.ID, plannedST.BoundId, newtime, trainNoAlias, platform);

                    if (lastStop != null && platform.Substring(0, 3) == lastStop.Platform.Substring(0, 3)) //This is to ignore the second arrival signal received at the same station
                        return;
                    (new ForecastManagementDL()).UpdateForecastByPlannedST(plannedST.ID, emuNo, newtime);
                    return;
                }
                #endregion

                #region Case 2.2. The train is withdrawn
                if (firstTrainNoDigit >= 8)
                {
                    (new ForecastManagementDL()).DeleteForecastedTimeByEMU(emuNo);

                    return;
                }
                #endregion

                #region Case 2.3. The train runs temporary train number code.
                (new ForecastManagementDL()).AddTrainMovement(emuNo, trainno, currStn.ID, bound.Value, newtime, trainNoAlias, platform);
                if (lastStop != null && platform.Substring(0, 3) == lastStop.Platform.Substring(0, 3)) //This is to ignore the second arrival signal received at the same station
                    return;
                (new ForecastManagementDL()).UpdateForecastByEMU(emuNo, trainno, lastStop.LastSignalTime, newtime, currStn.ID, bound.Value);
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// Signal B is to assign an EMU number to a train number
        /// </summary>
        /// <param name="line"></param>
        private void ProcessBSignal(string line)
        {
            string[] elements = line.Split(new char[] { ',' });

            //    DateTime time = Convert.ToDateTime(elements[1]);
            string trainno = elements[2].Trim();
            string emuNo = elements[3].Trim() + elements[4].Trim();
            int no1 = Convert.ToInt32(elements[3]);
            int no2 = Convert.ToInt32(elements[4]);
            if (no1 > no2)
                emuNo = elements[4] + elements[3];

            (new OperationManagementDL()).AddEMUNotoTrainNo(trainno, emuNo);
        }

        /// <summary>
        /// Signal C is to 
        /// </summary>
        /// <param name="line"></param>
        private void ProcessCSignal(string line)
        {
            string[] elements = line.Split(new char[] { ',' });

            for (int len = 0; len < elements.Length; len++)
                elements[len] = elements[len].Trim();

            //  DateTime time = Convert.ToDateTime(elements[1]);
            string trainno1 = elements[2];
            string trainno2 = elements[3];
            (new OperationManagementDL()).UpdateEMUNoandTrainNoMapping(trainno1, trainno2);
        }
        #endregion

        public void GenerateAccuracyReport()
        {
            System.IO.StreamWriter writer = new System.IO.StreamWriter("C:\\TTF\\accuracy-Report.csv");
            writer.WriteLine ("Time slot,	Forecast < 1min,	Actual <1 min,	Forecast < 2min,	Actual <2 min,	Forecast < 3min,	Actual <3 min,	Forecast Avg Error,	Actual avg Error");
            (new ForecastManagementDL()).QueryAccuracy(writer);
            writer.Close();
        }

        public void LoadNetworkData()
        {
            Logging.log.Debug("Loading the network data");
            List<Platform> list = (new NetworkManagementDL()).GetPlatformList(DataManager.Instance().NetworkVersion.ID);
            DataManager.Instance().MapPlatform = new Dictionary<string, Platform>();
            foreach (Platform plat in list)
            {
                if (!DataManager.Instance().MapPlatform.ContainsKey(plat.Code))
                    DataManager.Instance().MapPlatform.Add(plat.Code, plat);
            }

            List<Station> listStn = (new NetworkManagementDL()).GetStationList("" + DataManager.Instance().NetworkVersion.ID);
            DataManager.Instance().MapStation = new Dictionary<short, Station>();
            foreach (Station stn in listStn)
                DataManager.Instance().MapStation.Add((short)stn.ID, stn);

            DataManager.Instance().ListTerminalPlatforms = (new NetworkManagementDL()).GetTerminalPatforms(DataManager.Instance().NetworkVersion.ID);
        }

        private static void SendMessagesNew(List<ForecastedTrainArrivalTime> messages)
        {
            var config = new AmazonSQSConfig();
            config.ServiceURL = "http://sqs.ap-southeast-1.amazonaws.com";
            config.ProxyHost = "hqpr1.smrt.com.sg";
            config.ProxyPort = 8080;
            config.ProxyCredentials = CredentialCache.DefaultCredentials;
            var sqs = new AmazonSQSClient(config);

            // preparing send request (with queue URL and message body)
            var req = new SendMessageRequest();
            req.QueueUrl = "https://sqs.ap-southeast-1.amazonaws.com/971616992866/connect-forecasting-dev";
            req.MessageBody = GetMessageBody(messages);

            Logging.log.Debug("Sending message: \n" + req.MessageBody);


            // send request and show result
            try
            {
                var response = sqs.SendMessage(req);
                Logging.log.Debug("Message sent.");
                Logging.log.Debug("MessageId: " + response.MessageId);
                Logging.log.Debug("MD5: " + response.MD5OfMessageBody);
            }
            catch (InvalidMessageContentsException)
            {
                Logging.log.Debug("Error: InvalidMessageContentsException - The message contains characters outside the allowed set.");
            }
            catch (UnsupportedOperationException)
            {
                Logging.log.Debug("Error: UnsupportedOperationException - Error code 400. Unsupported operation.");
            }
            catch (Exception ex)
            {
                Logging.log.Debug(" Error sending data to SQS " + ex.Message, ex);
            }
        }


        private static string GetMessageBody(List<ForecastedTrainArrivalTime> items)
        {
            // Each message must be a JSON array containing JSON objects of the following format:
            // {
            //   "type": "TheNameOfTheClass",
            //   "data": {
            //     ...
            //   }
            // }

            // IMPORTANT: MessageBody can be up to 256KB (262,144 bytes) in size.
            // Avoid sending too many elements in the so that message size can be kept below 256 KB.
            // Ref: http://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessage.html



            List<object> list = new List<object>();
            foreach (ForecastedTrainArrivalTime f in items)
            {
                list.Add(new Dictionary<string, object> {
                { "type", "ForecastedTrainArrivalTime" },
                { "data", f }
            });
            }

            return JsonConvert.SerializeObject(list);
        }

        #region Circuit Data
        public CircuitData GetCircuitData(string trackCircuit)
        {
            return (new ForecastManagementDL()).GetCircuitData(trackCircuit);
        }

        #endregion
    }

    //public struct ForecastedTrainArrivalTime
    //{
    //    public String TrainNo;
    //    public String EmuNo;
    //    public DateTime Time;
    //    public long PlannedST;
    //    public String Bound;
    //    public String Platform;
    //}
}