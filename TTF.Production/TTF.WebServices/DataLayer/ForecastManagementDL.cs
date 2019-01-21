///
///<file>ForecastManagementDL.cs</file>
///<description>
/// Data layer for managing train travel time forecast. 
///</description>
///

///
///<created>
///<author>Liu Qizhang</author>
///<date>04-10-2014</date>
///</created>
///


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Configuration;
using System.Data.SqlClient;
using TTF.Utils;
using System.Data;
using TTF.Models;
using TTF.Models.OperationManagement;
using System.Text;
using TTF.BusinessLogic;
using TTF.Models.SQS;
using System.ComponentModel;
using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
namespace TTF.DataLayer
{
    public class ForecastManagementDL
    {
        
        public static List<ForecastedTrainArrivalTime> forecasts = new List<ForecastedTrainArrivalTime>();
       
        
        #region Historical station to station travel time
       
        /// <summary>
        /// Get the list of travel time between give stations from the database
        /// </summary>
        /// <param name="stn1"></param>
        /// <param name="stn2"></param>
        /// <returns></returns>
        public List<StnToStnTravelTime> GetHistoricalTravelTime(string fromPlat, string toPlat)
        {
            List<StnToStnTravelTime> timeList = new List<StnToStnTravelTime>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT fromPlat, toPlat, arrivaltime, traveltime FROM StnToStnTravelTime 
                                                                                        WHERE fromPlat=@fromPlat and toPlat=@toPlat", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@fromPlat", fromPlat);
                    cmd.Parameters.AddWithValue("@toPlat", toPlat);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        StnToStnTravelTime time = new StnToStnTravelTime();
                        time.FromPlat = r["fromPlat"].ToString();
                        time.ToPlat = r["toPlat"].ToString();
                        time.TravelTime = Convert.ToInt32(r["traveltime"]);
                        time.ArrivalTime = Convert.ToDateTime(r["arrivaltime"]);

                        timeList.Add(time);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return timeList;
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
            int result = -1;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    //// Insert into Station table and get Station id
                    string query = @"INSERT INTO StnToStnTravelTime(fromPlat, toPlat, arrivaltime, traveltime)
                                    VALUES(@fromPlat,@toPlat,@arrivalTime,@travelTime) SELECT SCOPE_IDENTITY()";
                    SqlCommand cmd = new SqlCommand(query, conn, t);

                    cmd.Parameters.AddWithValue("fromPlat", fromPlat);
                    cmd.Parameters.AddWithValue("stn2", toPlat);
                    cmd.Parameters.AddWithValue("arrivalTime", arrivalTime);
                    cmd.Parameters.AddWithValue("travelTime", travelTime);


                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        result = 1;
                    }
                    r.Close();


