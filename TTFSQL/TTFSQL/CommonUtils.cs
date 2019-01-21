using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;
using Microsoft.SqlServer.Server;

namespace TTFSQL
{
    public class CommonUtils
    {
        public static int GetDelayTime(string diff, int intDelay)
        {
            int delay = 0;
            string[] elm = diff.Split(new char[] { ':' });
            delay = Convert.ToInt16(elm[0]) * 3600 + Convert.ToInt16(elm[1]) * 60 + Convert.ToInt16(elm[2]);
            if (intDelay < 0)
                delay = -1 * delay;

            return delay;
        }

        public static bool IsPlatformServiceable(SqlConnection conn, string plat)
        {
            string sql = @"select serviceable from platform where code = @code";
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue(@"code", plat);
            SqlDataReader r = cmd.ExecuteReader();
            bool isService = false;
            while (r.Read())
            {
                isService = r.GetBoolean(0);
            }
            r.Close();

            return isService;
        }

        public static int GetNoServiceMovements(SqlConnection conn, string emuNo)
        {
            string sql = @"select count(*) from TrainMovement where EMUNo = @emuNo and trainno is not null";
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue(@"emuNo", emuNo);
            SqlDataReader r = cmd.ExecuteReader();
            int no = 0;
            while (r.Read())
            {
                no = r.GetInt32(0);
            }
            r.Close();

            return no;
        }

        public static TrainMovement GetLastTrainMovement(SqlConnection conn, string emuNo, string platform)
        {
            TrainMovement record = null;
            using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 1 ID, trainNo, lastStn, bound, lastSignalTime, trainNoAlias, platform FROM TrainMovement WHERE EMUNO = @emuNo and trainNo <> '' and platform <> @platform
                                                   order by lastSignalTime desc ", conn))
            {
                cmd.Parameters.AddWithValue("@emuNo", emuNo);
                cmd.Parameters.AddWithValue("@platform", platform);

                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    record = new TrainMovement();
                    record.ID = r.GetInt32(0);
                    record.TrainNo = r["trainNo"].ToString();
                    record.TrainNoAlias = r["trainNoAlias"].ToString();
                    record.Platform = r["platform"].ToString();
                    if (!r.IsDBNull(2))
                        record.LastStn = r.GetString(2);
                    if (!r.IsDBNull(3))
                        record.Bound = r.GetString(3);
                    if (!r.IsDBNull(4))
                        record.LastSignalTime = r.GetDateTime(4);

                    break;
                }
                r.Close();
            }
            return record;
        }

        public static string GetLineByPlatform(SqlConnection conn, string plat)
        {
            string line = null;
            using (SqlCommand cmd = new SqlCommand(@"select tl.Code from Platform plt inner join station stn on plt.StationID = stn.id 
inner join trainline tl on stn.lineCode = tl.code
where plt.Code = @plat ", conn))
            {
                cmd.Parameters.AddWithValue("@plat", plat);

                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    line = r.GetString(0);
                }
                r.Close();
            }
            return line;
        }

        public static int AddTrainMovement(SqlConnection conn, string emuNo, string trainNo, string stn, string bound, DateTime time, string trainNoAlias, string platform, Int64 plannedID)
        {
            int result = -1;

            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("", conn, t);

                    string headSql = "INSERT INTO TrainMovement(";
                    string valueSql = " Values (";

                    headSql += "[EMUNo],";
                    valueSql += "@emuNo,";
                    cmd.Parameters.Add(new SqlParameter("@emuNo", emuNo));

                    headSql += "[LastStn],";
                    valueSql += "@stn,";
                    cmd.Parameters.Add(new SqlParameter("@stn", stn));

                    if (bound != null)
                    {
                        headSql += "[Bound],";
                        valueSql += "@bound,";
                        cmd.Parameters.Add(new SqlParameter("@bound", bound));
                    }

                    headSql += "[LastSignalTime],";
                    valueSql += "@time,";
                    cmd.Parameters.Add(new SqlParameter("@time", time));

                    headSql += "[TrainNoAlias],";
                    valueSql += "@trainNoAlias,";
                    cmd.Parameters.Add(new SqlParameter("@trainNoAlias", trainNoAlias));

                    headSql += "[PlannedSTID],";
                    valueSql += "@PlannedSTID,";
                    cmd.Parameters.Add(new SqlParameter("@PlannedSTID", plannedID));

                    headSql += "[Platform],";
                    valueSql += "@platform,";
                    cmd.Parameters.Add(new SqlParameter("@platform", platform));

                    headSql += "[TrainNo]) ";
                    valueSql += "@trainNo) ";
                    cmd.Parameters.Add(new SqlParameter("@trainNo", trainNo));

                    cmd.CommandText = headSql + valueSql;
                    cmd.ExecuteNonQuery();
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw new ApplicationException(ex.Message);
                }
            }

