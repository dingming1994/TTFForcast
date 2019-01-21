using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Net.Mime;
using Microsoft.SqlServer.Server;
using System.IO;
using System.Globalization;


namespace TTFSQL
{
    public partial class StoredProcedures
    {
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void SimulateATSS(string date, int method)
        {
            DateTime opsDate = DateTime.Parse(date);
            TimeSpan ts1 = new TimeSpan(4, 0, 0);
            TimeSpan ts2 = new TimeSpan(3, 30, 0);
            DateTime start = opsDate + ts1;
            DateTime end = opsDate.AddDays(1) + ts2;
            List<string> records = new List<string>();

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                string sql = @"select ATSSSignal from ATSSInputLog where timestamp > @start and timestamp < @end order by timestamp";
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue(@"start", start);
                cmd.Parameters.AddWithValue(@"end", end);

                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    records.Add(r.GetString(0));
                }
                r.Close();
            }

        //    Dictionary<string, int> map = new Dictionary<string, int>();
            foreach (string record in records)
            {
                ProcessATSSSignal(record,method);
            }
        }

        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void GenerateATSSActualTime(string date)
        {
            DateTime opsDate = DateTime.Parse(date);
            TimeSpan ts1 = new TimeSpan(4, 0, 0);
            TimeSpan ts2 = new TimeSpan(3, 30, 0);
            DateTime start = opsDate + ts1;
            DateTime end = opsDate.AddDays(1) + ts2;
            List<string> records = new List<string>();

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                string sql = @"select ATSSSignal from ATSSInputLog where timestamp > @start and timestamp < @end order by timestamp";
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue(@"start", start);
                cmd.Parameters.AddWithValue(@"end", end);

                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    records.Add(r.GetString(0));
                }
                r.Close();
            }

            //    Dictionary<string, int> map = new Dictionary<string, int>();
            foreach (string record in records)
            {
                RecordATSSSignal(record);
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

                        cmdText = "TRUNCATE TABLE TrainNoEMUNoMapping";
                        cmd = new SqlCommand(cmdText, conn, t);
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
        public static int RecordATSSSignal(string signal)
        {
            try
            {
                string[] elements = signal.Split(new char[] { ',' });

                string type = elements[0];

                DateTime time = DateTime.Now;   //It should be time = DateTime.Now

                try
                {
                    time = DateTime.ParseExact(elements[1], "yyyy-MM-dd HH:mm:ss",
                                                                       new CultureInfo("en-US"),
                                                                       DateTimeStyles.None);

                    // time = Convert.ToDateTime(elements[1]);
                }
                catch (Exception e)
                {
                    throw new Exception("Unable to parse the ATSS time");
                }              
              

                if (type == "A")
                {
                    RecordASignal(elements);
                    //, writer
                    ;
                }

                if (type == "B")
                {
                    ProcessBSignal(elements);
                }

                if (type == "C")
                {
                    ProcessCSignal(elements);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in processing " + signal + " with error: " + ex.Message, ex);
            }

            return 1;
        }

        [Microsoft.SqlServer.Server.SqlProcedure]
        public static int ProcessATSSSignal(string signal, int method)
        {
            try
            {
                string[] elements = signal.Split(new char[] { ',' });

                string type = elements[0];


                if (type == "A")
                {
                    ProcessASignal(elements, method);
                    //, writer
                    ;
                }

                if (type == "B")
                {
                    ProcessBSignal(elements);
                }

                if (type == "C")
                {
                    ProcessCSignal(elements);
                }
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

            return 1;
        }

        public static int RecordASignal(string[] elements)
        {
            string boundCode = elements[5];

            if (boundCode == "SB" || boundCode == "NB") //ignore south bound and north bound
                return 0;

            string type = elements[6];

            if (type == "DEP")
            {
                return 0;
            }

            DateTime time = DateTime.Now;
            try
            {
                time = DateTime.ParseExact(elements[1], "yyyy-MM-dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None);
            }
            catch (Exception ex)
            {
                throw new Exception("Wrong datetime formate: " +  elements[1]);
                return 0;
            }

            string trainno = elements[2].Trim();
            if (trainno.Trim().Length != 3)
            {
                return 0;
            }

            string platform = elements[4].Trim();
            string station = platform.Substring(0,3);

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();

                try
                {
                    bool isService = CommonUtils.IsPlatformServiceable(conn, platform);

                    if (!isService)
                    {
                        conn.Close();
                        return 0;
                    }

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

                    #region Handling train number
                    //int firstTrainNoDigit = 0;

                    //try
                    //{
                    //    firstTrainNoDigit = Convert.ToInt16(trainno.Substring(0, 1));
                    //}
                    //catch (Exception ex)
                    //{
                    //    //        Logging.log.Error("Wrong train number format: " + line);
                    //    conn.Close();
                    //    return 0;
                    //}

                    string trainNoAlias = "";

                    //if (firstTrainNoDigit != 1 && firstTrainNoDigit != 2 && firstTrainNoDigit != 7)
                    //{

                    //    trainNoAlias = "1" + trainno.Substring(1, 2);
                    //}
                    //else
                    //{
                    //    trainNoAlias = trainno;
                    //}
                    #endregion

                    #region Get emu number
                    string emuNo = CommonUtils.GetEMUNoByTrainNo(conn, trainno, platform, time);

                    if (emuNo == "00330034")
                    {
                        int n = 0;
                    }

                    if (emuNo == "")
                    {
                        conn.Close();
                        return 0;
                    }
                    #endregion

                    #region Only handle Arr signal. In case the first signal received at a platform is a departure, treat it as arrival


                    string sql = @"SELECT TOP 1 platform, plannedSTID, ID FROM TrainMovement 
                                    WHERE EMUNo = @emuNo order by [LastSignalTime] desc";
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue(@"emuNo", emuNo);

                    SqlDataReader r = cmd.ExecuteReader();
                    int movementID = 0;
                    Int64 plannID = 0;
                    while (r.Read())
                    {
                        string lastPlt = r.GetString(0);
                        plannID = r.GetInt64(1);

                        if (lastPlt == platform && plannID != 0)
                        {
                            type = "DEP";
                        }
                        else
                        {
                            if (lastPlt == platform && plannID == 0)
                            {
                                movementID = r.GetInt32(2);  //last Arrival signal is a failed signal
                            }
                        }
                    }
                    r.Close();

                    if (type == "DEP")
                    {
                        conn.Close();
                        return 0;
                    }
                    #endregion

                    DailyTrainStationTime ST = CommonUtils.GetPlannedStationTimeByATSSData(conn, trainno, platform, actualTime, station);


                    Int64 plannedID = 0;
                    if (ST != null)
                    {
                        plannedID = ST.plannedSTID;
                        boundCode = ST.bound;
                        trainNoAlias = ST.trainNo;
                    }
                    if (movementID == 0)
                    {
                        CommonUtils.AddTrainMovement(conn, emuNo, trainno, station, boundCode, time, trainNoAlias, platform, plannedID);
                        CommonUtils.AddActualStationTime(conn, plannedID, emuNo, trainno, station, boundCode, time, trainNoAlias, platform);
                    }
                    else
                    {
                        CommonUtils.UpdateTrainMovement(conn, movementID, time, plannedID);
                        CommonUtils.UpdateActualStationTime(conn, emuNo, platform, time, plannedID);
                    }
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
            return 1;
        }

        public static int ProcessASignal(string[] elements, int method)
        {
            string boundCode = elements[5];

            if (boundCode == "SB" || boundCode == "NB") //ignore south bound and north bound
                return 0;

            string type = elements[6];

            if (type == "DEP")
            {
                return 0;
            }

            DateTime time = DateTime.Now;
            try
            {
                time = DateTime.ParseExact(elements[1], "yyyy-MM-dd HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None);
            }
            catch (Exception ex)
            {
                throw new Exception("Wrong datetime formate: " + elements[1]);
                return 0;
            }

            string trainno = elements[2].Trim();
            if (trainno.Trim().Length != 3)
            {
                return 0;
            }

            string platform = elements[4].Trim();
            string station = platform.Substring(0, 3);

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();

                bool isService = CommonUtils.IsPlatformServiceable(conn, platform);

                if (!isService)
                {
                    conn.Close();
                    return 0;
                }

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

                #region Handling train number
                //int firstTrainNoDigit = 0;

                //try
                //{
                //    firstTrainNoDigit = Convert.ToInt16(trainno.Substring(0, 1));
                //}
                //catch (Exception ex)
                //{
                //    //        Logging.log.Error("Wrong train number format: " + line);
                //    conn.Close();
                //    return 0;
                //}

                string trainNoAlias = "";

                //if (firstTrainNoDigit != 1 && firstTrainNoDigit != 2 && firstTrainNoDigit != 7)
                //{

                //    trainNoAlias = "1" + trainno.Substring(1, 2);
                //}
                //else
                //{
                //    trainNoAlias = trainno;
                //}
                #endregion

                #region Get emu number
                string emuNo = CommonUtils.GetEMUNoByTrainNo(conn, trainno, platform, time);

                if (emuNo == "")
                {
                    conn.Close();
                    return 0;
                }
                #endregion

                #region Only handle Arr signal. In case the first signal received at a platform is a departure, treat it as arrival
                string sql = @"SELECT TOP 1 platform,  plannedSTID, ID FROM TrainMovement 
                                    WHERE EMUNo = @emuNo order by [LastSignalTime] desc";
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue(@"emuNo", emuNo);

                SqlDataReader r = cmd.ExecuteReader();
                int movementID = 0;
                Int64 plannID = 0;
                while (r.Read())
                {
                    string lastPlt = r.GetString(0);
                    plannID = r.GetInt64(1);

                    if (lastPlt == platform && plannID != 0)
                    {
                        type = "DEP";
                    }
                    else
                    {
                        if (lastPlt == platform && plannID == 0)
                        {
                            movementID = r.GetInt32(2);  //last Arrival signal is a failed signal
                        }
                    }
                }
                r.Close();

                if (type == "DEP")
                {
                    conn.Close();
                    return 0;
                }
                #endregion

                DailyTrainStationTime ST = CommonUtils.GetPlannedStationTimeByATSSData(conn, trainno, platform, actualTime, station);
                

                if (ST == null)
                {
                    CommonUtils.AddTrainMovement(conn, emuNo, trainno, station, "", time, "", platform,0);
                    //      CommonUtils.DeleteTrainMovementByEMU(conn, emnuNo); //Need to talk to Connect Vendor to build API to remove forecast
                }
                else
                {
                    TrainMovement lastMove = CommonUtils.GetLastTrainMovement(conn, emuNo, platform);
                    trainNoAlias = ST.trainNo;

                    if (lastMove == null) //If this is the first movement of the EMU
                    {
                        CommonUtils.AddTrainMovement(conn, emuNo, trainno, station, ST.bound, time, trainNoAlias, platform,ST.plannedSTID);
                        //Don't do forecast for the first movement
                    }
                    else
                    {
                        int travelTime = (int)(time - lastMove.LastSignalTime).TotalSeconds;

                        if (travelTime > 3000) //either split duty or accident
                        {
                            CommonUtils.DeleteTrainMovementByEMU(conn, emuNo);
                        }

                        if (movementID == 0)
                        {
                            CommonUtils.AddTrainMovement(conn, emuNo, trainno, station, boundCode, time, trainNoAlias, platform, ST.plannedSTID);                            
                        }
                        else
                        {
                            CommonUtils.UpdateTrainMovement(conn, movementID, time, ST.plannedSTID);
                        }
                     
                        int noServiceMovement = CommonUtils.GetNoServiceMovements(conn, emuNo);
                        //Only start recording stn to stn travel time from the second station onwards, because very often the 
                        //first station movement does not follow timetable closely
                        #region In case there are more than 2 service movements, record stn to stn travel time
                        if (noServiceMovement > 2)
                        {
                            CommonUtils.AddStnToStnTravelTimeToday(conn, lastMove.Platform, platform, travelTime, time);

                            CommonUtils.UpdateTravelTime(conn, lastMove.Platform, platform);
                            CommonUtils.UpdateForecast(conn, trainNoAlias, emuNo, ST.time, time, method);
                        }
                        #endregion


                    }
                }

                conn.Close();
            }
            return 1;
        }

          /// <summary>
        /// Signal B is to assign an EMU number to a train number
        /// </summary>
        /// <param name="line"></param>
        private static void ProcessBSignal(string[] elements)
        {
            //    DateTime time = Convert.ToDateTime(elements[1]);
            string trainno = elements[2].Trim();
            string emuNo = elements[3].Trim() + elements[4].Trim();
            int no1 = Convert.ToInt32(elements[3]);
            int no2 = Convert.ToInt32(elements[4]);
            if (no1 > no2)
                emuNo = elements[4] + elements[3];

            if(emuNo == "00330034")
            {
                int n=0;
            }

            bool emuNoExisting = false;
            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                string cmdText = @"select trainno from TrainNoEMUNoMapping where EMUNo = @emuNo";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("emuNo", emuNo);

                    SqlDataReader r = cmd.ExecuteReader();

                    if (r.Read())
                    {
                        //    string train = r.GetString(0);
                        //if (trainno != train)
                        //{
                        //    throw new ApplicationException("The train number " + trainno + " and emuNo " + emuNo + " are mismatched.");
                        //}
                        emuNoExisting = true;
                    }
                    r.Close();
                }

                if (emuNoExisting)
                {
                    cmdText = @"Update TrainNoEMUNoMapping set TrainNo=@trainno,DateCreated=(select GETDATE()) where EMUNo=@emuNo";
                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("emuNo", emuNo);
                        cmd.Parameters.AddWithValue("trainno", trainno);

                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    cmdText = @"INSERT INTO TrainNoEMUNoMapping (TrainNo, EMUNo, datecreated) values (@trainno, @emuNo,(select GETDATE()))";
                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("emuNo", emuNo);
                        cmd.Parameters.AddWithValue("trainno", trainno);

                        cmd.ExecuteNonQuery();
                    }
                }

                cmdText = @"UPDATE DailyActualStationTime SET EMUNo = @emu where LEN(EMUNo) < 6  and TrainNo = @trainNo ";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("emu", emuNo);
                    cmd.Parameters.AddWithValue("trainNo", trainno);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Signal C is to 
        /// </summary>
        /// <param name="line"></param>
        private static void ProcessCSignal(string[] elements)
        {

            for (int len = 0; len < elements.Length; len++)
                elements[len] = elements[len].Trim();

            //  DateTime time = Convert.ToDateTime(elements[1]);
            string trainno1 = elements[2];
            string trainno2 = elements[3];

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();

                string cmdText = @"Select * from TrainNoEMUNoMapping where trainno=@trainno1";
                bool hasRecord = false;
                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("trainno1", trainno1);

                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        hasRecord = true;
                    }
                    r.Close();
                }

                if (!hasRecord)
                {
                    return;
                }

                cmdText = @"UPDATE TrainNoEMUNoMapping SET TrainNo = null, oldTrainno=@trainno2, DateCreated=(select GETDATE()) where TrainNo = @trainno2  ";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("trainno2", trainno2);

                    cmd.ExecuteNonQuery();
                }

                cmdText = @"UPDATE TrainNoEMUNoMapping SET TrainNo = @trainno2, oldtrainno=@trainno1, DateCreated=(select GETDATE()) where TrainNo = @trainno1  ";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("trainno1", trainno1);
                    cmd.Parameters.AddWithValue("trainno2", trainno2);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    
    }
}