                    t.Commit();
                }
                conn.Close();
            }

            return result;
        }
        #endregion

        #region Manage real time train movement

        /// <summary>
        /// Update the train movement details for a given EMUNo
        /// </summary>
        /// <param name="emuNo"></param>
        /// <param name="trainNo"></param>
        /// <param name="stn"></param>
        /// <param name="bound"></param>
        /// <param name="time"></param>
        public void UpdateTrainMovement(string emuNo, string trainNo, short stn, short? bound, DateTime time, string trainNoAlias, string platform)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("", conn);
                string cmdText = "UPDATE TrainMovement set ";


                cmdText += "[TrainNo] = @trainNo,";
                cmd.Parameters.Add(new SqlParameter("@trainNo", trainNo));

                cmdText += "[LastStn] = @stn,";
                cmd.Parameters.Add(new SqlParameter("@stn", stn));


                if (bound != null)
                {
                    cmdText += "[Bound] = @bound,";
                    cmd.Parameters.Add(new SqlParameter("@bound", bound.Value));
                }


                cmdText += "[LastSignalTime] = @time,";
                cmd.Parameters.Add(new SqlParameter("@time", time));

                cmdText += "[TrainNoAlias] = @trainNoAlias,";
                cmd.Parameters.Add(new SqlParameter("@trainNoAlias", trainNoAlias));

                cmdText += "[Platform] = @platform,";
                cmd.Parameters.Add(new SqlParameter("@platform", platform));


                cmdText = cmdText.Substring(0, cmdText.Length - 1);

                cmdText += " where [EmuNo]=@emuNo";
                cmd.Parameters.Add(new SqlParameter("@emuNo", emuNo));

                cmd.CommandText = cmdText;

                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Add a train movement into the database
        /// </summary>
        /// <param name="emuNo"></param>
        /// <param name="trainNo"></param>
        /// <param name="stn"></param>
        /// <param name="bound"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public int AddTrainMovement(string emuNo, string trainNo, short stn, short? bound, DateTime time, string trainNoAlias, string platform)
        {
            int result = -1;


            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    string cmdText = @"SELECT ID FROM TrainMovement WHERE EMUNo = @emuNo and Platform=@platform
                                    AND LastSignalTime > @timeLimit";
                    SqlCommand cmd = new SqlCommand(cmdText, conn, t);
                    int ID = -1;
                    cmd.Parameters.Add(new SqlParameter("@emuNo", emuNo));
                    cmd.Parameters.Add(new SqlParameter("@platform", platform));
                    cmd.Parameters.Add(new SqlParameter("@timeLimit", time.AddMinutes(-5)));
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        ID = reader.GetInt32(0);
                    }
                    reader.Close();

                    if (ID == -1)
                    {
                        cmd = new SqlCommand("", conn, t);
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
                            cmd.Parameters.Add(new SqlParameter("@bound", bound.Value));
                        }

                        headSql += "[LastSignalTime],";
                        valueSql += "@time,";
                        cmd.Parameters.Add(new SqlParameter("@time", time));

                        headSql += "[TrainNoAlias],";
                        valueSql += "@trainNoAlias,";
                        cmd.Parameters.Add(new SqlParameter("@trainNoAlias", trainNoAlias));

                        headSql += "[Platform],";
                        valueSql += "@platform,";
                        cmd.Parameters.Add(new SqlParameter("@platform", platform));

                        headSql += "[TrainNo]) ";
                        valueSql += "@trainNo) ";
                        cmd.Parameters.Add(new SqlParameter("@trainNo", trainNo));

                        cmd.CommandText = headSql + valueSql + " SELECT SCOPE_IDENTITY()";

                        SqlDataReader r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            result = int.Parse(r[0].ToString());
                        }
                        r.Close();
                    }
                    else
                    {
                        cmdText = @"Update TrainMovement set lastsignaltime=@time where ID=@id";
                        cmd = new SqlCommand(cmdText, conn, t);
                        cmd.Parameters.Add(new SqlParameter("@time", time));
                        cmd.Parameters.Add(new SqlParameter("@id", ID));
                        cmd.ExecuteNonQuery();
                    }


                    t.Commit();
                }
                conn.Close();
            }
            

            return result;
        }

        /// <summary>
        /// Check whether the EMU No exists in the table
        /// </summary>
        /// <param name="EMUNo"></param>
        /// <returns></returns>
        public bool IsEMUNoExisting(string EMUNo)
        {

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("select count(*) from TrainMovement where EMUNo=@EMUNo", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@EMUNo", EMUNo);
                    int count = (int)cmd.ExecuteScalar();
                    conn.Close();
                    if (count >= 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                conn.Close();
            }
        }

        ///// <summary>
        ///// Delete a particular record
        ///// </summary>
        ///// <param name="id">TrainMovement Id</param>
        //public void DeleteTrainMovement(int id)
        //{
        //    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
        //    {
        //        conn.Open();
        //        string query = @"DELETE FROM TrainMovement WHERE Id = @Id";

        //        SqlCommand cmd = new SqlCommand(query, conn);

        //        SqlParameter p1 = new SqlParameter("Id", id);
        //        cmd.Parameters.Add(p1);

        //        cmd.ExecuteNonQuery();

        //        conn.Close();
        //    }
        //}

        /// <summary>
        /// Delete a particular record
        /// </summary>
        /// <param name="emuNo"></param>
        public void DeleteTrainMovement(string emuNo)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string query = @"DELETE FROM TrainMovement WHERE EMUNo = @emuNo";

                SqlCommand cmd = new SqlCommand(query, conn);

                SqlParameter p1 = new SqlParameter("emuNo", emuNo);
                cmd.Parameters.Add(p1);

                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        /// <summary>
        /// Get the stn id of the last stn that the train passed.
        /// </summary>
        /// <param name="emuNo"></param>
        /// <returns></returns>
        public short? GetLastStnByEMUNo(string emuNo)
        {
            short? stn = null;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string query = @"SELECT LastStn FROM TrainMovement WHERE emuNo = @emuNo order by lastSignalTime desc";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@emuNo", emuNo);

                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    stn = r.GetInt16(0);
                    break;
                }
                r.Close();

                conn.Close();
            }

            return stn;
        }


        public TrainMovement GetLastTrainMovement(string emuNo, string platform)
        {
            TrainMovement record = null;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand(@"SELECT ID, trainNo, lastStn, bound, lastSignalTime, trainNoAlias, platform FROM TrainMovement WHERE EMUNO = @emuNo
                                                        AND platform <> @platform order by lastSignalTime desc ", conn))
                {
                    conn.Open();
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
                            record.LastStn = r.GetInt16(2);
                        if (!r.IsDBNull(3))
                            record.Bound = r.GetInt16(3);
                        if (!r.IsDBNull(4))
                            record.LastSignalTime = r.GetDateTime(4);

                        break;
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return record;
        }
        #endregion

        #region Latest station to station travel time

        /// <summary>
        /// Get the latest travel time between give stations from the database
        /// </summary>
        /// <param name="stn1"></param>
        /// <param name="stn2"></param>
        /// <returns></returns>
        public int GetLatestTravelTime(SqlCommand cmd, string plat1, string plat2, int historicalTime)
        {
            int time = -1;

//            cmd.CommandText = @"SELECT traveltime FROM StnToStnTravelTimeToday 
//                           WHERE fromPlat=@plat1 and toPlat=@plat2 order by arrivalTime desc";

//            List<int> listTime = new List<int>();

//            if (historicalTime != -1)
//                listTime.Add(historicalTime);

//            cmd.Parameters.AddWithValue("@plat1", plat1);
//            cmd.Parameters.AddWithValue("@plat2", plat2);
//            SqlDataReader r = cmd.ExecuteReader();
//            int total = 0;
//            while (r.Read())
//            {
//                listTime.Add( Convert.ToInt16(r["traveltime"]));
//                total++;

//                if (total == 4)
//                    break;
//            }
//            r.Close();

//            if (listTime.Count > 1 && listTime[listTime.Count - 1] > 600)
//            {
//                listTime.RemoveAt(listTime.Count - 1);
//            }

//            if (listTime.Count > 0)
//            {
//                int sum = 0;
//                foreach (int t in listTime)
//                    sum += t;
//                time = (int) ((double)sum / listTime.Count);
//            }

            cmd.CommandText = @"SELECT traveltime FROM StnToStnTravelTimeToday 
                           WHERE fromPlat=@plat1 and toPlat=@plat2 order by arrivalTime desc";

            List<int> listTime = new List<int>();

            cmd.Parameters.AddWithValue("@plat1", plat1);
            cmd.Parameters.AddWithValue("@plat2", plat2);
            SqlDataReader r = cmd.ExecuteReader();
            int total = 0;
            while (r.Read())
            {
                listTime.Add(Convert.ToInt16(r["traveltime"]));
                total++;

                if (total == 1)
                    break;
            }
            r.Close();

            if (listTime.Count >= 1 && listTime[listTime.Count - 1] > 600)
            {
                listTime.RemoveAt(listTime.Count - 1);
            }

            if (listTime.Count > 0)
            {
                int sum = 0;
                foreach (int t in listTime)
                    sum += t;
                time = (int)((double)sum / listTime.Count);
            }


            if (time == -1)
                time = historicalTime;

            
            return time;
        }

        /// <summary>
        /// Add StnToStnTravelTimeToday record into database
        /// </summary>
        /// <param name="stn1"></param>
        /// <param name="stn2"></param>
        /// <param name="traveltime"></param>
        /// <returns></returns>
        public int AddStnToStnTravelTimeToday(string plat1, string plat2, int travelTime, DateTime arrivalTime)
        {
            int result = -1;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    string query = @"SELECT ID FROM StnToStnTravelTimeToday where fromPlat = @plat1 and toPlat=@plat2
                    and arrivalTime > @timeLimit ";
                    SqlCommand cmd = new SqlCommand(query, conn, t);
                    cmd.Parameters.AddWithValue("plat1", plat1);
                    cmd.Parameters.AddWithValue("plat2", plat2);
                    cmd.Parameters.AddWithValue("timeLimit", arrivalTime.AddMinutes(-1));
                    int ID = -1;
                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        ID = r.GetInt32(0);
                    }
                    r.Close();

                    if (ID == -1)
                    {
                        //// Insert into Station table and get Station id
                        query = @"INSERT INTO StnToStnTravelTimeToday(fromPlat, toPlat, traveltime, arrivalTime, [datetype])
                                    VALUES(@plat1,@plat2,@travelTime, @arrivalTime, @datetype) SELECT SCOPE_IDENTITY()";
                        cmd = new SqlCommand(query, conn, t);

                        if (ForecastManagementBL.dateType == 0)
                        {
                            ForecastManagementBL.dateType = (new UtilityDL()).GetTimeTableDateTypeID(DateTime.Now.ToString("yyyy-MM-dd"));
                        }

                        cmd.Parameters.AddWithValue("plat1", plat1);
                        cmd.Parameters.AddWithValue("plat2", plat2);
                        cmd.Parameters.AddWithValue("travelTime", travelTime);
                        cmd.Parameters.AddWithValue("arrivalTime", arrivalTime);
                        cmd.Parameters.AddWithValue("datetype", ForecastManagementBL.dateType);

                        r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            result = 1;
                        }
                        r.Close();
                    }
                    else
                    {
                        query = @"Update StnToStnTravelTimeToday set traveltime = @travelTime
                                    where ID=@ID";
                        cmd = new SqlCommand(query, conn, t);

                        cmd.Parameters.AddWithValue("travelTime", travelTime);
                        cmd.Parameters.AddWithValue("ID", ID);

                        cmd.ExecuteNonQuery();
                    }

                    t.Commit();
                }
                conn.Close();
            }

            return result;
        }
        #endregion

        #region Manage forecasted arrival time at stations
        /// <summary>
        /// Add an arrival time forecast by ATSS data into the database
        /// </summary>
        /// <param name="emuNo"></param>
        /// <param name="trainNo"></param>
        /// <param name="stn"></param>
        /// <param name="bound"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public int AddForecastedArrivalTime(SqlCommand cmd, string emuNo, string trainNo, string trainNoAlias, short stn, short bound, DateTime time, long? plannedST, bool isService, string platform)
        {
            int result = -1;

            string headSql = "INSERT INTO DailyForecastedArrivalTime(";
            string valueSql = " Values (";

            if (emuNo != null)
            {
                headSql += "[EMUNo],";
                valueSql += "@emuNo,";
                cmd.Parameters.Add(new SqlParameter("@emuNo", emuNo));
            }

            headSql += "[Bound],";
            valueSql += "@bound,";
            cmd.Parameters.Add(new SqlParameter("@bound", bound));

            headSql += "[isService],";
            valueSql += "@isService,";
            cmd.Parameters.Add(new SqlParameter("@isService", isService));
                    

            headSql += "[Time],";
            valueSql += "@time,";
            cmd.Parameters.Add(new SqlParameter("@time", time));


            if (trainNo != null)
            {
                headSql += "[TrainNo], ";
                valueSql += "@trainNo, ";
                cmd.Parameters.Add(new SqlParameter("@trainNo", trainNo));
            }

            if (trainNoAlias != null)
            {
                headSql += "[trainNoAlias], ";
                valueSql += "@trainNoAlias, ";
                cmd.Parameters.Add(new SqlParameter("@trainNoAlias", trainNoAlias));
            }

            if (plannedST != null)
            {
                headSql += "[PlannedStationTime], ";
                valueSql += "@plannedST, ";
                cmd.Parameters.Add(new SqlParameter("@plannedST", plannedST));
            }

            if (platform != null)
            {
                headSql += "[Platform], ";
                valueSql += "@platform, ";
                cmd.Parameters.Add(new SqlParameter("@platform", platform));
            }

            headSql += "[Stn])";
            valueSql += "@stn)";
            cmd.Parameters.Add(new SqlParameter("@stn", stn));

            cmd.CommandText = headSql + valueSql + " SELECT SCOPE_IDENTITY()";

            SqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
            {
                result = int.Parse(r[0].ToString());
            }
            r.Close();

            
            return result;
        }

        

        /// <summary>
        /// Update the forecasted arrival time of a train based on a planned station time.
        /// </summary>
        /// <param name="plannedSTId"></param>
        /// <param name="emuNo"></param>
        public void UpdateForecastByEMU(string emuNo, string trainNo, DateTime pastTime, DateTime currTime, short currStn, short bound)
        {
//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
//            {
//                conn.Open();

//                using (SqlTransaction t = conn.BeginTransaction())
//                {
//                    #region Get List of planned station time after given planned station time
//                    List<DailyForecastedArrivalTime> list = new List<DailyForecastedArrivalTime>();
//                    try
//                    {
//                        string query = @"select dt.trainNo, dt.[time], dt.stn, dt.bound, dt.[ID], stn.code, [platform] from [DailyForecastedArrivalTime]  dt
//                                inner join station stn on dt.stn = stn.id
//                                where emuNo = @emuNo and [time] > @time";

//                        SqlCommand cmd = new SqlCommand(query, conn, t);

//                        cmd.Parameters.AddWithValue("emuNo", emuNo);
//                        cmd.Parameters.AddWithValue("time", pastTime);

//                        using (SqlDataReader r = cmd.ExecuteReader())
//                        {
//                            while (r.Read())
//                            {
//                                DailyForecastedArrivalTime curr = new DailyForecastedArrivalTime();
//                                curr.TrainNo = r.GetString(0);
//                                curr.Time = r.GetDateTime(1);
//                                curr.EMUNo = emuNo;
//                                if (!r.IsDBNull(2))
//                                    curr.Stn = r.GetInt16(2);
//                                if (!r.IsDBNull(3))
//                                    curr.Bound = r.GetInt16(3);
//                                curr.ID = r.GetInt32(4);
//                                if (!r.IsDBNull(5))
//                                    curr.StnCode = r.GetString(5);
//                                if (!r.IsDBNull(6))
//                                    curr.Platform = r.GetString(6);
//                                list.Add(curr);
//                            }
//                            r.Close();
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        int n = 0;
//                    }
//                    #endregion

//                    #region If there is such list, Iterate through the list and update the forecasted arrival time
//                    if (list.Count > 1)
//                    {
//                        list[0].Time = currTime;
                        
//                        for (int i = 0; i < list.Count - 1; i++)
//                        {
//                            DailyForecastedArrivalTime currST = list[i];
//                            DailyForecastedArrivalTime nextST = list[i + 1];

//                            int hisTime = GetAvgTravelTime(new SqlCommand("", conn, t), currST.Platform, nextST.Platform, currST.Time, ForecastManagementBL.dateType);

//                            int travelTime = GetLatestTravelTime(new SqlCommand("", conn, t), currST.Platform, nextST.Platform, hisTime);

//                            nextST.Time = currST.Time.AddSeconds(travelTime);

//                            DeleteForecastedTimeByEMUandStn(new SqlCommand("", conn, t), emuNo, nextST.Stn, bound);
//                     //       DeleteForecastedTimeByTime(new SqlCommand("", conn, t), nextST.Time, nextST.Stn, bound);
//                            int result = AddForecastedArrivalTime(new SqlCommand("", conn, t), emuNo, nextST.TrainNo, nextST.TrainNo, nextST.Stn, bound, nextST.Time, null, true, nextST.Platform);
//                            QueueForecastForSending(new SqlCommand("", conn, t), result);
                            
//                        }
                        
//                    }
//                    #endregion

//                    #region Otherwise, need to forecast for two bounds
//                    else
//                    {
////                        List<Station> listStn = new List<Station>();
////                        List<short> listBound = new List<short>();

////                        try
////                        {
////                            string query3 = @"select bs.StationID, stn.Code, bs.boundid from boundstation bs
////                                            inner join station stn on bs.StationID=stn.id
////                                            where boundid=@bound and position >= (select position from boundstation where StationID=@stn and boundid=@bound)";
////                            SqlCommand cmd3 = new SqlCommand(query3, conn, t);

////                            cmd3.Parameters.AddWithValue("bound", bound);
////                            cmd3.Parameters.AddWithValue("stn", currStn);

////                            SqlDataReader r3 = cmd3.ExecuteReader();
////                            while (r3.Read())
////                            {
////                                Station station = new Station();
////                                station.ID = r3.GetInt16(0);
////                                station.Code = r3.GetString(1);
////                                listStn.Add(station);
////                                listBound.Add(r3.GetInt16(2));
////                            }
////                            r3.Close();
////                        }
////                        catch (Exception ex)
////                        {
////                            int n = 0;
////                        }

////                        try
////                        {
////                            string query4 = @"select bs.StationID, stn.Code,bs.boundid from boundstation bs
////                                            inner join station stn on bs.StationID=stn.id
////                                            where boundid=(select BoundID from BoundStation where stationid=@station and Position=1)
////                                            order by bs.Position";
////                            SqlCommand cmd4 = new SqlCommand(query4, conn, t);

////                            cmd4.Parameters.AddWithValue("station", listStn[listStn.Count - 1].ID);
////                            listStn.RemoveAt(listStn.Count - 1);
////                            SqlDataReader r4 = cmd4.ExecuteReader();
////                            r4.Read();
////                            while (r4.Read())
////                            {
////                                Station station = new Station();
////                                station.ID = r4.GetInt16(0);
////                                station.Code = r4.GetString(1);
////                                listStn.Add(station);
////                                listBound.Add(r4.GetInt16(2));
////                            }
////                            r4.Close();
////                        }
////                        catch (Exception ex)
////                        {
////                            int n = 0;
////                        }

////                        DateTime time = currTime;
////                        for (int i = 0; i < listStn.Count - 1; i++)
////                        {
////                            Station station1 = listStn[i];
////                            Station station2 = listStn[i + 1];

////                            int hisTime = GetAvgTravelTime(new SqlCommand("", conn, t), station1.Code, station2.Code, time, DataManager.Instance().DateType);
////                            int travelTime = GetLatestTravelTime(new SqlCommand("", conn, t), station1.Code, station2.Code,hisTime);

////                            if (travelTime == -1)   //end of current service at currST
////                            {
////                                break;
////                            }

////                            time = time.AddSeconds(travelTime);

////                            //DeleteForecastedTimeByTime(new SqlCommand("", conn, t), time, station2.ID, listBound[i+1]);
////                            AddForecastedArrivalTime(new SqlCommand("", conn, t), emuNo, trainNo, trainNo, station2.ID, listBound[i + 1], time, null, true);
////                        }

//                    }
//                    #endregion
//                    t.Commit();
//                }
//                conn.Close();
//            }
        }

        /// <summary>
        /// Update the forecasted arrival time of a train based on a planned station time.
        /// </summary>
        /// <param name="plannedSTId"></param>
        /// <param name="emuNo"></param>
        public void UpdateForecastByPlannedST(long plannedSTId, string emuNo, DateTime currTime)
        {
            DailyTrainStationTime ST = (new OperationManagementDL()).GetDailyTrainStationTime(plannedSTId);



            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    DeleteForecastedTimeByEMU(new SqlCommand("", conn, t), emuNo);

                    #region Get List of planned station time after given planned station time
                    List<DailyTrainStationTime> list = new List<DailyTrainStationTime>();
                    string query = @"select dst.TrainNo, dst.BoundID, pt.StationID, dst.[time], dst.[ID], stn.code, pt.[code] from [dbo].[DailyTrainStationTime] dst
                                   inner join [platform] pt on dst.PlatformID=pt.id 
                                   inner join systemlookup s on dst.[type] = s.id
                                    inner join station stn on pt.stationid=stn.id
                                    inner join trainline tl on dst.lineid = tl.id
                                   where (tl.code = 'EWL' or tl.code='NSL') and  s.[value]='Arrival' and dst.TrainNo = @trainNo and dst.[Time] > @time 
                                   order by dst.[Time]";

                    SqlCommand cmd = new SqlCommand(query, conn, t);

                    cmd.Parameters.AddWithValue("trainNo", ST.TrainNo);
                    cmd.Parameters.AddWithValue("time", ST.Time);

                    list.Add(ST);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        DailyTrainStationTime curr = new DailyTrainStationTime();
                        curr.TrainNo = r.GetString(0);
                        curr.BoundId = r.GetInt16(1);
                        curr.StationID = r.GetInt16(2);
                        curr.Time = r.GetDateTime(3);
                        curr.ID = r.GetInt64(4);
                        curr.Station = r.GetString(5);
                        curr.Platform = r.GetString(6);
                        if (list.Count > 0 && curr.StationID == list[list.Count - 1].StationID)
                            continue;
                        list.Add(curr);
                    }

                    r.Close();

                    //    Logging.log.Debug("List count  IS " + list.Count);
                    #endregion

                    #region Iterate through the list of planned station time for two bounds and update the forecasted arrival time
                    string startStn = ST.Station;
                    string secondStn = "";
                    if (list.Count > 1)
                        secondStn = list[1].Station;
                    int startStnVisited = 0;
                    int secondStnVisited = 0; //to handle the case when the starting stn is a terminal

                    if (list.Count > 0)
                        list[0].Time = currTime;

                    int maxerror = 0, minError = 100000, totalerror = 0, count = 0, currIndex = 0;


                    List<DailyActualStationTime> listActualST = GetListofActualArrival(new SqlCommand("", conn, t), emuNo, currTime);
                    //        bool hasReachedTerminal = false;
                    //    Logging.log.Debug("listActualST count  IS " + listActualST.Count);
                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        DailyTrainStationTime currST = list[i];
                        DailyTrainStationTime nextST = list[i + 1];



                        DailyActualStationTime actualST = null;
                        for (int k = currIndex; k < listActualST.Count; k++)
                        {
                            if (listActualST[k].Platform.Substring(0, 3) == nextST.Platform.Substring(0, 3))
                            {
                                TimeSpan span = listActualST[k].Time - nextST.Time;
                                int diff = (int)Math.Abs(span.TotalSeconds);
                                if (diff > 1800)
                                    break;
                                actualST = listActualST[k];
                                currIndex = k;
                                break;
                            }
                        }


                        TimeSpan timespan = nextST.Time - currST.Time;
                        if (timespan.TotalHours > 1)   //split duty case
                            break;

                        if (nextST.Station == startStn)
                        {
                            startStnVisited++;
                            if (startStnVisited == 2)
                                break;
                        }

                        if (nextST.Station == secondStn)
                        {
                            secondStnVisited++;
                            if (secondStnVisited == 3)
                                break;
                        }

                        int hisTime = GetAvgTravelTime(new SqlCommand("", conn, t), currST.Platform, nextST.Platform, currST.Time, ForecastManagementBL.dateType);

                        if (hisTime == -1)
                            hisTime = GetAvgTravelTime(new SqlCommand("", conn, t), currST.Platform, nextST.Platform, currST.Time.AddMinutes(30), ForecastManagementBL.dateType);

                        int travelTime;

                        if (i < 9)
                            travelTime = GetLatestTravelTime(new SqlCommand("", conn, t), currST.Platform, nextST.Platform, hisTime);
                        else
                            travelTime = hisTime;

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


                        DateTime plannedTime = nextST.Time;
                        nextST.Time = currST.Time.AddSeconds(travelTime);




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
                        DeleteForecastedTimeByPlannedST(new SqlCommand("", conn, t), nextST.ID);
                        //DeleteForecastedTimeByTime(new SqlCommand("", conn, t), nextST.Time, nextST.StationID, nextST.BoundId);
                        int result = AddForecastedArrivalTime(new SqlCommand("", conn, t), emuNo, nextST.TrainNo, nextST.TrainNo, nextST.StationID, nextST.BoundId, nextST.Time, nextST.ID, true, nextST.Platform);

                  //      QueueForecastForSending(new SqlCommand("", conn, t), result);

                        if (actualST != null)
                        {
                            DateTime actualTime = actualST.Time;

                            if (actualTime == null)
                                continue;

                            TimeSpan forecastTS = nextST.Time - actualTime;
                            int forecastError = (int)Math.Abs(forecastTS.TotalSeconds);

                            TimeSpan actualTS = plannedTime - actualTime;
                            int actualError = (int)Math.Abs(actualTS.TotalSeconds);

                            AddForecastError(new SqlCommand("", conn, t), emuNo, ST.TrainNo, nextST.Station, currTime, forecastError, actualError, nextST.Time, actualTime, plannedTime);
                        }
                    }


                    //if (count > 0)
                    //{
                    //    int avgError = (int)((double)totalerror) / count;

                    //    AddForecastError(new SqlCommand("", conn, t), emuNo, ST.TrainNo, ST.Station, currTime, maxerror, minError, avgError);
                    //}

                    #endregion
                    t.Commit();
                }
                conn.Close();
            }
        }

        public void AddForecastError(SqlCommand cmd, string emuNo, string trainNo, string stn, DateTime time, int forecastError, int actualError, DateTime ForecastTime, DateTime ActualTime, DateTime PlannedTime)
        {
            string query = @"insert into [ForecastError] (EMUNo, TrainNo, Stn, [Time], ForecastError, ActualError, ForecastTime, ActualTime, PlannedTime)
                            values (@emuNo, @trainNo, @stn, @time,@forecastError,@actualError, @ForecastTime, @ActualTime,@PlannedTime) ";
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("emuNo", emuNo);
            cmd.Parameters.AddWithValue("trainNo", trainNo);
            cmd.Parameters.AddWithValue("stn", stn);
            cmd.Parameters.AddWithValue("time", time);
            cmd.Parameters.AddWithValue("forecastError", forecastError);
            cmd.Parameters.AddWithValue("actualError", actualError);
            cmd.Parameters.AddWithValue("ForecastTime", ForecastTime);
            cmd.Parameters.AddWithValue("ActualTime", ActualTime);
            cmd.Parameters.AddWithValue("PlannedTime", PlannedTime);

            cmd.ExecuteNonQuery();
        }

        public int GetForecastError(SqlCommand cmd, string emuNo, string stn, DateTime forecastedTime)
        {
            int error = -1;

            DateTime startTime = forecastedTime.AddSeconds(-1800);
            DateTime endTime = forecastedTime.AddSeconds(1800);

            string query = @"select abs(DATEDIFF(second, convert(time,[time]), convert(time,@forecastedTime))), trainno from [DailyActualStationTime]
                                where EMUNo=@emuNo and SUBSTRING([platform],1,3)=@stn
                                and convert(time,[time]) >convert(time,@startTime) and convert(time,[time]) < convert(time,@endTime) and [type]='ARR'
                            order by abs(DATEDIFF(second, convert(time,[time]),  convert(time,@forecastedTime)))";

            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("emuNo", emuNo);
            cmd.Parameters.AddWithValue("stn", stn);
            cmd.Parameters.AddWithValue("startTime", startTime);
            cmd.Parameters.AddWithValue("endTime", endTime);
            cmd.Parameters.AddWithValue("forecastedTime", forecastedTime);

            SqlDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                error = r.GetInt32(0);
                string trainno = r.GetString(1);
                if (trainno.Substring(0, 1) == "9" || trainno.Substring(0, 1) == "8")
                    error = -2;
                break;
            }
            r.Close();
            return error;
        }

        public List<DailyActualStationTime> GetListofActualArrival(SqlCommand cmd, string emuNo, DateTime time)
        {
            List<DailyActualStationTime> result = new List<DailyActualStationTime>();


            string query = @"select [time], [platform] from [DailyActualStationTime]
                                where EMUNo=@emuNo and [time] > @time and [type]='ARR'
                            order by [time]";

            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("emuNo", emuNo);
            cmd.Parameters.AddWithValue("time", time);

             SqlDataReader r = cmd.ExecuteReader();

             string prevPlat = "";

             while (r.Read())
             {
                 DailyActualStationTime st = new DailyActualStationTime();
                 st.Time = r.GetDateTime(0);
                 st.Platform = r.GetString(1);
                 if (st.Platform == prevPlat)
                     continue;
                 else
                     prevPlat = st.Platform;
                 result.Add(st);
             }

             r.Close();
            return result;
        }

        public DateTime? GetActualArrivalTime(SqlCommand cmd, string emuNo, string stn, DateTime forecastedTime)
        {
            DateTime? time = null;

            DateTime startTime = forecastedTime.AddSeconds(-1800);
            DateTime endTime = forecastedTime.AddSeconds(1800);

            string query = @"select [time], trainno from [DailyActualStationTime]
                                where EMUNo=@emuNo and SUBSTRING([platform],1,3)=@stn
                                and convert(time,[time]) >convert(time,@startTime) and convert(time,[time]) < convert(time,@endTime) and [type]='ARR'
                            order by abs(DATEDIFF(second, convert(time,[time]),  convert(time,@forecastedTime)))";

            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("emuNo", emuNo);
            cmd.Parameters.AddWithValue("stn", stn);
            cmd.Parameters.AddWithValue("startTime", startTime);
            cmd.Parameters.AddWithValue("endTime", endTime);
            cmd.Parameters.AddWithValue("forecastedTime", forecastedTime);

            SqlDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                string trainno = r.GetString(1);

                if (trainno.Substring(0, 1) == "9" || trainno.Substring(0, 1) == "8")
                    break;

                time = r.GetDateTime(0);
                
                
                break;
            }
            r.Close();
            return time;
        }

        /// <summary>
        /// Delete a forecast time for a given EMU no at a station along a given bound
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="emuNo"></param>
        /// <param name="stn"></param>
        /// <param name="bound"></param>
        public void DeleteForecastedTimeByEMUandStn(SqlCommand cmd, string emuNo, short stn, short bound)
        {
            try
            {
                string query = @"DELETE FROM DailyForecastedArrivalTime WHERE EmuNo = @emuNo and Stn = @stn and bound = @bound";

                cmd.CommandText = query;

                SqlParameter p1 = new SqlParameter("emuNo", emuNo);
                cmd.Parameters.Add(p1);

                SqlParameter p2 = new SqlParameter("stn", stn);
                cmd.Parameters.Add(p2);

                SqlParameter p3 = new SqlParameter("bound", bound);
                cmd.Parameters.Add(p3);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                string message = "Error in deleting old forecast arrival time for " + emuNo + " at " + stn + " along bound " + bound;
                Logging.log.Error(message);
                throw new ApplicationException(message);
            }
        }

        /// <summary>
        /// Delete a forecast time for a given EMU
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="emuNo"></param>
        public void DeleteForecastedTimeByEMU(string emuNo)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"DELETE FROM DailyForecastedArrivalTime WHERE EmuNo = @emuNo", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@emuNo", emuNo);

                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }

        /// <summary>
        /// Delete a forecast time for a given EMU
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="emuNo"></param>
        public void DeleteForecastedTimeByEMU(SqlCommand cmd, string emuNo)
        {
            cmd.CommandText = @"DELETE FROM DailyForecastedArrivalTime WHERE EmuNo = @emuNo";
            cmd.Parameters.AddWithValue("@emuNo", emuNo);

            cmd.ExecuteNonQuery();

        }

        /// <summary>
        /// Delete the forecasted time based on a given planned station time
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="plannedST"></param>
        public void DeleteForecastedTimeByPlannedST(SqlCommand cmd, long plannedST)
        {
            string query = @"DELETE FROM DailyForecastedArrivalTime WHERE PlannedStationTime = @plannedST";

            cmd.CommandText = query;

            SqlParameter p1 = new SqlParameter("plannedST", plannedST);
            cmd.Parameters.Add(p1);

            cmd.ExecuteNonQuery();
        }

        ///// <summary>
        ///// Delete the forecasted time based on a given planned station time
        ///// </summary>
        ///// <param name="cmd"></param>
        ///// <param name="plannedST"></param>
        //public void DeleteForecastedTimeByTime(SqlCommand cmd, DateTime time, short stn, short bound)
        //{
        //    string query = @"DELETE FROM DailyForecastedArrivalTime WHERE [Time] <= @time and Stn=@stn and Bound = @bound";

        //    cmd.CommandText = query;

        //    SqlParameter p1 = new SqlParameter("time", time);
        //    cmd.Parameters.Add(p1);

        //    SqlParameter p2 = new SqlParameter("stn", stn);
        //    cmd.Parameters.Add(p2);

        //    SqlParameter p3 = new SqlParameter("bound", bound);
        //    cmd.Parameters.Add(p3);


        //    cmd.ExecuteNonQuery();
        //}

        
        #endregion

        #region Initiate forecast 
        /// <summary>
        /// Use the planned station time from TT to forecast the arrival time at stations
        /// </summary>
        public void InitiateForecast()
        {
            Logging.log.Debug("start initial forecasting");
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    #region clear old data
                    //string queryClear = "truncate table [DailyForecastedArrivalTime]";
                    //SqlCommand cmd = new SqlCommand(queryClear, conn, t);
                    //cmd.ExecuteNonQuery();

                    string queryClear = "truncate table [TrainNoToBound]";
                    SqlCommand cmd = new SqlCommand(queryClear, conn, t);
                    cmd.ExecuteNonQuery();

                    queryClear = "truncate table [TrainMovement]";
                    cmd = new SqlCommand(queryClear, conn, t);
                    cmd.ExecuteNonQuery();
                    #endregion


                    #region copy data from dailytrainstationtime to DailyForecastedArrivalTime as initial forecast
                    string queryInsert = @"insert into [dbo].[DailyForecastedArrivalTime] (TrainNo, [Time], plannedstationtime, stn, bnd, isservice, [platform])
                                            select st.trainno, st.[time], st.[id], st.stn, st.bnd, 1, st.plt from dailytrainstationtime st 
                                            where st.type='ARR' 
                                            order by st.trainno, st.[time]";
                    cmd = new SqlCommand(queryInsert, conn, t);

                    cmd.ExecuteNonQuery();
                    #endregion


                    t.Commit();
                }
                conn.Close();
            }
            Logging.log.Debug("Finish initial forecasting");
        }

        /// <summary>
        /// Use the travel time in TT as the initial data of the travel time between stations
        /// </summary>
        public void InitiateTravelTime()
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    #region clear old data
                    string queryClear = "truncate table [StnToStnTravelTimeToday]";
                    SqlCommand cmd = new SqlCommand(queryClear, conn, t);
                    cmd.ExecuteNonQuery();
                    #endregion


                    #region use travel time in planned TT as the initial data of the travel time between stations
                    DateTime currDate = DataManager.Instance().OperationDate();
                    string queryInsert = @"insert into [StnToStnTravelTimeToday]
                                            select code1, code2, dur, @currentDate, @dateType from
                                            (select *,row_number() over (partition by combinedCode order by dur) rn from
                                            (select tt1.TrainNo as num,tt1.[platform]+tt2.[platform] as combinedCode,tt1.[platform] as code1, tt2.[platform] as code2, 
                                            Datediff(second,tt1.[time],tt2.[time]) as dur, tt1.[time] as thisTime from [DailyForecastedArrivalTime] tt1 
                                            inner join [DailyForecastedArrivalTime] tt2 on tt2.id=tt1.id+1 and tt2.TrainNo=tt1.TrainNo) temp) temp2
                                            where rn=1";
                    cmd = new SqlCommand(queryInsert, conn, t);
                    cmd.Parameters.AddWithValue("currentDate", currDate);
                    cmd.Parameters.AddWithValue("dateType", ForecastManagementBL.dateType);

                    cmd.ExecuteNonQuery();
                    #endregion


                    t.Commit();
                }
                conn.Close();
            }

        }

        /// <summary>
        /// Use the travel time in TT as the initial data of the travel time between stations
        /// </summary>
        public void InitiateTravelTimeWithTT()
        {
            Logging.log.Debug("Start calculating intial travel time");
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    try
                    {
                        #region clear old data
                        string queryClear = "truncate table [lastavgtraveltime]";
                        SqlCommand cmd = new SqlCommand(queryClear, conn, t);
                        cmd.ExecuteNonQuery();
                        #endregion


                        #region use travel time in planned TT as the initial data of the travel time between stations
                        DateTime currDate = DataManager.Instance().OperationDate();
                        string queryInsert = @"insert into [lastavgtraveltime]
                                            select code1, code2, dur from
                                            (select *,row_number() over (partition by combinedCode order by dur) rn from
                                            (select tt1.TrainNo as num,tt1.plt+tt2.plt as combinedCode,tt1.plt as code1, tt2.plt as code2, 
                                            Datediff(second,tt1.[time],tt2.[time]) as dur, tt1.[time] as thisTime from DailyTrainStationTime tt1 
                                            inner join DailyTrainStationTime tt2 on tt2.id=tt1.id+2 and tt2.TrainNo=tt1.TrainNo) temp) temp2
                                            where rn=1";

//                        string queryInsert = @"insert into [lastavgtraveltime]
//                                            select code1, code2, dur from
//                                            (select *,row_number() over (partition by combinedCode order by dur) rn from
//                                            (select tt1.TrainNo as num,tt1.[platform]+tt2.[platform] as combinedCode,tt1.[platform] as code1, tt2.[platform] as code2, 
//                                            Datediff(second,tt1.[time],tt2.[time]) as dur, tt1.[time] as thisTime from [DailyForecastedArrivalTime] tt1 
//                                            inner join [DailyForecastedArrivalTime] tt2 on tt2.id=tt1.id+1 and tt2.TrainNo=tt1.TrainNo) temp) temp2
//                                            where rn=1";
                        cmd = new SqlCommand(queryInsert, conn, t);
                        cmd.Parameters.AddWithValue("currentDate", currDate);
                        cmd.Parameters.AddWithValue("dateType", ForecastManagementBL.dateType);

                        cmd.ExecuteNonQuery();
                        #endregion

//                        #region A special case, need to change "JURD" or "JURE" to "JURDE"
//                        string queryUpdate = @"update lastavgtraveltime set FromPLAT = 'JURDE' where FromPLAT='JURD'
//update lastavgtraveltime set ToPLAT = 'JURDE' where ToPLAT='JURD'
//update lastavgtraveltime set FromPLAT = 'JURDE' where FromPLAT='JURE'
//update lastavgtraveltime set ToPLAT = 'JURDE' where ToPLAT='JURE'";
//                        cmd = new SqlCommand(queryUpdate, conn, t);
//                        cmd.ExecuteNonQuery();
//                        #endregion

                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        conn.Close();
                        throw new ApplicationException("Error in initiating travel time with TT: " + ex.Message);
                    }
                }
                conn.Close();
            }
            Logging.log.Debug("Finish intial travel time calculation");
        }

        /// <summary>
        /// Remove the old transaction data in database on the mapping between train number and EMU number
        /// </summary>
        public void ClearTrainNoandEMUMapping()
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    #region clear old data
                    string queryClear = "truncate table [TrainNoEMUNoMapping]";
                    SqlCommand cmd = new SqlCommand(queryClear, conn, t);
                    cmd.ExecuteNonQuery();
                    #endregion


                    t.Commit();
                }
                conn.Close();
            }
        }

        /// <summary>
        /// Remove the old transaction data in database on the terminal movement
        /// </summary>
        public void ClearTerminalMovement()
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    #region clear old data
                    string queryClear = "truncate table [TerminalMovement]";
                    SqlCommand cmd = new SqlCommand(queryClear, conn, t);
                    cmd.ExecuteNonQuery();
                    #endregion


                    t.Commit();
                }
                conn.Close();
            }
        }
        #endregion

        #region Manage Train No To bound mapping
        /// <summary>
        /// Generate the train no to bound mapping based on TT
        /// </summary>
        public void GenerateTrainNoToBound()
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    #region add data
                    string query = @"insert into TrainNoToBound (TrainNo, Bound) select distinct dts.TrainNo,dts.bnd from DailyTrainStationTime dts
                                            order by dts.TrainNo";
                    SqlCommand cmd = new SqlCommand(query, conn, t);
                    cmd.ExecuteNonQuery();
                    #endregion


                    t.Commit();
                }
                conn.Close();
            }
        }

        /// <summary>
        /// Get the allowable deviation between planned station time and actual station time based on train number
        /// </summary>
        /// <param name="trainNo"></param>
        /// <returns></returns>
        public int GetAllowableDeviation(string trainNo)
        {
            int deviation = 2200;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                string query = @"select bound from trainnotobound where trainno=@trainNo and bound = 'EASTC'";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("trainNo",trainNo);
                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())
                {
                    deviation = 480;
                }
                r.Close();

                conn.Close();
            }

            return deviation;
        }
        #endregion

        #region Manage historical average travel time between stations
        public void CalculateAvgTravelTime(int dateType)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    #region clear old data
                    string query = @"DELETE FROM AvgStnToStnTravelTime WHERE [DateType]=@dateType";
                    SqlCommand cmd = new SqlCommand(query, conn, t);
                    cmd.Parameters.AddWithValue("dateType", dateType);
                    cmd.ExecuteNonQuery();
                    #endregion


                    #region Remove StnToStnTravelTime data older than 5 instances for this specific type
                    string queryR = @"DELETE FROM [StnToStnTravelTime] 
                        WHERE DateType = @dateType AND 
                        CONVERT(date,ArrivalTime) IN (
                        SELECT TravelDate FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY TravelDate DESC) AS Pos, TravelDate FROM
                        (
                        SELECT  DISTINCT CONVERT(date,ArrivalTime) AS TravelDate
                        FROM [dbo].[StnToStnTravelTime]
                        WHERE DateType=@dateType
                        ) AS TDates
                        ) AS NumberedDates
                        WHERE Pos > 5 ) 
                        ";
                    SqlCommand cmdR = new SqlCommand(queryR, conn, t);
                    cmdR.Parameters.AddWithValue("dateType", dateType);
                    cmdR.ExecuteNonQuery();
                    #endregion

                    DateTime fromTime = Convert.ToDateTime("2014-01-01 04:00:00");
                    DateTime endTime = Convert.ToDateTime("2014-01-02 03:30:00");

                    while (true)
                    {
                        DateTime toTime = fromTime.AddMinutes(30);
                        
                        string query2 = @"INSERT INTO AvgStnToStnTravelTime (fromPlat, toPlat, fromTime, toTime, [dateType], travelTime, deviation)
                                        select distinct fromPlat, toPlat, convert(time,@fromTime), convert(time,@toTime), @dateType, avg(traveltime) 
                                        OVER(PARTITION BY fromPlat, toPlat), stdev(traveltime)  OVER(PARTITION BY fromPlat, toPlat) 
                                            from StnToStnTravelTime 
                                            where  
                            convert(time,arrivalTime) >= convert(time,@fromTime) and 
                            convert(time,arrivalTime) < convert(time,@toTime) and
                        [datetype]=@dateType and travelTime < 800";
                        SqlCommand cmd2 = new SqlCommand(query2, conn, t);
                        cmd2.Parameters.AddWithValue("fromTime", fromTime);
                        cmd2.Parameters.AddWithValue("toTime", toTime);
                        cmd2.Parameters.AddWithValue("dateType", dateType);

                        cmd2.ExecuteNonQuery();
                        fromTime = toTime;
                        if (fromTime >= endTime)
                            break;
                    }

                    t.Commit();
                }
                conn.Close();
            }
        }

        /// <summary>
        /// Calcualte the historical average travel time by every 15 mins
        /// </summary>
        public void CalculateAvg15MinsTravelTime(DateTime OpsDate)
        {
            Logging.log.Debug("Start calculating average travel time");
            int dateType = (new UtilityDL()).GetTimeTableDateTypeID(OpsDate.ToString("yyyy-MM-dd"));
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    try
                    {
                        #region clear old data
                        string query = @"TRUNCATE TABLE AvgStnToStnTravelTime";
                        SqlCommand cmd = new SqlCommand(query, conn, t);
                    //    cmd.Parameters.AddWithValue("dateType", dateType);
                        cmd.ExecuteNonQuery();

                        query = @"TRUNCATE TABLE AvgStnToStnTravelTimeTemp";
                        cmd = new SqlCommand(query, conn, t);
                        cmd.ExecuteNonQuery();
                        #endregion


                        #region Remove StnToStnTravelTime data older than 3 instances for this specific type
                        string queryR = @"DELETE FROM [StnToStnTravelTime] 
                        WHERE dateType = @dateType and  
                        CONVERT(date,ArrivalTime) IN (
                        SELECT TravelDate FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY TravelDate DESC) AS Pos, TravelDate FROM
                        (
                        SELECT  DISTINCT CONVERT(date,ArrivalTime) AS TravelDate
                        FROM [dbo].[StnToStnTravelTime]
                        where datetype = @dateType
                        ) AS TDates
                        ) AS NumberedDates
                        WHERE Pos > 3 ) 
                        ";
                        SqlCommand cmdR = new SqlCommand(queryR, conn, t);
                        cmdR.Parameters.AddWithValue("dateType", dateType);
                        cmdR.ExecuteNonQuery();
                        #endregion

                        #region Obtain the average tavel time for every 15 mins over the last 3 days
                        query = @"SELECT TravelDate FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY TravelDate DESC) AS Pos, TravelDate FROM
                        (
                        SELECT  DISTINCT CONVERT(date,ArrivalTime) AS TravelDate
                        FROM [dbo].[StnToStnTravelTime]
                        where datetype = @dateType) as Tdates) as numbereddates";
                        cmd = new SqlCommand(query, conn, t);
                        cmd.Parameters.AddWithValue("dateType", dateType);
                        SqlDataReader r = cmd.ExecuteReader();
                        List<DateTime> pastDates = new List<DateTime>();
                        while (r.Read())
                        {
                            if (r.GetDateTime(0) == OpsDate.Date)
                                continue;
                            pastDates.Add(r.GetDateTime(0));
                        }
                        r.Close();

                        TimeSpan ts1 = new TimeSpan(4, 0, 0);
                        TimeSpan ts2 = new TimeSpan(3, 30, 0);                      
                        int size = pastDates.Count;
                        if (size > 3)
                            size = 3;

                        for (int i = 0; i < size; i++)
                        {
                            DateTime startTime = pastDates[i] + ts1;
                            DateTime fromTime = pastDates[i] + ts1;
                            DateTime endTime = pastDates[i].AddDays(1) + ts2;
                            while (true)
                            {
                                DateTime toTime = fromTime.AddMinutes(15);

                                string query2 = @"INSERT INTO AvgStnToStnTravelTimeTemp (fromPlat, toPlat, fromTime, toTime,  travelTime, [Date])
                               select distinct fromPlat, toPlat, convert(time,@fromTime), convert(time,@toTime), avg(traveltime) 
                                        OVER(PARTITION BY fromPlat, toPlat), @date
                                            from StnToStnTravelTime 
                                            where  
                            convert(time,arrivalTime) >= convert(time,@fromTime) and 
                            convert(time,arrivalTime) < convert(time,@toTime) and
                        arrivalTime > @start and arrivalTime < @end and travelTime < 800";
                                SqlCommand cmd2 = new SqlCommand(query2, conn, t);
                                cmd2.Parameters.AddWithValue("fromTime", fromTime);
                                cmd2.Parameters.AddWithValue("toTime", toTime);
                                cmd2.Parameters.AddWithValue("start", startTime);
                                cmd2.Parameters.AddWithValue("end", endTime);
                                cmd2.Parameters.AddWithValue("date", startTime.Date);
                       

                                cmd2.ExecuteNonQuery();
                                fromTime = toTime;
                                if (fromTime >= endTime)
                                    break;
                            }
                        }
                        #endregion

                        #region Now finalise the average time used as historical travel time for forecasting
                        query = @"select FromPlat, ToPlat, FromTime, ToTime, TravelTime from AvgStnToStnTravelTimeTemp