            return result;
        }

        public static int UpdateTrainMovement(SqlConnection conn, int ID, DateTime time, Int64 plannedID)
        {
            int result = -1;

            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("", conn, t);
                    string cmdText = @"UPDATE TrainMovement SET PlannedSTID = @plannedID, LastSignalTime = @time WHERE ID = @id";
                    cmd.CommandText = cmdText;
                    cmd.Parameters.AddWithValue("@plannedID", plannedID);
                    cmd.Parameters.AddWithValue("@time", time);
                    cmd.Parameters.AddWithValue("@id", ID);
                    cmd.ExecuteNonQuery();
                 
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw new ApplicationException(ex.Message);
                }
            }

            return result;
        }

        public static int UpdateActualStationTime(SqlConnection conn, string emuNo, string platform, DateTime time, Int64 plannedID)
        {
            int result = -1;

            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("", conn, t);
                    string cmdText = @"SELECT TOP 1 ID, [PLATFORM] FROM DailyActualStationTime WHERE emuNo = @emuNo ORDER BY Time DESC";
                    cmd.CommandText = cmdText;
                    cmd.Parameters.AddWithValue("@emuNo", emuNo);
                    SqlDataReader r = cmd.ExecuteReader();
                    int ID = 0;
                    string plat = "";
                    if (r.Read())
                    {
                        ID = r.GetInt32(0);
                        plat = r.GetString(1);
                    }
                    r.Close();

                    if (ID != 0 && plat == platform)
                    {
                        cmdText = @"UPDATE DailyActualStationTime SET PlannedStationTime = @plannedID, time = @time WHERE ID = @id";
                        cmd = new SqlCommand("", conn, t);
                        cmd.CommandText = cmdText;
                        cmd.Parameters.AddWithValue("@plannedID", plannedID);
                        cmd.Parameters.AddWithValue("@time", time);
                        cmd.Parameters.AddWithValue("@id", ID);
                        cmd.ExecuteNonQuery();
                    }


                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw new ApplicationException(ex.Message);
                }
            }

            return result;
        }

        public static int AddActualStationTime(SqlConnection conn, long plannedID, string emuNo, string trainNo, string stn, string bound, DateTime time, string trainNoAlias, string platform)
        {
            int result = -1;

            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("", conn, t);

                    string headSql = "INSERT INTO DailyActualStationTime(";
                    string valueSql = " Values (";

                    headSql += "[EMUNo],";
                    valueSql += "@emuNo,";
                    cmd.Parameters.Add(new SqlParameter("@emuNo", emuNo));

                    headSql += "[Stn],";
                    valueSql += "@stn,";
                    cmd.Parameters.Add(new SqlParameter("@stn", stn));

                    headSql += "[PlannedStationTime],";
                    valueSql += "@ID,";
                    cmd.Parameters.Add(new SqlParameter("@ID", plannedID));

                    if (bound != null)
                    {
                        headSql += "[Bound],";
                        valueSql += "@bound,";
                        cmd.Parameters.Add(new SqlParameter("@bound", bound));
                    }

                    headSql += "[Time],";
                    valueSql += "@time,";
                    cmd.Parameters.Add(new SqlParameter("@time", time));

                    headSql += "[Platform],";
                    valueSql += "@platform,";
                    cmd.Parameters.Add(new SqlParameter("@platform", platform));

                    headSql += "[TrainNo]) ";
                    valueSql += "@trainNo) ";
                    cmd.Parameters.Add(new SqlParameter("@trainNo", trainNo));

                    cmd.CommandText = headSql + valueSql;
                    cmd.ExecuteNonQuery();
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw new ApplicationException(ex.Message);
                }
            }

            return result;
        }

        public static void DeleteTrainMovementByEMU(SqlConnection conn, string emuNo)
        {
            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("", conn, t);

                    string sql = "delete from TrainMovement where EMUNo = @emuNo";

                    cmd.CommandText = sql;
                    cmd.Parameters.Add(new SqlParameter("@emuNo", emuNo));


                    cmd.ExecuteNonQuery();
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw new ApplicationException(ex.Message);
                }
            }
        }

        public static string GetTrainNoByEMU(SqlConnection conn, string emuNo)
        {
            string trainno = "";
            string sql = @"select top 1 trainno from TrainMovement where emuno = @emuNo order by LastSignalTime desc";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("emuNo", emuNo);
            SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                trainno = r.GetString(0);
            }
            r.Close();
            return trainno;
        }

        public static int AddStnToStnTravelTimeToday(SqlConnection conn, string plat1, string plat2, int travelTime, DateTime arrivalTime)
        {
            int result = -1;

            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    string query = @"INSERT INTO StnToStnTravelTimeToday(fromPlat, toPlat, traveltime, arrivalTime)
                                VALUES(@plat1,@plat2,@travelTime, @arrivalTime) SELECT SCOPE_IDENTITY() ";
                    SqlCommand cmd = new SqlCommand(query, conn, t);
                    cmd.Parameters.AddWithValue("plat1", plat1);
                    cmd.Parameters.AddWithValue("plat2", plat2);
                    cmd.Parameters.AddWithValue("travelTime", travelTime);
                    cmd.Parameters.AddWithValue("arrivalTime", arrivalTime);

                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        result = 1;
                    }
                    r.Close();

                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw new ApplicationException(ex.Message);
                }
               
            }

            return result;
        }

        public static int DeleteMovementByEMU(SqlConnection conn, string emu)
        {
            int result = -1;

            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    string query = @"DELETE FROM DailyForecastArrivalTime where EMUNo= @emu";
                    SqlCommand cmd = new SqlCommand(query, conn, t);
                    cmd.Parameters.AddWithValue("emu", emu);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw new ApplicationException(ex.Message);
                }

            }

            return result;
        }

        public static int  UpdateTravelTime(SqlConnection conn, string plat1, string plat2)
        {
            int result = -1;
            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    string query = @"select top 3 travelTime from StnToStnTravelTimeToday 
where FromPlat = @from and ToPlat = @to order by ArrivalTime desc";
                    SqlCommand cmd = new SqlCommand(query, conn, t);
                    cmd.Parameters.AddWithValue("from", plat1);
                    cmd.Parameters.AddWithValue("to", plat2);

                    SqlDataReader r = cmd.ExecuteReader();
                    List<int> times = new List<int>();
                    while (r.Read())
                    {
                        times.Add(r.GetInt32(0));
                    }
                    r.Close();

                    int travelTime = -1;
                    if (times.Count < 3)
                    {
                        travelTime = (int)Math.Floor(Mean(times));
                    }
                    else
                    {
                        travelTime = (int)Math.Floor(Mean3WithMinVar(times));
                    }

                    query = @"select ID from LastAvgTravelTime WHERE FromPLAT = @from and ToPLAT=@to";
                    cmd = new SqlCommand(query, conn, t);
                    cmd.Parameters.AddWithValue("from", plat1);
                    cmd.Parameters.AddWithValue("to", plat2);
                    int ID = -1;
                    r = cmd.ExecuteReader();

                    if (r.Read())
                    {
                        ID = r.GetInt32(0);
                    }
                    r.Close();

                    if (ID == -1)//No existing record
                    {
                        query = @"INSERT INTO LastAvgTravelTime(fromPlat, toPlat, traveltime)
                                VALUES(@plat1,@plat2,@travelTime)";
                        cmd = new SqlCommand(query, conn, t);
                        cmd.Parameters.AddWithValue("plat1", plat1);
                        cmd.Parameters.AddWithValue("plat2", plat2);
                        cmd.Parameters.AddWithValue("travelTime", travelTime);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        query = @"UPDATE LastAvgTravelTime SET travelTime = @time where ID=@ID";
                        cmd = new SqlCommand(query, conn, t);
                        cmd.Parameters.AddWithValue("time", travelTime);
                        cmd.Parameters.AddWithValue("ID", ID);
                        cmd.ExecuteNonQuery();
                    }

                    t.Commit();
                    result = 1;
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    result = -1;
                    throw new ApplicationException(ex.Message);
                }

            }

            return result;
        }

        public static void UpdateForecast(SqlConnection conn, string trainNo, string emuNo, DateTime plannedTime, DateTime actualTime, int method)
        {
            DateTime threshold = actualTime.Date;
            TimeSpan ts = new TimeSpan(5, 30, 0);
            threshold = threshold + ts;

            DataTable table = BuildForecastTable();
            using (SqlTransaction t = conn.BeginTransaction())
            {
                try
                {
                    #region read in plannedstationtime for two bounds
                    List<DailyTrainStationTime> list = new List<DailyTrainStationTime>();
                    string query = @"select dst.bnd, dst.stn, dst.[ID], dst.plt, dst.time from [dbo].[DailyTrainStationTime] dst
                                   where  dst.TrainNo = @trainNo and dst.[Time] >= @time 
                                   order by dst.[Time]";
                    SqlCommand cmd = new SqlCommand(query, conn, t);

                    cmd.Parameters.AddWithValue("trainNo", trainNo);
                    cmd.Parameters.AddWithValue("time", plannedTime);

                    SqlDataReader r = cmd.ExecuteReader();
                    List<string> mapBoundCovered = new List<string>();
                    List<DateTime> listPlannedTime = new List<DateTime>();
                    string currBound = "";
                    while (r.Read())
                    {
                        DailyTrainStationTime st = new DailyTrainStationTime();
                        string bound = r.GetString(0);

                        if (bound == "EAST" && currBound == "WEST") //We only forecast one round for west bound train due to CBTC signal unavailable
                            break;

                        if (bound != currBound)
                        {
                            if (mapBoundCovered.Contains(bound)) //just need to forecast for two bounds
                                break;
                            else
                                mapBoundCovered.Add(bound);
                        }
                        st.bound = bound;
                        st.station = r.GetString(1);
                        st.plannedSTID = r.GetInt64(2);
                        st.platform = r.GetString(3);
                        st.time = r.GetDateTime(4);
                        currBound = bound;
                        list.Add(st);
                        listPlannedTime.Add(r.GetDateTime(4));
                    }
                    r.Close();
                    #endregion

                    #region read in actual stationtime for two bounds
//                    List<DailyTrainStationTime> actuallist = new List<DailyTrainStationTime>();
//                    query = @"select dst.bound, dst.stn, dst.PlannedStationTime, dst.platform, dst.time from [dbo].[dailyactualstationtime] dst
//                                   where  dst.EMUNo = @emuNo and dst.[Time] >= @time 
//                                   order by dst.[Time]";
//                    cmd = new SqlCommand(query, conn, t);

//                    cmd.Parameters.AddWithValue("emuNo", emuNo);
//                    cmd.Parameters.AddWithValue("time", actualTime);

//                    r = cmd.ExecuteReader();
//                    mapBoundCovered = new List<string>();
//                    currBound = "";
//                    while (r.Read())
//                    {                       
//                        DailyTrainStationTime st = new DailyTrainStationTime();
//                        st.platform = r.GetString(3);
//                        string bound = "";
//                        //if (r.IsDBNull(0) && st.platform != "JURD")
//                        //    continue;

//                        if (!r.IsDBNull(0))
//                            bound = r.GetString(0);

//                        if (bound == "EAST" && currBound == "WEST")
//                            break;

//                        if (bound != "" && bound != currBound)
//                        {
//                            if (mapBoundCovered.Contains(bound)) //just need to forecast for two bounds
//                                break;
//                            else
//                                mapBoundCovered.Add(bound);
//                            currBound = bound;
//                        }
//                        st.bound = bound;
//                        st.station = r.GetString(1);
//                        st.time = r.GetDateTime(4);

//                        if (actuallist.Count > 0 && st.station == actuallist[actuallist.Count - 1].station)
//                            continue;

//                        if (actuallist.Count > 0 && (st.time - actuallist[actuallist.Count - 1].time).TotalMinutes > 60)
//                            break;

//                        st.plannedSTID = r.GetInt64(2);
                        
                        
//                        actuallist.Add(st);
//                    }
//                    r.Close();
                    #endregion

                    #region Now, do the forecasting

                    if (list.Count == 0)
                    {
                        throw new Exception("No planned station time records are retrieved");
                    }

                    list[0].time = actualTime;
                    string prevBound = list[0].bound;
                    bool secondBound = false;

                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        DailyTrainStationTime currST = list[i];
                        DailyTrainStationTime nextST = list[i + 1];
                        if (!secondBound && nextST.bound != prevBound)
                            secondBound = true;

                        TimeSpan timespan = nextST.time - currST.time;
                        if (timespan.TotalHours > 1)   //split duty case
                            break;

                        //int hisTime = GetAvgTravelTime(new SqlCommand("", conn, t), currST.Platform, nextST.Platform, currST.Time, ForecastManagementBL.dateType);

                        //if (hisTime == -1)
                        //    hisTime = GetAvgTravelTime(new SqlCommand("", conn, t), currST.Platform, nextST.Platform, currST.Time.AddMinutes(30), ForecastManagementBL.dateType);

                        //if (nextST.platform == "JURD" || nextST.platform == "JURE") //A special case where trains don't run according to timetable.
                        //    nextST.platform = "JURDE";

                        int travelTime = -1;

                        if (method == 1)
                        {
                            if (secondBound)
                                travelTime = GetAvgTravelTime(new SqlCommand("", conn, t), currST.platform, nextST.platform, currST.time);
                            if (travelTime == -1 || !secondBound)
                                travelTime = CommonUtils.GetLatestTravelTime(new SqlCommand("", conn, t), currST.platform, nextST.platform, -1);
                        }
                        if (method == 2)
                        {
                            travelTime = CommonUtils.GetLatestTravelTime(new SqlCommand("", conn, t), currST.platform, nextST.platform, -1);
                        }
                        if (method == 3)
                        {
                            if (i>=9)
                                travelTime = GetAvgTravelTime(new SqlCommand("", conn, t), currST.platform, nextST.platform, currST.time);
                            if (travelTime == -1 || i<9)
                                travelTime = CommonUtils.GetLatestTravelTime(new SqlCommand("", conn, t), currST.platform, nextST.platform, -1);
                        }
                            

                        if (travelTime == -1)   //end of current service at currST
                        {
                            break;
                        }

                        #region handle turnaround station specially
                        //bool hasReachedTerminal = false;
                        //if (i > 0 && i < list.Count - 1 && list[i - 1].Station == list[i + 1].Station)
                        //{
                        //    hasReachedTerminal = true;
                        //    DateTime forecastTime = currST.Time.AddSeconds(travelTime);

                        //    if((forecastTime.Hour > 7 && forecastTime.Hour<10) || (forecastTime.Hour > 17 && forecastTime.Hour<20) )
                        //        continue;

                        //    if (forecastTime > nextST.Time)
                        //    {
                        //        travelTime = (int)Math.Max(travelTime - 180, 180);  //make sure turnaround time is not less than 3 mins
                        //        TimeSpan tsGap = nextST.Time - currST.Time;
                        //        int gap = (int)tsGap.TotalSeconds;
                        //        travelTime = (int)Math.Max(gap, travelTime);   //make sure forecast time is not earlier than planned time.
                        //    }
                        //    else
                        //    {
                        //        travelTime = (int)Math.Min(travelTime + 120, 360);  //make sure turnaround time is not more than 6 mins
                        //        TimeSpan tsGap = nextST.Time - currST.Time;
                        //        int gap = (int)tsGap.TotalSeconds;
                        //        travelTime = (int)Math.Min(gap, travelTime);   //make sure forecast time is not earlier than planned time.
                        //    }
                        //}


                        #endregion

                        #region Forecast the next arrival time
                        if(actualTime >= threshold)
                            nextST.time = currST.time.AddSeconds(travelTime);

                        DataRow row = table.NewRow();
                        row["TrainNo"] = trainNo;
                        row["EMUNo"] = emuNo;
                        row["Time"] = nextST.time;
                        row["PlannedStationTime"] = nextST.plannedSTID;
                        row["Platform"] = nextST.platform;
                        row["Station"] = nextST.station;
                        row["Bound"] = nextST.bound;
                        table.Rows.Add(row);
                        CommonUtils.AddForecastTime(new SqlCommand("", conn, t), trainNo,emuNo,nextST.time,nextST.plannedSTID,nextST.platform,nextST.station,nextST.bound, actualTime);
                        #endregion

                        #region Check forecast error against the actual arrival time
                        //if (i < actuallist.Count - 1 && actuallist[i + 1].station != list[i + 1].station)
                        //{
                        //    int de = 0;
                        //}

                        //if (i < actuallist.Count - 1 && actuallist[i + 1].station == list[i + 1].station)
                        //{
                        //    int forecastError = (int)Math.Abs((actuallist[i + 1].time - nextST.time).TotalSeconds);
                        //    int planError = (int)Math.Abs((actuallist[i + 1].time - listPlannedTime[i + 1]).TotalSeconds);

                        //    if (forecastError > 5000)
                        //    {
                        //        int n = 0;
                        //    }
                        //    AddForecastError(new SqlCommand("", conn, t), trainNo, emuNo, actualTime, nextST.station, forecastError, planError, nextST.time, listPlannedTime[i+1], actuallist[i + 1].time);
                        //}
                        #endregion

                        //count++;
                        //totalerror += gap;

                        //if (gap > maxerror)
                        //{
                        //    maxerror = gap;
                        //}

                        //if (gap < minError)
                        //{
                        //    minError = gap;
                        //}
                        //        Logging.log.Debug("Trying to DeleteForecastedTimeByPlannedST");
                        //DeleteForecastedTimeByEMUandStn(new SqlCommand("", conn, t), emuNo, nextST.StationID, nextST.BoundId);
                       // DeleteForecastedTimeByPlannedST(new SqlCommand("", conn, t), nextST.ID);
                        //DeleteForecastedTimeByTime(new SqlCommand("", conn, t), nextST.Time, nextST.StationID, nextST.BoundId);
                        //int result = AddForecastedArrivalTime(new SqlCommand("", conn, t), emuNo, nextST.TrainNo, nextST.TrainNo, nextST.StationID, nextST.BoundId, nextST.Time, nextST.ID, true, nextST.Platform);

                        //QueueForecastForSending(new SqlCommand("", conn, t), result);                       
                    }
                    #endregion

                    t.Commit();

                    #region Send Data to Client
                    List<SqlMetaData> metaData = new List<SqlMetaData>();
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        metaData.Add(new SqlMetaData(table.Columns[i].ColumnName, SqlDbType.Variant));
                    }

                    SqlDataRecord record = new SqlDataRecord(metaData.ToArray());
                    SqlContext.Pipe.SendResultsStart(record);

                    foreach (DataRow row in table.Rows)
                    {
                        for (int j = 0; j < table.Columns.Count; j++)
                        {
                            record.SetValue(j, row[j]);
                        }

                        SqlContext.Pipe.SendResultsRow(record);
                    }
                    SqlContext.Pipe.SendResultsEnd();
                    #endregion
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw new ApplicationException("Error in update forecast by TT: " + ex.Message);
                }
               

            }
        }



        public static int GetAvgTravelTime(SqlCommand cmd, string fromPlat, string toPlat, DateTime currTime)
        {
            int travelTime = -1;

            string query = @"select travelTime from AvgStnToStnTravelTime where fromPlat = @fromPlat and toPlat=@toPlat and fromTime <= convert(time,@currTime) and toTime > convert(time,@currTime)";
            cmd.CommandText = query;

            cmd.Parameters.AddWithValue("fromPlat", fromPlat);
            cmd.Parameters.AddWithValue("toPlat", toPlat);
            cmd.Parameters.AddWithValue("currTime", currTime);

            SqlDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                travelTime = r.GetInt32(0);
            }
            r.Close();

            if (travelTime == -1)
            {
                string query1 = @"select travelTime from AvgStnToStnTravelTime where 
                             fromPlat like @fromPlat and toPlat like @toPlat and fromTime <= convert(time,@currTime) and toTime > convert(time,@currTime)";
                SqlCommand cmd1 = new SqlCommand(query1, cmd.Connection, cmd.Transaction);

                cmd1.Parameters.AddWithValue("fromPlat", fromPlat.Substring(0, 3) + "%");
                cmd1.Parameters.AddWithValue("toPlat", toPlat.Substring(0, 3) + "%");
                cmd1.Parameters.AddWithValue("currTime", currTime);

                SqlDataReader r1 = cmd1.ExecuteReader();

                while (r1.Read())
                {
                    travelTime = r1.GetInt32(0);
                }
                r1.Close();
            }


            return travelTime;
        }

        public static int AddForecastTime(SqlCommand cmd, string trainNo, string emuNo, DateTime time, long plannedID, string platform, string station, string bound, DateTime forecastTime)
        {
            string query = @"INSERT INTO DailyForecastedArrivalTime (TrainNo, EmuNo, Time, PlannedStationTime, IsService, Platform, Stn, Bnd, forecasttime)
                            VALUES (@trainNo, @emuNo, @time, @plannedID, 1, @platform,@stn,@bnd, @forecasttime)";
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("@trainNo", trainNo);
            cmd.Parameters.AddWithValue("@emuNo", emuNo);
            cmd.Parameters.AddWithValue("@time", time);
            cmd.Parameters.AddWithValue("@plannedID", plannedID);
            cmd.Parameters.AddWithValue("@platform", platform);
            cmd.Parameters.AddWithValue("@stn", station);
            cmd.Parameters.AddWithValue("@bnd", bound);
            cmd.Parameters.AddWithValue("@forecasttime", forecastTime);

            cmd.ExecuteNonQuery();
            return 1;
        }

        public static int AddForecastError(SqlCommand cmd, string trainNo, string emuNo, DateTime time, string station, int forecastError, int planError, DateTime forecastedTime, DateTime plannedTime, DateTime ActualTime)
        {
            string query = @"INSERT INTO ForecastError (TrainNo, EmuNo, Time,Stn, ForecastError, ActualError,ForecastTime, PlannedTime,ActualTime)
                            VALUES (@trainNo, @emuNo, @time, @Stn,  @ForecastError,@ActualError,@ForecastTime,@PlannedTime, @ActualTime)";
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("@trainNo", trainNo);
            cmd.Parameters.AddWithValue("@emuNo", emuNo);
            cmd.Parameters.AddWithValue("@time", time);
            cmd.Parameters.AddWithValue("@Stn", station);
            cmd.Parameters.AddWithValue("@ForecastError", forecastError);
            cmd.Parameters.AddWithValue("@ActualError", planError);
            cmd.Parameters.AddWithValue("@ForecastTime", forecastedTime);
            cmd.Parameters.AddWithValue("@PlannedTime", plannedTime);
            cmd.Parameters.AddWithValue("@ActualTime", ActualTime);

            cmd.ExecuteNonQuery();
            return 1;
        }

        /// <summary>
        /// Get the latest travel time between give stations from the database
        /// </summary>
        /// <param name="stn1"></param>
        /// <param name="stn2"></param>
        /// <returns></returns>
        public static int GetLatestTravelTime(SqlCommand cmd, string plat1, string plat2, int historicalTime)
        {
            int time = -1;

            cmd.CommandText = @"SELECT travelTime FROM LastAvgTravelTime 
                           WHERE fromPlat=@plat1 and toPlat=@plat2";

            cmd.Parameters.AddWithValue("@plat1", plat1);
            cmd.Parameters.AddWithValue("@plat2", plat2);
            SqlDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                time = r.GetInt32(0);
            }
            r.Close();

            if (time == -1)
                time = historicalTime;


            return time;
        }

        public static DailyTrainStationTime GetPlannedStationTimeByATSSData(SqlConnection conn, string trainno, string platform, DateTime time, string stn)
        {
            DailyTrainStationTime st = null;
            string likeNo = "%" + trainno.Substring(1, 2);

            int deviation = 480;
            DateTime startTime = time.AddSeconds(-1 * deviation);
            DateTime endTime = time.AddSeconds(deviation);

            string cmdText = "";
            bool isTerminal = false;

            #region Find the type of the station
            cmdText = @"SELECT [Type] FROM Station where Code = @code and LineCode = 'EWL'";
            SqlCommand cmd = new SqlCommand(cmdText, conn);
            cmd.Parameters.AddWithValue("code", stn);
            SqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
            {
                string type = r.GetString(0);
                if (type == "Terminal")
                    isTerminal = true;
            }
            r.Close();
            #endregion

            cmdText = @"SELECT dst.ID, dst.TrainNo, dst.bnd, dst.covered, dst.time FROM DailyTrainStationTime dst
                    WHERE dst.plt = @platform and dst.[time] > @time1 and dst.[time] < @time2 
                    and line = 'EWL' and trainNo like @trainNo";
            cmd = new SqlCommand(cmdText, conn);
            cmd.Parameters.AddWithValue("platform", platform);
            cmd.Parameters.AddWithValue("time1", startTime);
            cmd.Parameters.AddWithValue("time2", endTime);
            cmd.Parameters.AddWithValue("trainNo", likeNo);

            r = cmd.ExecuteReader();
            int count = 0;
            bool found = false;
            while (r.Read())
            {
                st = new DailyTrainStationTime();
                st.trainNo = r.GetString(1);
                st.plannedSTID = r.GetInt64(0);
                st.bound = r.GetString(2);
                st.time = r.GetDateTime(4);
                if (st.trainNo == trainno)
                {
                    found = true;
                    break;
                }
                else
                    count++;
            }
            r.Close();
            if (!found && count > 1)
            {
                throw new Exception("Duplicated planned train numbers for one actual signal");
            }

            //There is case where the actual platform will be different from planned platform when the station is terminal
            //This block find the planned station time via station 
            if(st == null && isTerminal)
            {
                cmdText = @"SELECT dst.ID, dst.TrainNo, dst.bnd, dst.covered, dst.time FROM DailyTrainStationTime dst
                    WHERE dst.stn = @stn and dst.[time] > @time1 and dst.[time] < @time2 
                    and line = 'EWL' and trainNo like @trainNo";
                cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("stn", stn);
                cmd.Parameters.AddWithValue("time1", startTime);
                cmd.Parameters.AddWithValue("time2", endTime);
                cmd.Parameters.AddWithValue("trainNo", likeNo);

                r = cmd.ExecuteReader();
                count = 0;
                found = false;
                while (r.Read())
                {
                    st = new DailyTrainStationTime();
                    st.trainNo = r.GetString(1);
                    st.plannedSTID = r.GetInt64(0);
                    st.bound = r.GetString(2);
                    st.time = r.GetDateTime(4);
                    if (st.trainNo == trainno)
                    {
                        found = true;
                        break;
                    }
                    else
                        count++;
                }
                r.Close();

                if (!found && count > 1)
                {
                    throw new Exception("Duplicated planned train numbers for one actual signal");
                }
            }

            if (st != null)
            {
                cmdText = @"UPDATE DailyTrainStationTime SET covered = 1 WHERE ID = @id";
                cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("id", st.plannedSTID);
                cmd.ExecuteNonQuery();
            }
            return st;
        }
           
            

        public static string GetEMUNoByTrainNo(SqlConnection conn, string trainno, string platform, DateTime time)
        {
            string emu = "";

            //Find the planned station time via platform
            string cmdText = @"select EMUNo from TrainNoEMUNoMapping where trainno=@trainno";

            using (SqlCommand cmd = new SqlCommand(cmdText, conn))
            {
                cmd.Parameters.AddWithValue("trainNo", trainno);

                SqlDataReader r = cmd.ExecuteReader();

                if (r.Read())
                {
                    emu = r.GetString(0);
                }

                r.Close();
            }

            #region If still can't find an EMU to map, then look for a train number that shares the same last three digits of the train no
            if (emu == "" && trainno.Length < 4)
            {
                cmdText = @"select EMUNo from TrainNoEMUNoMapping where trainno like @trainno";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("trainno", "%" + trainno.Substring(trainno.Length - 3, 3));

                    SqlDataReader r = cmd.ExecuteReader();

                    if (r.Read())
                    {
                        emu = r.GetString(0);
                    }

                    r.Close();
                }


                if (emu != "")
                {

                    cmdText = @"Update TrainNoEMUNoMapping set oldTrainNo = trainno, trainno = @trainno,  DateCreated=(select GETDATE()) where emuno=@emuno";

                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("trainno", trainno);
                        cmd.Parameters.AddWithValue("emuno", emu);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            #endregion

            return emu;
        }


        public static double Mean(List<int> list)
        {
            int sum = 0;
            foreach (int no in list)
                sum += no;
            return sum / ((double)list.Count);
        }

        public static double Variance(List<int> list, double mean)
        {
            double total = 0;
            foreach (int no in list)
            {
                total += (no - mean) * (no - mean);
            }

            return total / ((double)list.Count);
        }

        public static double Mean(int no1, int no2)
        {
            double sum = no1 + no2;
            return sum / 2.0;
        }

        public static double Variance(int no1, int no2, double mean)
        {
            double total = (no1-mean)*(no1-mean) + (no2-mean)*(no2-mean);
            

            return total / 2;
        }

        /// <summary>
        /// Find the mean of two among three that has the minimum variance
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static double Mean3WithMinVar(List<int> list)
        {
            double mean, var;

            mean = Mean(list[0], list[1]);
            var = Variance(list[0], list[1], mean);

            double mean1 = Mean(list[0], list[2]);
            double var1 = Variance(list[0], list[2], mean1);
            if (var1 < var)
            {
                var = var1;
                mean = mean1;
            }

            double mean2 = Mean(list[1], list[2]);
            double var2 = Variance(list[1], list[2], mean2);
            if (var2 < var)
            {
                var = var2;
                mean = mean2;
            }

            return mean;
        }

        private static DataTable BuildForecastTable()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("TrainNo");
            dataTable.Columns.Add("EMUNo");
            dataTable.Columns.Add("Time");
            dataTable.Columns.Add("PlannedStationTime");
            dataTable.Columns.Add("Platform");
            dataTable.Columns.Add("Station");
            dataTable.Columns.Add("Bound");
            return dataTable;
        }
    }

}
