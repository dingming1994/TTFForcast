using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using TTF.SetupDataJob.Utils;
using System.Data;
using System.Net;
using System.IO;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using System.Globalization;

namespace TTF.TDSForecast
{
    class Program
    {
        public static DateTime runDate = DateTime.Now;
        static void Main(string[] args)
        {

            //This job will retrieve the network data, lookup data and dailytrainstationtime from ATOMS for a specified day (defaulted to today)
            //runDate = new DateTime(2014, 12, 19);
            if (args.Length > 0)
            {
                runDate = DateTime.Parse(args[0]);
            }

            DateTime prevTime = DateTime.Parse(ConfigurationManager.AppSettings["StartTime"]);
            int interval = Convert.ToInt32(ConfigurationManager.AppSettings["TDSInterval"]);
            int method = Convert.ToInt32(ConfigurationManager.AppSettings["ForecastMethod"]);

            while (true)
            {
              //  DateTime currTime = prevTime.AddSeconds(interval);  //Will change it to DateTime.Now in production
                DateTime currTime = DateTime.Now;
                List<TDSRecord> records = GetNewTDSRecords(prevTime, currTime);

                foreach (TDSRecord record in records)
                {
                     try
                     {
                        DataSet forecastResult = ProcessTDSSignal(record, method);

                        if (forecastResult.Tables.Count > 0)
                        {
                            DataTable table = forecastResult.Tables[0];


                            if (table.Columns.Count == 2) //error
                            {
                                string error = (string)table.Rows[0]["Message"];
                                Logging.log.Error("Error in processing TDS signal at " + record.time.ToString() + " with error: " + error);
                            }
                            else
                            {
                                List<ForecastedTrainArrivalTime> list = new List<ForecastedTrainArrivalTime>();
                                foreach (DataRow row in table.Rows)
                                {
                                    ForecastedTrainArrivalTime forecast = new ForecastedTrainArrivalTime();
                                    forecast.Bound = (string) row["Bound"];
                                    forecast.EmuNo = (string) row["EMUNo"];
                                    forecast.Time = DateTime.Parse((string)row["Time"]);
                                    forecast.PlannedST = Convert.ToInt32((string)row["PlannedStationTime"]);
                                    forecast.Platform = (string)row["Platform"];
                                    forecast.TrainNo = (string)row["TrainNo"];
                                    list.Add(forecast);
                                }
                                SendMessages(list);
                            }
                        }
                        else
                        {
                            throw new ApplicationException("No results returned from DB");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.log.Error("Error in processing TDS signal at " + record.time.ToString() + " with error: " + ex.Message);
                    }
                }

                prevTime = currTime;
                Thread.Sleep(interval * 1000);
            }
            //SendInitialForecastsToSQS();
            //SendInitialTravelTimesToSQS();
            //SendInitialLinkwayWalkTimesToSQS();

        }

        static private void PrepareCommandParameters(SqlCommand cmd, Dictionary<string, object> parameters)
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

        static private DataSet ProcessTDSSignal(TDSRecord record, int method)
        {
            #region format the parameters with the variables from client
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["@plat"] = record.platform;
            parameters["@station"] = record.station;
            parameters["@time"] = record.time;
            parameters["@type"] = record.type;
            parameters["@emnuNo"] = record.emuNo;
            parameters["@delay"] = record.delay;
            parameters["@delayTime"] = record.delayTime;
            parameters["@method"] = method;
            #endregion

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandTimeout = 900000;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ProcessTDSSignal";
                PrepareCommandParameters(cmd, parameters);

                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = cmd;
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                return ds;
            }
        }

        static List<TDSRecord> GetNewTDSRecords(DateTime prevTime, DateTime currTime)
        {

            List<TDSRecord> records = new List<TDSRecord>();
            try
            {
                Logging.log.Debug("Getting data from " + prevTime.ToString() + " to " + currTime.ToString());
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TDSDB"].ConnectionString))
                {

                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                            select int_delay, date, station, platform, Direction,PVNo, Delay 
                                from vW_CBTCDataForReformApp where date >= @fromDate 
                                and date < @toDate order by date", conn))
                    {

                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("fromDate", prevTime);
                        cmd.Parameters.AddWithValue("toDate", currTime);

                        SqlDataReader r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            TDSRecord record = new TDSRecord();
                            record.delay = r.GetInt32(0);
                            record.time = r.GetDateTime(1);
                            record.station = r.GetString(2);
                            record.platform = r.GetString(3);
                            if (record.platform == "JURDE")
                                record.platform = "JURD";
                            record.type = r.GetString(4).ToUpper();
                            record.emuNo = r.GetString(5);
                            record.delayTime = r.GetString(6);

                            records.Add(record);
                            Logging.log.Debug("New record: " + record.delay + " " + record.time.ToString() + " " + record.station + " "
                                + record.platform + " " + record.type + " " + record.emuNo + " " + record.delayTime);
                        }
                        r.Close();
                    }

                }
                

            }
            catch (Exception ex)
            {
                 Logging.log.Error("Error retrieving data from TDS", ex);
            }

            Logging.log.Debug("Number of records retrieved: " + records.Count);
            return records;
        }

        private static void SendMessages(List<ForecastedTrainArrivalTime> messages)
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

        private struct ForecastedTrainArrivalTime
        {
            public String TrainNo;
            public String EmuNo;
            public DateTime Time;
            public long PlannedST;
            public String Bound;
            public String Platform;
        }

        private struct AvgStnToStnTravelTime
        {
            public String FromPlat;
            public String ToPlat;
            public String FromTime;
            public String ToTime;
            public int TravelTime;
        }
        private struct LinkwayWalkTime
        {
            public String Station;
            public String FromLine;
            public String ToLine;
            public String FromTime;
            public String ToTime;
            public int WalkTime;
        }

        class TDSRecord
        {
            public DateTime time;
            public string station;
            public string platform;
            public string type;
            public string emuNo;
            public int delay;
            public string delayTime;
        }
    }
}