ORDER BY FromPlat, ToPlat, FromTime, Date";
                        cmd = new SqlCommand(query, conn, t);
                        r = cmd.ExecuteReader();
                        List<AvgTravelTime> listTime = new List<AvgTravelTime>();
                        while(r.Read())
                        {
                            AvgTravelTime time = new AvgTravelTime();
                            time.fromPlat = r.GetString(0);
                            time.toPlat = r.GetString(1);
                            time.fromTime = r.GetTimeSpan(2);
                            time.toTime = r.GetTimeSpan(3);
                            time.travelTime = r.GetInt32(4);
                            listTime.Add(time);
                        }
                        r.Close();

                        string prevFromPlat = "", prevToPlat = "";
                        TimeSpan prevFromTime = new TimeSpan(0, 0, 0), prevToTime = new TimeSpan(0, 0, 0);
                        List<int> times = new List<int>();
                        for (int i = 0; i < listTime.Count; i++)
                        {
                            AvgTravelTime currTime = listTime[i];
                            if (currTime.fromPlat != prevFromPlat || currTime.toPlat != prevToPlat || currTime.fromTime != prevFromTime)
                            {
                                if (times.Count > 0)
                                {
                                    int avgTime = 0;
                                    if(times.Count < 3)
                                        avgTime = (int)Math.Floor(Mean(times));
                                    else
                                        avgTime = (int)Math.Floor(Mean3WithMinVar(times));
                                    query = @"INSERT INTO AvgStnToStnTravelTime (fromPlat, toPlat, fromTime, toTime, travelTime)
                                        values (@fromPlat, @toPlat, @fromTime, @toTime, @travelTime)";
                                    cmd = new SqlCommand(query, conn, t);
                                    cmd.Parameters.AddWithValue("fromTime", prevFromTime);
                                    cmd.Parameters.AddWithValue("toTime", prevToTime);
                                    cmd.Parameters.AddWithValue("fromPlat", prevFromPlat);
                                    cmd.Parameters.AddWithValue("toPlat", prevToPlat);
                                    cmd.Parameters.AddWithValue("travelTime", avgTime);
                               //     cmd.Parameters.AddWithValue("dateType", dateType);
                                    cmd.ExecuteNonQuery();
                                    times = new List<int>();
                                }                   
                            }
                            times.Add(currTime.travelTime);
                            prevFromPlat = currTime.fromPlat;
                            prevToPlat = currTime.toPlat;
                            prevFromTime = currTime.fromTime;
                            prevToTime = currTime.toTime;
                        }

                        if (times.Count > 0)
                        {
                            int avgTime = 0;
                            if (times.Count < 3)
                                avgTime = (int)Math.Floor(Mean(times));
                            else
                                avgTime = (int)Math.Floor(Mean3WithMinVar(times));
                            query = @"INSERT INTO AvgStnToStnTravelTime (fromPlat, toPlat, fromTime, toTime, travelTime)
                                        values (@fromPlat, @toPlat, @fromTime, @toTime, @travelTime)";
                            cmd = new SqlCommand(query, conn, t);
                            cmd.Parameters.AddWithValue("fromTime", prevFromTime);
                            cmd.Parameters.AddWithValue("toTime", prevToTime);
                            cmd.Parameters.AddWithValue("fromPlat", prevFromPlat);
                            cmd.Parameters.AddWithValue("toPlat", prevToPlat);
                            cmd.Parameters.AddWithValue("travelTime", avgTime);
                      //      cmd.Parameters.AddWithValue("dateType", dateType);
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw new Exception("Unable to calculate historical average travel time: " + ex.Message);
                    }
                }
                conn.Close();
            }

            Logging.log.Debug("Finish calculating average travel time");
        }

        public int GetAvgTravelTime(SqlCommand cmd, string fromPlat, string toPlat, DateTime currTime, int dateType)
        {
            int travelTime = -1;

            string query = @"select travelTime from AvgStnToStnTravelTime where [dateType]=@dateType 
                            and fromPlat = @fromPlat and toPlat=@toPlat and fromTime <= convert(time,@currTime) and toTime > convert(time,@currTime)";
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("dateType", dateType);
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
                string query1 = @"select travelTime from AvgStnToStnTravelTime where [dateType]=@dateType 
                            and fromPlat like @fromPlat and toPlat like @toPlat and fromTime <= convert(time,@currTime) and toTime > convert(time,@currTime)";
                SqlCommand cmd1 = new SqlCommand(query1, cmd.Connection, cmd.Transaction);
                cmd1.Parameters.AddWithValue("dateType", dateType);
                cmd1.Parameters.AddWithValue("fromPlat", fromPlat.Substring(0,3) + "%");
                cmd1.Parameters.AddWithValue("toPlat", toPlat.Substring(0,3)+"%");
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
        #endregion

        public void QueryAccuracy(System.IO.StreamWriter writer)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                DateTime fromTime = Convert.ToDateTime("2017-11-07 05:00:00");
                DateTime endTime = Convert.ToDateTime("2017-11-08 03:30:00");

                while (true)
                {
                    DateTime toTime = fromTime.AddMinutes(30);
                    string query = @"select (select count(*) from ForecastError where ForecastError<60 and convert(time,[time])>=convert(time,@fromTime) and convert(time,[time])<convert(time,@toTime)),
                            (select count(*) from ForecastError where ActualError<60 and  convert(time,[time])>=convert(time,@fromTime) and convert(time,[time])<convert(time,@toTime)),
                            (select count(*) from ForecastError where ForecastError<120 and  convert(time,[time])>=convert(time,@fromTime) and convert(time,[time])<convert(time,@toTime)),
                            (select count(*) from ForecastError where ActualError<120 and  convert(time,[time])>=convert(time,@fromTime) and convert(time,[time])<convert(time,@toTime)),
                            (select count(*) from ForecastError where ForecastError<180 and  convert(time,[time])>=convert(time,@fromTime) and convert(time,[time])<convert(time,@toTime)),
                           (select count(*) from ForecastError where ActualError<180 and  convert(time,[time])>=convert(time,@fromTime) and convert(time,[time])<convert(time,@toTime)),
                            (select count(*) from ForecastError where  convert(time,[time])>=convert(time,@fromTime) and convert(time,[time])<convert(time,@toTime)),
                            avg(ForecastError), avg(actualError) from ForecastError
                            where  convert(time,[time])>=convert(time,@fromTime) and convert(time,[time])<convert(time,@toTime) ";

                    SqlCommand cmd2 = new SqlCommand(query, conn);
                    cmd2.Parameters.AddWithValue("fromTime", fromTime);
                    cmd2.Parameters.AddWithValue("toTime", toTime);

                    SqlDataReader reader = cmd2.ExecuteReader();

                    while (reader.Read())
                    {
                        writer.Write(fromTime.ToString("H:mm:ss")+",");
                        int n1 = reader.GetInt32(0);
                        int n2 = reader.GetInt32(1);
                        int n3 = reader.GetInt32(2);
                        int n4 = reader.GetInt32(3);
                        int n5 = reader.GetInt32(4);
                        int n6 = reader.GetInt32(5);
                        int n = reader.GetInt32(6);
                        writer.Write( ((double)n1)/((double)n)+ ",");
                        writer.Write(((double)n2) / ((double)n) + ",");
                        writer.Write(((double)n3) / ((double)n) + ",");
                        writer.Write(((double)n4) / ((double)n) + ",");
                        writer.Write(((double)n5) / ((double)n) + ",");
                        writer.Write(((double)n6) / ((double)n) + ",");
                        if (!reader.IsDBNull(7))
                            writer.Write(reader.GetInt32(7) + ",");
                        else
                            writer.Write("0,");
                        if (!reader.IsDBNull(8))
                            writer.Write(reader.GetInt32(8) + ",");
                        else
                            writer.Write("0");
                        writer.WriteLine();
                    }

                    reader.Close();
                    fromTime = toTime;
                    if (fromTime >= endTime)
                        break;
                }
                conn.Close();
            }
        
        }


        public CircuitData GetCircuitData(string trackCircuit)
        {
            CircuitData cd = new CircuitData();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand com = new SqlCommand(@"SELECT PlatformCode,Bound,PositionCode FROM ATSSCircuitMapping WHERE CircuitCode=@CC
                ", conn))
                {
                    com.Parameters.AddWithValue("CC", trackCircuit);
                    SqlDataReader reader = com.ExecuteReader();

                    if (reader.Read())
                    {

                        cd.PlatformCode = reader[0].ToString();
                        if (!reader.IsDBNull(1))
                            cd.Bound = reader[1].ToString();
                        if (!reader.IsDBNull(2))
                            cd.PositionCode = reader[2].ToString();
                    }
                    reader.Close();
                }
            }
            return cd;
        }

        public  double Mean(List<int> list)
        {
            int sum = 0;
            foreach (int no in list)
                sum += no;
            return sum / ((double)list.Count);
        }

        public  double Variance(List<int> list, double mean)
        {
            double total = 0;
            foreach (int no in list)
            {
                total += (no - mean) * (no - mean);
            }

            return total / ((double)list.Count);
        }

        public  double Mean(int no1, int no2)
        {
            double sum = no1 + no2;
            return sum / 2.0;
        }

        public  double Variance(int no1, int no2, double mean)
        {
            double total = (no1 - mean) * (no1 - mean) + (no2 - mean) * (no2 - mean);


            return total / 2;
        }

        /// <summary>
        /// Find the mean of two among three that has the minimum variance
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public  double Mean3WithMinVar(List<int> list)
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

        #region SQS
