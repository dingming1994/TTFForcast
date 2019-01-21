using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;


namespace TTFSQL
{
    public partial class StoredProcedures
    {
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void Accuracy(string date)
        {
            DateTime opsDate = DateTime.Parse(date);

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                string sql = @"select int_delay, date, station, platform, Direction,PVNo, Delay 
                                from cbtc_train_arrival where date > @fromDate 
                                and date < @toDate order by date";
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue(@"fromDate", opsDate.AddHours(3));
                cmd.Parameters.AddWithValue(@"toDate", opsDate.AddDays(1));

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
                }
                r.Close();
            }

        //    Dictionary<string, int> map = new Dictionary<string, int>();
            foreach (TDSRecord record in records)
            {
                int hasMapping = ProcessTDSSignal(record.platform, record.station, record.time, record.type, record.emuNo, record.delay, record.delayTime, method);

                //if (hasMapping == 0)
                //{
                //    if (map.ContainsKey(record.emuNo))
                //    {
                //        map[record.emuNo] = map[record.emuNo]+1;
                //    }
                //    else
                //    {
                //        map.Add(record.emuNo, 1);
                //    }
                //}
            }
        }

        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void GenerateActualTime(string date)
        {
            DateTime opsDate = DateTime.Parse(date);
            List<TDSRecord> records = new List<TDSRecord>();

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                string sql = @"select int_delay, date, station, platform, Direction,PVNo, Delay 
                                from cbtc_train_arrival where date > @fromDate 
                                and date < @toDate order by date";
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue(@"fromDate", opsDate.AddHours(3));
                cmd.Parameters.AddWithValue(@"toDate", opsDate.AddDays(1));

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
                }
                r.Close();
            }

            //    Dictionary<string, int> map = new Dictionary<string, int>();
            foreach (TDSRecord record in records)
            {

                int hasMapping = RecordTDSSignal(record.platform, record.station, record.time, record.type, record.emuNo, record.delay, record.delayTime);

            }

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                using(SqlTransaction t = conn.BeginTransaction())
                {
                    try
                    {
                        string cmdText = "TRUNCATE TABLE TrainMovement";
                        SqlCommand cmd = new SqlCommand(cmdText, conn, t);
                        cmd.ExecuteNonQuery();
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                    }
                }
                
            }
        }

        [Microsoft.SqlServer.Server.SqlProcedure]
        public static int ProcessTDSSignal(string plat, string station, DateTime time, string type, string emnuNo, int delay, string delayTime, int method)
        {
            int hasPlannedTimeMapping = 1;
            SqlTransaction transaction = null;

            string Message = "";

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                try
                {
                    conn.Open();
                    string line = CommonUtils.GetLineByPlatform(conn, plat);
                    if (line == "EWL")
                    {
               //         conn.Close();
                        throw new Exception("The record is from EWL");
                    //    return 1;
                    }

                    bool isService = CommonUtils.IsPlatformServiceable(conn, plat);

                    if (!isService)
                    {
                 //       conn.Close();
                        throw new Exception("The record is a service record");
                        return 1;
                    }

                    int deviation = CommonUtils.GetDelayTime(delayTime, delay);

                    #region If type is dep, need to check whether there is already an arrival signal at the platform. If not, treat this departure signal as arrival
                    if (type == "DEP")
                    {
                        type = "ARR";
                        string sql = @"SELECT TOP 1 platform FROM TrainMovement 
                                    WHERE EMUNo = @emuNo order by [LastSignalTime] desc";
                        SqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = sql;
                        cmd.Parameters.AddWithValue(@"emuNo", emnuNo);

                        SqlDataReader r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            string lastPlt = r.GetString(0);
                            if (lastPlt == plat)
                            {
                                type = "DEP";
                            }
                        }
                        r.Close();
                    }
                    #endregion

                    if (type == "DEP")
                    {
               //         conn.Close();
                        throw new Exception("This is a departure record");
                    }

                    #region Process arrival signal
                    if (type == "ARR")
                    {
                        Int64 plannedSTID = 0;
                        string trainNo = null;
                        string bound = null;
                        DateTime plannedTime = DateTime.Now;
                        #region try to relate DTS signal to timetable
                        DateTime planTime = time.AddSeconds(-1 * deviation);
                        string sql = "";
                        SqlCommand cmd = conn.CreateCommand();
                        if (plat != "JURD" && delay != 0)
                        {
                            sql = @"SELECT dst.ID, dst.TrainNo, dst.bnd, dst.covered, dst.time FROM DailyTrainStationTime dst
                        WHERE dst.plt = @platform and dst.[time] > @time1 and dst.[time] < @time2";
                            DateTime time1 = planTime.AddSeconds(-30);
                            DateTime time2 = planTime.AddSeconds(60);

                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue(@"platform", plat);
                            cmd.Parameters.AddWithValue(@"time1", time1);
                            cmd.Parameters.AddWithValue(@"time2", time2);
                        }

                        if (plat == "JURD" || delay==0) //TDS cannot handle JURD platform properly, no deviation time was recorded
                        {
                            string train = CommonUtils.GetTrainNoByEMU(conn, emnuNo);
                            sql = @"SELECT dst.ID, dst.TrainNo, dst.bnd, dst.covered, dst.time FROM DailyTrainStationTime dst
                        WHERE dst.trainno = @trainNo and dst.plt = @platform and dst.[time] > @time1 and dst.[time] < @time2";
                            DateTime time1 = planTime.AddSeconds(-200);
                            DateTime time2 = planTime.AddSeconds(200);

                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue(@"platform", plat);
                            cmd.Parameters.AddWithValue(@"trainNo", train);
                            cmd.Parameters.AddWithValue(@"time1", time1);
                            cmd.Parameters.AddWithValue(@"time2", time2);
                        }
                        SqlDataReader r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            #region We may need to look into the case to check if there are two actual station times linking to one plan time
                            //bool covered = r.GetBoolean(3);
                            //if (covered)
                            //{
                            //    throw new ApplicationException("Two TDS signals link to one planned time");
                            //}
                            #endregion
                            trainNo = r.GetString(1);
                            plannedSTID = r.GetInt64(0);
                            bound = r.GetString(2);
                            plannedTime = r.GetDateTime(4);
                        }
                        r.Close();
                        #endregion

                        if (plannedSTID == 0)
                        {
                            hasPlannedTimeMapping = 0;
                            CommonUtils.AddTrainMovement(conn, emnuNo, "", station, "", time, "", plat,0);

                    //        conn.Close();
                            throw new Exception("Cannot match the record to timetable");
                      //      CommonUtils.DeleteTrainMovementByEMU(conn, emnuNo); //Need to talk to Connect Vendor to build API to remove forecast
                        }
                        else
                        {
                            TrainMovement lastMove = CommonUtils.GetLastTrainMovement(conn, emnuNo,plat);

                            if (lastMove == null) //If this is the first movement of the EMU
                            {
                                CommonUtils.AddTrainMovement(conn, emnuNo, trainNo, station, bound, time, "", plat, plannedSTID);

                                throw new Exception("No past record of emuNo " + emnuNo + " is found");
                                //Don't do forecast for the first movement
                            }
                            else
                            {
                                int travelTime = (int)(time - lastMove.LastSignalTime).TotalSeconds;

                                if (travelTime > 1800) //either split duty or accident
                                {
                                    CommonUtils.DeleteTrainMovementByEMU(conn, emnuNo);
                                }

                                CommonUtils.AddTrainMovement(conn, emnuNo, trainNo, station, bound, time, "", plat, plannedSTID);
                                int noServiceMovement = CommonUtils.GetNoServiceMovements(conn, emnuNo);
                                //Only start recording stn to stn travel time from the second station onwards, because very often the 
                                //first station movement does not follow timetable closely
                                #region In case there are more than 2 service movements, record stn to stn travel time
                                if (noServiceMovement > 2)
                                {
                                    int result = CommonUtils.AddStnToStnTravelTimeToday(conn, lastMove.Platform, plat, travelTime, time);
                                    if (result == -1)
                                    {
                                        throw new Exception("Error in adding stn to stn travel time today");
                                    }

                                    result = CommonUtils.UpdateTravelTime(conn, lastMove.Platform, plat);

                                    if (result == -1)
                                    {
                                        throw new Exception("Error in updating travel time");
                                    }

                                    CommonUtils.UpdateForecast(conn, trainNo, emnuNo, plannedTime, time, method);
                                }
                                else
                                {
                                    throw new Exception("Number of past service movement of " + emnuNo + " is <= 2");
                                }
                                #endregion


                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    List<SqlMetaData> metaData = new List<SqlMetaData>();
                    DataTable table = new DataTable();
                    table.Columns.Add("Status");
                    table.Columns.Add("Message");

                    DataRow row = table.NewRow();
                    row["Status"] = 1;
                    row["Message"] = ex.Message;
                    table.Rows.Add(row);

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        metaData.Add(new SqlMetaData(table.Columns[i].ColumnName, SqlDbType.Variant));
                    }

                    SqlDataRecord record = new SqlDataRecord(metaData.ToArray());
                    SqlContext.Pipe.SendResultsStart(record);

                    foreach (DataRow row1 in table.Rows)
                    {
                        for (int j = 0; j < table.Columns.Count; j++)
                        {
                            record.SetValue(j, row1[j]);
                        }

                        SqlContext.Pipe.SendResultsRow(record);
                    }
                    SqlContext.Pipe.SendResultsEnd();
                }
                finally
                {
                    conn.Close();
                    
                }

                
            }
            return hasPlannedTimeMapping;
        }

        [Microsoft.SqlServer.Server.SqlProcedure]
        public static int RecordTDSSignal(string plat, string station, DateTime time, string type, string emnuNo, int delay, string delayTime)
        {
            int hasPlannedTimeMapping = 1;
            SqlTransaction transaction = null;

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                try
                {
                    conn.Open();
                    string line = CommonUtils.GetLineByPlatform(conn, plat);
                    if (line == "EWL")
                    {
                        conn.Close();
                        return 1;
                    }

                    bool isService = CommonUtils.IsPlatformServiceable(conn, plat);

                    if (!isService)
                    {
                        conn.Close();
                        return 1;
                    }

                    int deviation = CommonUtils.GetDelayTime(delayTime, delay);

                    #region If type is dep, need to check whether there is already an arrival signal at the platform. If not, treat this departure signal as arrival
                    if (type == "DEP")
                    {
                        type = "ARR";
                        string sql = @"SELECT TOP 1 platform FROM TrainMovement 
                                    WHERE EMUNo = @emuNo order by [LastSignalTime] desc";
                        SqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = sql;
                        cmd.Parameters.AddWithValue(@"emuNo", emnuNo);

                        SqlDataReader r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            string lastPlt = r.GetString(0);
                            if (lastPlt == plat)
                            {
                                type = "DEP";
                            }
                        }
                        r.Close();
                    }
                    #endregion

                    #region Process arrival signal
                    if (type == "ARR")
                    {
                        Int64 plannedSTID = 0;
                        string trainNo = "";
                        string bound = null;
                        #region try to relate DTS signal to timetable
                        DateTime planTime = time.AddSeconds(-1 * deviation);
                        string sql = "";
                        SqlCommand cmd = conn.CreateCommand();
                        if (plat != "JURD" && delay != 0)
                        {
                            sql = @"SELECT dst.ID, dst.TrainNo, dst.bnd, dst.covered FROM DailyTrainStationTime dst
                        WHERE dst.plt = @platform and dst.[time] > @time1 and dst.[time] < @time2";
                            DateTime time1 = planTime.AddSeconds(-30);
                            DateTime time2 = planTime.AddSeconds(60);

                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue(@"platform", plat);
                            cmd.Parameters.AddWithValue(@"time1", time1);
                            cmd.Parameters.AddWithValue(@"time2", time2);
                        }

                        if (plat == "JURD" || delay == 0) //TDS cannot handle JURD platform properly, no deviation time was recorded
                        {
                            string train = CommonUtils.GetTrainNoByEMU(conn, emnuNo);
                            sql = @"SELECT dst.ID, dst.TrainNo, dst.bnd, dst.covered FROM DailyTrainStationTime dst
                        WHERE dst.trainno = @trainNo and dst.plt = @platform and dst.[time] > @time1 and dst.[time] < @time2";
                            DateTime time1 = planTime.AddSeconds(-200);
                            DateTime time2 = planTime.AddSeconds(200);

                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue(@"platform", plat);
                            cmd.Parameters.AddWithValue(@"trainNo", train);
                            cmd.Parameters.AddWithValue(@"time1", time1);
                            cmd.Parameters.AddWithValue(@"time2", time2);
                        }
                       
                        
                        SqlDataReader r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            #region We may need to look into the case to check if there are two actual station times linking to one plan time
                            //bool covered = r.GetBoolean(3);
                            //if (covered)
                            //{
                            //    throw new ApplicationException("Two TDS signals link to one planned time");
                            //}
                            #endregion
                            trainNo = r.GetString(1);
                            plannedSTID = r.GetInt64(0);
                            bound = r.GetString(2);
                        }
                        r.Close();
                        #endregion

                        if (plannedSTID == 0)
                        {
                            hasPlannedTimeMapping = 0;
                        }

                        CommonUtils.AddTrainMovement(conn, emnuNo, trainNo, station, bound, time, "", plat, plannedSTID);
                        CommonUtils.AddActualStationTime(conn, plannedSTID, emnuNo, trainNo, station, bound, time, "", plat);
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    int n = 0;
                }
                finally
                {
                    conn.Close();

                }


            }
            return hasPlannedTimeMapping;
        }

        

    
    }
}