//        private static void SendMessages(List<ForecastedTrainArrivalTime> messages)
//        {
//            var config = new AmazonSQSConfig();
//            config.ServiceURL = "http://sqs.ap-southeast-1.amazonaws.com";
//            config.ProxyHost = "hqpr1.smrt.com.sg";
//            config.ProxyPort = 8080;
//            config.ProxyCredentials = CredentialCache.DefaultCredentials;
//            var sqs = new AmazonSQSClient(config);

//            // preparing send request (with queue URL and message body)
//            var req = new SendMessageRequest();
//            req.QueueUrl = "https://sqs.ap-southeast-1.amazonaws.com/971616992866/connect-forecasting-dev";
//            req.MessageBody = GetMessageBody(messages);

//            Logging.log.Debug("Sending message: \n" + req.MessageBody);


//            // send request and show result
//            try
//            {
//                var response = sqs.SendMessage(req);
//                Logging.log.Debug("Message sent.");
//                Logging.log.Debug("MessageId: " + response.MessageId);
//                Logging.log.Debug("MD5: " + response.MD5OfMessageBody);
//            }
//            catch (InvalidMessageContentsException)
//            {
//                Logging.log.Debug("Error: InvalidMessageContentsException - The message contains characters outside the allowed set.");
//            }
//            catch (UnsupportedOperationException)
//            {
//                Logging.log.Debug("Error: UnsupportedOperationException - Error code 400. Unsupported operation.");
//            }
//            catch (Exception ex)
//            {
//                Logging.log.Debug(" Error sending data to SQS " + ex.Message, ex);
//            }
//        }
//        private static string GetMessageBody(List<ForecastedTrainArrivalTime> items)
//        {
//            // Each message must be a JSON array containing JSON objects of the following format:
//            // {
//            //   "type": "TheNameOfTheClass",
//            //   "data": {
//            //     ...
//            //   }
//            // }

//            // IMPORTANT: MessageBody can be up to 256KB (262,144 bytes) in size.
//            // Avoid sending too many elements in the so that message size can be kept below 256 KB.
//            // Ref: http://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessage.html



//            List<object> list = new List<object>();
//            foreach (ForecastedTrainArrivalTime f in items)
//            {
//                list.Add(new Dictionary<string, object> {
//                { "type", "ForecastedTrainArrivalTime" },
//                { "data", f }
//            });
//            }

//            return JsonConvert.SerializeObject(list);
//        }

//        public void QueueForecastForSending(SqlCommand c,int newID)
//        {
//            if (forecasts == null)
//                forecasts = new List<ForecastedTrainArrivalTime>();
//            Logging.log.Debug("Queueing forecast for sending");
//            c.CommandText = @"SELECT DFAT.TrainNo,DFAT.EMUNo,DFAT.Time,DFAT.PlannedStationTime,B.Code,DFAT.Platform FROM DailyForecastedArrivalTime DFAT 
//                    WITH (NOLOCK) 
//                    INNER JOIN Bound B ON B.ID = DFAT.Bound WHERE DFAT.ID=@ID";
//            c.Parameters.AddWithValue("ID", newID);
//            using (SqlDataReader r = c.ExecuteReader())
//            {
//                while (r.Read())
//                {
//                    ForecastedTrainArrivalTime f = new ForecastedTrainArrivalTime();
//                    f.TrainNo = r.GetString(0);
//                    if (!r.IsDBNull(1))
//                        f.EmuNo = r.GetString(1);
//                    if (!r.IsDBNull(2))
//                        f.Time = r.GetDateTime(2);
//                    if (!r.IsDBNull(3))
//                        f.PlannedST = (int)r.GetInt64(3);
//                    if (!r.IsDBNull(4))
//                        f.Bound = r.GetString(4);
//                    if (!r.IsDBNull(5))
//                        f.Platform = r.GetString(5);
//                    forecasts.Add(f);
//                }
//                r.Close();

//            }
//            if (forecasts.Count > 20)
//            {
//                //Clear the queue
//                List<ForecastedTrainArrivalTime> queue = new List<ForecastedTrainArrivalTime>();
//                BackgroundWorker sqsWorker = new BackgroundWorker();
//                sqsWorker.DoWork += new DoWorkEventHandler(sqsWorker_DoWork);
//                for (int i = 0; i < forecasts.Count; i++)
//                {
//                    ForecastedTrainArrivalTime f = forecasts[i];
//                    queue.Add(f);
//                    forecasts.Remove(f);
//                    i--;
//                }
//                Logging.log.Debug("Sending " + queue.Count + " messages");
//                SendMessages(queue);
//                //.RunWorkerAsync(queue);
//                //sqsWorker.Wa
//            }

//        }

        void sqsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
           // SendMessages((List<ForecastedTrainArrivalTime>)e.Argument);
        }
        #endregion
    }

}