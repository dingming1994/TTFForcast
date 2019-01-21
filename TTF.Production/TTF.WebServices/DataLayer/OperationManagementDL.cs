///
///<file>OperationManagementDL.cs</file>
///<description>
/// Data layer for managing daily operations. It manages data in the following database tables:
///  -DailyTrainCaptainStatus
///  -DailyTrainCaptainScan
///  -DailyPlannedWorkPiece
///  -DailyActualWorkPiece
///  -DailyTrainStationTime
///</description>
///

///
///<created>
///<author>Liu Qizhang</author>
///<date>18-12-2013</date>
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

namespace TTF.DataLayer
{
    public class OperationManagementDL
    {

        //  #region Handle actual station time
        /// <summary>
        /// Add a new ATSS data record in DailyActualStationTime table
        /// </summary>
        /// <param name="TrainNo"></param>
        /// <param name="ScheduleNo"></param>
        /// <param name="lineID"></param>
        /// <param name="boundID"></param>
        /// <param name="Time"></param>
        /// <param name="plannedStationTime"></param>
        /// <param name="alertID"></param>
        /// <param name="deviation"></param>
        /// <param name="type"></param>
        /// <param name="EMUNo"></param>
        /// <param name="platform"></param>
        /// <param name="trainCaptainID"></param>
        /// <param name="trainNoAlias"></param>
        /// <returns></returns>
        public int AddATSSData(string TrainNo, DateTime Time, long? plannedStationTime, string type, string EMUNo, string platform, string trainNoAlias, DateTime opsDate)
        {
            int result = -1;

            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    #region Try to check whether there are past record for the given EMU at the given platform
//                    DateTime time1 = Time.AddMinutes(-5);
//                    string cmdText = @"Select [ID] from DailyActualStationTime where [platform]=@platform and 
//                                EMUNo=@EMUNo and [type]=@type and [time]>@time";
//                    int ID = -1;
//                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
//                    {
//                        cmd.Parameters.AddWithValue("platform", platform);
//                        cmd.Parameters.AddWithValue("EMUNo", EMUNo);
//                        cmd.Parameters.AddWithValue("type", type);
//                        cmd.Parameters.AddWithValue("time", time1);

//                        SqlDataReader r = cmd.ExecuteReader();

//                        if (r.Read())
//                        {
//                            ID = r.GetInt32(0);
//                        }
//                        r.Close();
//                    }
//                    //If it is an arrival and there was a record already, then replace the record with current once
//                    if (ID > -1 && type == "ARR")
//                    {
//                        cmdText = "UPDATE DailyActualStationTime set [time]=@time where [ID]=@ID";
//                        using (SqlCommand cmd = new SqlCommand(cmdText, conn))
//                        {
//                            cmd.Parameters.AddWithValue("ID", ID);
//                            cmd.Parameters.AddWithValue("time", Time);

//                            cmd.ExecuteNonQuery();
//                        }
//                        return ID;
//                    }

//                    if (ID > -1 && type == "DEP")
//                        return ID;

                    #endregion

                    if (plannedStationTime != null)
                    {
                        string cmdText = @"insert into [dbo].DailyActualStationTime (TrainNo,[time],PlannedStationTime,[type],EMUNo,[platform],OpsDate,trainNoAlias,bound, stn)
                                select @TrainNo, @Time,@plannedStationTime,@type,@EMUNo,@platform,@opsDate,@trainNoAlias, b.code, s.code from DailyTrainStationTime st 
                                inner join Bound b on st.BoundID=b.id
                                inner join [platform] p on st.PlatformID=p.id
                                inner join [Station] s on p.stationid=s.id
                                where st.id=@plannedStationTime";


                        using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                        {
                            cmd.Parameters.Add(new SqlParameter("@TrainNo", TrainNo));
                            cmd.Parameters.Add(new SqlParameter("@Time", Time));
                            cmd.Parameters.Add(new SqlParameter("@plannedStationTime", plannedStationTime.Value));
                            cmd.Parameters.Add(new SqlParameter("@type", type));
                            cmd.Parameters.Add(new SqlParameter("@EMUNo", EMUNo));
                            cmd.Parameters.Add(new SqlParameter("@opsDate", opsDate));
                            cmd.Parameters.Add(new SqlParameter("@platform", platform));
                            cmd.Parameters.Add(new SqlParameter("@trainNoAlias", trainNoAlias));
                            SqlDataReader r = cmd.ExecuteReader();
                            if (r.Read())
                            {
                                result = r.GetInt32(0);
                            }
                            r.Close();
                        }
                    }
                    else
                    {
                        string cmdText = @"insert into [dbo].DailyActualStationTime (TrainNo,[time],[type],EMUNo,[platform],OpsDate,trainNoAlias)
                                values (@TrainNo, @Time,@type,@EMUNo,@platform,@opsDate,@trainNoAlias)";
                        using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                        {
                            cmd.Parameters.Add(new SqlParameter("@TrainNo", TrainNo));
                            cmd.Parameters.Add(new SqlParameter("@Time", Time));
                            cmd.Parameters.Add(new SqlParameter("@type", type));
                            cmd.Parameters.Add(new SqlParameter("@EMUNo", EMUNo));
                            cmd.Parameters.Add(new SqlParameter("@opsDate", opsDate));
                            cmd.Parameters.Add(new SqlParameter("@platform", platform));
                            cmd.Parameters.Add(new SqlParameter("@trainNoAlias", trainNoAlias));

                            SqlDataReader r = cmd.ExecuteReader();
                            if (r.Read())
                            {
                                result = r.GetInt32(0);
                            }
                            r.Close();
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Cannot add ATSS data for " + TrainNo + "," + Time.ToString() + "," + EMUNo + "," + platform + "," + trainNoAlias);
            }


            return result;
        }

        public bool CheckIfDuplicateATSSData(string trainno, string platform, DateTime Time, string type, int deviation)
        {
            bool exists = false;


            //Find the planned station time via platform
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                //Check if an actual signal with same train No, platform and type was received in the last 1 minute
                string cmdText = @"SELECT ast.ID, ast.plannedStationTime, tst.Time FROM DailyActualStationTime ast 
inner join DailyTrainStationTime tst on ast.PlannedStationTime=tst.ID
WHERE ast.TrainNo=@trainNo AND ast.[Platform]=@platform 
AND @Time < DATEADD(MINUTE,5,ast.[Time])
order by ast.[time] desc";

                int ID = 0;
                DateTime plannedTime = DateTime.Now;

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {

                    cmd.Parameters.AddWithValue("trainNo", trainno.Trim());
                    cmd.Parameters.AddWithValue("platform", platform);
                    cmd.Parameters.AddWithValue("Time", Time);


                    SqlDataReader r = cmd.ExecuteReader();

                    if (r.Read())
                    {
                        ID = r.GetInt32(0);
                        plannedTime = r.GetDateTime(2);

                        exists = true;

                    }

                    r.Close();
                }

                if (exists)
                {
                    DateTime newTime = plannedTime.AddSeconds(-1 * deviation);

                    cmdText = "UPDATE DailyActualStationTime SET [time] = @newTime WHERE ID = @id";

                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {

                        cmd.Parameters.AddWithValue("newTime", newTime);
                        cmd.Parameters.AddWithValue("id", ID);

                        cmd.ExecuteNonQuery();
                    }
                }
            }


            return exists;
        }


        #region handle planned station time
        public string GetEMUNoByTrainNo(string trainno, string platform, DateTime time)
        {
            string emu = "";
            string error = "";

            //Find the planned station time via platform
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
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


                #region If the emu number cannot be found, look for the most like train by train movement
                if (emu == "" && !DataManager.Instance().MapUnmappedTrainNo.ContainsKey(trainno) && trainno.Length < 4)
                {
                    cmdText = @"select EMUNo, Datediff (second, time, @time), TrainNo, stationid, 
CASE when (select platformno from platform where code = @platform and versionid=@versionId) - p.platformno < 0 
then (select platformno from platform where code = @platform and versionid=@versionId) - p.platformno + p.length
ELSE (select platformno from platform where code = @platform and versionid=@versionId) - p.platformno END As gap, p.length
from  (select  no =row_number() over (partition by emuno order by [time] desc), * from DailyActualStationTime) t
inner join [platform] p on t.Platform = p.Code
where no<2 and p.VersionID=@versionId and Datediff (second, time, @time) <600 and p.length is not null
order by gap, time desc";

                    List<CandidateEMU> list = new List<CandidateEMU>();

                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("time", time);
                        cmd.Parameters.AddWithValue("versionId", DataManager.Instance().NetworkVersion.ID);
                        cmd.Parameters.AddWithValue("platform", platform);

                        SqlDataReader r = cmd.ExecuteReader();

                        while (r.Read())
                        {
                            string emu1 = r.GetString(0);
                            int timeDiff = r.GetInt32(1);
                            string trainno1 = r.GetString(2);
                            short station = r.GetInt16(3);
                            short gap = r.GetInt16(4);

                            if (gap > 1)
                                break;

                            CandidateEMU candidate = new CandidateEMU();
                            candidate.emu = emu1;
                            candidate.timeDiff = timeDiff;
                            candidate.gap = gap;
                            candidate.trainno = trainno1;
                            candidate.stationid = station;
                            list.Add(candidate);
                        }

                        r.Close();
                    }
                    string oldTrainNo = "";
                    foreach (CandidateEMU candidate in list)
                    {
                        if ((candidate.gap == 0 || candidate.gap == 1) && candidate.trainno.Substring(candidate.trainno.Length - 2, 2) == trainno.Substring(trainno.Length - 2, 2))
                        {
                            emu = candidate.emu;
                            oldTrainNo = candidate.trainno;
                            break;
                        }
                    }

                    if (emu == "")
                    {
                        foreach (CandidateEMU candidate in list)
                        {
                            if (candidate.gap == 0 && candidate.timeDiff < 40)
                            {
                                emu = candidate.emu;
                                oldTrainNo = candidate.trainno;
                                break;
                            }

                            if (candidate.gap == 1)
                            {
                                cmdText = @"
                            select [time] from plattoplattraveltime s2s 
                            inner join station stn1 on s2s.platform1 = stn1.code
                            inner join station stn2 on s2s.platform2 = stn2.code
                            inner join platform plt on plt.StationID = stn2.id
                            where stn1.VersionID = @versionId and stn2.VersionID = @versionId and stn1.id = @station and plt.Code = @platform";

                                int travelTime = 0;

                                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                                {
                                    cmd.Parameters.AddWithValue("station", candidate.stationid);
                                    cmd.Parameters.AddWithValue("versionId", DataManager.Instance().NetworkVersion.ID);
                                    cmd.Parameters.AddWithValue("platform", platform);

                                    SqlDataReader r = cmd.ExecuteReader();

                                    if (r.Read())
                                    {
                                        travelTime = r.GetInt32(0);
                                    }
                                    else
                                    {
                                        error += "No travel time defined between station " + candidate.stationid + " and " + platform;
                                    }
                                    r.Close();
                                }

                                if (candidate.timeDiff > travelTime)
                                {
                                    emu = candidate.emu;
                                    oldTrainNo = candidate.trainno;
                                    break;
                                }
                            }
                        }
                    }


                    if (emu != "") //if it is changing from a working train no to another working train no, need to verify
                    //need to make sure the old train no is no longer using the emu no
                    {
                        if ((trainno.Substring(0, 1) == "1" || trainno.Substring(0, 1) == "2") &&
                            (oldTrainNo.Substring(0, 1) == "1" || oldTrainNo.Substring(0, 1) == "2"))
                        {
                            cmdText = @"select EMUNo, TrainNo, platform
from  (select  no =row_number() over (partition by emuno order by [time] desc), * from DailyActualStationTime) t 
where no<2  and Datediff (second, time, @time) <600";

                            using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                            {
                                cmd.Parameters.AddWithValue("trainno", oldTrainNo);
                                cmd.Parameters.AddWithValue("emuno", emu);
                                cmd.Parameters.AddWithValue("time", time);

                                SqlDataReader r = cmd.ExecuteReader();

                                if (r.Read())
                                {
                                    emu = "";
                                }

                                r.Close();
                            }
                        }
                        //Non service train can only be reformed from the train number that has the same last two digits.
                        if (trainno.Substring(0, 1) != "1" && trainno.Substring(0, 1) != "2")
                        {
                            if (trainno.Substring(trainno.Length - 2, 2) != oldTrainNo.Substring(oldTrainNo.Length - 2, 2))
                            {
                                emu = "";
                            }
                        }
                    }

                    if (emu != "")
                    {
                        cmdText = @"Update TrainNoEMUNoMapping set oldTrainNo=@oldTrainNo, trainno = @trainno,  DateCreated=(select GETDATE()) where emuno=@emuno";

                        using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                        {
                            cmd.Parameters.AddWithValue("oldTrainNo", oldTrainNo);
                            cmd.Parameters.AddWithValue("trainno", trainno);
                            cmd.Parameters.AddWithValue("emuno", emu);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                #endregion

                #region Next check the case that a train may be withdrawn and turn around at some station
                if (emu == "" && !DataManager.Instance().MapUnmappedTrainNo.ContainsKey(trainno) && trainno.Length < 4)
                {
                    string oldTrainno = "";
                    cmdText = @"select t.EMUNo, t.TrainNo, t.platform
from  (select  no =row_number() over (partition by emuno order by [time] desc), * from DailyActualStationTime) t 
where no<2  and Datediff (second, time, @time) <500 and t.Platform like @platform and substring(t.trainno,2,2) = @trainno";
                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("trainno", trainno.Substring(1, 2));
                        cmd.Parameters.AddWithValue("time", time);
                        cmd.Parameters.AddWithValue("platform", platform.Substring(0, 3) + "%");

                        SqlDataReader r = cmd.ExecuteReader();

                        if (r.Read())
                        {
                            emu = r.GetString(0);
                            oldTrainno = r.GetString(1);
                        }

                        r.Close();
                    }

                    if (emu != "")
                    {
                        cmdText = @"Update TrainNoEMUNoMapping set oldTrainNo=@oldTrainNo, trainno = @trainno,  DateCreated=(select GETDATE()) where emuno=@emuno";

                        using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                        {
                            cmd.Parameters.AddWithValue("oldTrainNo", oldTrainno);
                            cmd.Parameters.AddWithValue("trainno", trainno);
                            cmd.Parameters.AddWithValue("emuno", emu);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                #endregion

            }

            if (emu == "")
            {

                //If still no mapping EMU number, throw an exception
                if (!DataManager.Instance().MapUnmappedTrainNo.ContainsKey(trainno))
                {
                    DataManager.Instance().MapUnmappedTrainNo.Add(trainno, "");
                }
                emu = trainno;

                Logging.log.Error("Cannot find EMU number assigned to train number " + trainno);
                //     throw new ApplicationException("Cannot find EMU number assigned to train number " + trainno);
            }
            else
            {
                if (DataManager.Instance().MapUnmappedTrainNo.ContainsKey(trainno))
                    DataManager.Instance().MapUnmappedTrainNo.Remove(trainno);
            }

            if (error != "")
            {
                Logging.log.Error(error);
                //     throw new ApplicationException(error);
            }

            return emu;
        }

        public void UpdateEMUNoandTrainNoMapping(string trainno1, string trainno2)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
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

        public void AddEMUNotoTrainNo(string trainno, string emuNo)
        {
            bool emuNoExisting = false;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
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

        public bool IsValidMovement(string emuNo, string platform, DateTime time, string type)
        {
            if (type == "ARR")
                return true;


            bool hasMovement = false;

            //Check if the platform belongs to terminal
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                
               

                DateTime timelimit = time.AddSeconds(-800);
                string cmdText = @" select * from DailyActualStationTime where platform like @platform and type = 'ARR' and emuNo=@emuNo and [time] > @timelimit";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("platform", platform.Substring(0,3) + "%");
                    cmd.Parameters.AddWithValue("emuNo", emuNo);
                    cmd.Parameters.AddWithValue("timelimit", timelimit);

                    SqlDataReader r = cmd.ExecuteReader();

                    if (r.Read())
                    {
                        hasMovement = true;
                    }

                    r.Close();
                }

                
            }

            return hasMovement;
        }


        public DailyTrainStationTime GetPlannedStationTimeByATSSData(string trainno, string platform, DateTime time, string type, Station stn)
        {
            DailyTrainStationTime st = null;

            int deviation = (new ForecastManagementDL()).GetAllowableDeviation(trainno);
            DateTime startTime = time.AddSeconds(-1 * deviation);
            DateTime endTime = time.AddSeconds(deviation);

            if (stn.Type != "Terminal")
            {
                //Find the planned station time via platform. We start with searching for those planned station time not linked to actual station time yet.
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    string cmdText = "", p;

                    if (DataManager.Instance().ListTerminalPlatforms.Contains(platform))
                    {
                        cmdText = @"select pst.[ID], pst.TrainNo, pst.ScheduleNo, pst.LineID, pst.BoundID, pst.[PlatformID],pst.[Time],t.[Key] 
                                    from DailyTrainStationTime pst
                                    inner join Platform plt on pst.platformID=plt.ID
                                    inner join Systemlookup t on pst.[Type]=t.ID 
                                    where pst.TrainNo = @trainNo and plt.[code] like @platform and pst.[time] >= @startTime and pst.[time] <= @endTime and t.[key]=@type 
                                    and (pst.covered is null or pst.covered = 0)  ";
                        p = platform.Substring(0, 3) + "%";
                    }
                    else
                    {
                        cmdText = @"select pst.[ID], pst.TrainNo, pst.ScheduleNo, pst.LineID, pst.BoundID, pst.[PlatformID],pst.[Time],t.[Key] 
                                    from DailyTrainStationTime pst
                                    inner join Platform plt on pst.platformID=plt.ID
                                    inner join Systemlookup t on pst.[Type]=t.ID 
                                    where pst.TrainNo = @trainNo and plt.[code]=@platform and pst.[time] >= @startTime and pst.[time] <= @endTime and t.[key]=@type 
                                    and (pst.covered is null or pst.covered = 0)  ";
                        p = platform;
                    }

                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("trainNo", trainno.Trim());
                        cmd.Parameters.AddWithValue("platform", p);
                        cmd.Parameters.AddWithValue("startTime", startTime);
                        cmd.Parameters.AddWithValue("endTime", endTime);
                        cmd.Parameters.AddWithValue("type", type);

                        SqlDataReader r = cmd.ExecuteReader();

                        if (r.Read())
                        {
                            st = new DailyTrainStationTime();

                            st.ID = r.GetInt64(0);
                            st.TrainNo = r.GetString(1);
                            if (!r.IsDBNull(2))
                                st.ScheduleNo = r.GetString(2);
                            if (!r.IsDBNull(3))
                                st.LineID = r.GetInt16(3);
                            if (!r.IsDBNull(4))
                                st.BoundId = r.GetInt16(4);
                            st.PlatformId = r.GetInt16(5);
                            st.Time = r.GetDateTime(6);
                            st.Type = r.GetString(7);
                        }

                        r.Close();
                    }
                    conn.Close();
                }

                if (st == null)
                {
                    //There is case that when train is reformed, it may start with a planned station time that has been covered.
                    //In this case, we have to relax the condition to look for those covereed
                    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                    {
                        conn.Open();
                        string cmdText = @"select pst.[ID], pst.TrainNo, pst.ScheduleNo, pst.LineID, pst.BoundID, pst.[PlatformID],pst.[Time],t.[Key] 
                                    from DailyTrainStationTime pst
                                    inner join Platform plt on pst.platformID=plt.ID
                                    inner join Systemlookup t on pst.[Type]=t.ID 
                                    where pst.TrainNo = @trainNo and plt.[code]=@platform and pst.[time] >= @startTime and pst.[time] <= @endTime and t.[key]=@type";


                        using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                        {
                            cmd.Parameters.AddWithValue("trainNo", trainno.Trim());
                            cmd.Parameters.AddWithValue("platform", platform);
                            cmd.Parameters.AddWithValue("startTime", startTime);
                            cmd.Parameters.AddWithValue("endTime", endTime);
                            cmd.Parameters.AddWithValue("type", type);

                            SqlDataReader r = cmd.ExecuteReader();

                            if (r.Read())
                            {
                                st = new DailyTrainStationTime();

                                st.ID = r.GetInt64(0);
                                st.TrainNo = r.GetString(1);
                                if (!r.IsDBNull(2))
                                    st.ScheduleNo = r.GetString(2);
                                if (!r.IsDBNull(3))
                                    st.LineID = r.GetInt16(3);
                                if (!r.IsDBNull(4))
                                    st.BoundId = r.GetInt16(4);
                                st.PlatformId = r.GetInt16(5);
                                st.Time = r.GetDateTime(6);
                                st.Type = r.GetString(7);
                            }

                            r.Close();
                        }
                        conn.Close();
                    }
                }
            }


            else
            {
                //There is case where the actual platform will be different from planned platform when the station is terminal
                //This block find the planned station time via station 
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();
                    string cmdText = @"select pst.[ID], pst.TrainNo, pst.ScheduleNo, pst.LineID, pst.BoundID, pst.[PlatformID],pst.[Time],t.[Key] 
                                    from DailyTrainStationTime pst 
                                    inner join Platform plt1 on pst.platformID=plt1.ID
                                    inner join Platform plt2 on plt1.stationid = plt2.stationid
                                    inner join Station stn on plt1.stationid=stn.id
                                    inner join Systemlookup t on pst.[Type]=t.ID 
                                    where pst.TrainNo = @trainNo and plt2.[code]=@platform and pst.[time] >= @startTime and pst.[time] <= @endTime and t.[key]=@type 
                                    and (pst.covered is null or pst.covered = 0)  ";

                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("trainNo", trainno);
                        cmd.Parameters.AddWithValue("platform", platform);
                        cmd.Parameters.AddWithValue("startTime", startTime);
                        cmd.Parameters.AddWithValue("endTime", endTime);
                        cmd.Parameters.AddWithValue("type", type);
                        SqlDataReader r = cmd.ExecuteReader();

                        if (r.Read())
                        {
                            st = new DailyTrainStationTime();

                            st.ID = r.GetInt64(0);
                            st.TrainNo = r.GetString(1);
                            if (!r.IsDBNull(2))
                                st.ScheduleNo = r.GetString(2);
                            if (!r.IsDBNull(3))
                                st.LineID = r.GetInt16(3);
                            if (!r.IsDBNull(4))
                                st.BoundId = r.GetInt16(4);
                            st.PlatformId = r.GetInt16(5);
                            st.Time = r.GetDateTime(6);
                            st.Type = r.GetString(7);

                        }
                        r.Close();
                    }
                    conn.Close();
                }
                if (st == null)
                {
                    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                    {
                        conn.Open();
                        string cmdText = @"select pst.[ID], pst.TrainNo, pst.ScheduleNo, pst.LineID, pst.BoundID, pst.[PlatformID],pst.[Time],t.[Key] 
                                    from DailyTrainStationTime pst 
                                    inner join Platform plt1 on pst.platformID=plt1.ID
                                    inner join Platform plt2 on plt1.stationid = plt2.stationid
                                    inner join Station stn on plt1.stationid=stn.id
                                    inner join Systemlookup t on pst.[Type]=t.ID 
                                    where pst.TrainNo = @trainNo and plt2.[code]=@platform and pst.[time] >= @startTime and pst.[time] <= @endTime and t.[key]=@type ";


                        using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                        {
                            cmd.Parameters.AddWithValue("trainNo", trainno);
                            cmd.Parameters.AddWithValue("platform", platform);
                            cmd.Parameters.AddWithValue("startTime", startTime);
                            cmd.Parameters.AddWithValue("endTime", endTime);
                            cmd.Parameters.AddWithValue("type", type);
                            SqlDataReader r = cmd.ExecuteReader();

                            if (r.Read())
                            {
                                st = new DailyTrainStationTime();

                                st.ID = r.GetInt64(0);
                                st.TrainNo = r.GetString(1);
                                if (!r.IsDBNull(2))
                                    st.ScheduleNo = r.GetString(2);
                                if (!r.IsDBNull(3))
                                    st.LineID = r.GetInt16(3);
                                if (!r.IsDBNull(4))
                                    st.BoundId = r.GetInt16(4);
                                st.PlatformId = r.GetInt16(5);
                                st.Time = r.GetDateTime(6);
                                st.Type = r.GetString(7);

                            }
                            r.Close();
                        }
                        conn.Close();
                    }
                }

            }

            if (st != null)
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();
                    string cmdText = "Update DailyTrainStationTime set covered=1 where [id]= @id";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("id", st.ID);
                    cmd.ExecuteNonQuery();
                }
            }
            return st;
        }


        #endregion



        /// <summary>
        /// Get DailyTrainStationTime record by its id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DailyTrainStationTime GetDailyTrainStationTime(long id)
        {
            DailyTrainStationTime ST = null;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(@"select dst.TrainNo, dst.BoundID, pt.StationID,[time],stn.code, pt.[code]  from [dbo].[DailyTrainStationTime] dst
                                                    inner join [platform] pt on dst.PlatformID=pt.id 
                                                    inner join systemlookup s on dst.[type] = s.id
                                                    inner join station stn on stn.id=pt.stationid
                                                    where dst.ID=@id", conn);
                cmd.Parameters.AddWithValue("id", id);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    ST = new DailyTrainStationTime();
                    ST.ID = id;
                    ST.TrainNo = r.GetString(0);
                    ST.BoundId = r.GetInt16(1);
                    ST.StationID = r.GetInt16(2);
                    ST.Time = r.GetDateTime(3);
                    ST.Station = r.GetString(4);
                    ST.Platform = r.GetString(5);
                }

                r.Close();
                conn.Close();
            }
            return ST;
        }


        /// <summary>
        /// Archive Operation data
        /// </summary>
        public void ArchiveOperationData(DateTime oldDate)
        {
            int dateType = (new UtilityDL()).GetTimeTableDateTypeID(oldDate.ToString("yyyy-MM-dd"));
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string cmdText;
                SqlCommand cmd;

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    try
                    {
                        #region Move data from StnToStnTravelTimeToday to StnToStnTravelTime
                        Logging.log.Debug("Archiving StnToStnTravelTime");
                        cmdText = @"INSERT INTO [dbo].[StnToStnTravelTime]
                           ([FromPlat]
                           ,[ToPlat]
                           ,[ArrivalTime]
                           ,[TravelTime]
                           ,[DateType])
                           SELECT [FromPlat]
                           ,[ToPlat]
                           ,[ArrivalTime]
                           ,[TravelTime]
                           ,@dateType FROM StnToStnTravelTimeToday";
                        cmd = new SqlCommand(cmdText, conn, t);
                        cmd.Parameters.AddWithValue("@dateType", dateType);
                        cmd.ExecuteNonQuery();

                        cmdText = "TRUNCATE TABLE StnToStnTravelTimeToday";
                        cmd = new SqlCommand(cmdText, conn, t);
                        cmd.ExecuteNonQuery();
                        Logging.log.Debug("Done");
                        #endregion


                        t.Commit();
                    }
                    catch (SqlException ex1)
                    {
                        Logging.log.Debug("Unable to archive old data due to error: " + ex1.Message);
                        t.Rollback();
                        throw new Exception("Unable to archive old data due to error: " + ex1.Message);
                    }
                    catch (Exception ex)
                    {
                        Logging.log.Debug("Unable to archive old data due to error: " + ex.Message);
                        t.Rollback();
                        throw new Exception("Unable to archive old data due to error: " + ex.Message);
                    }
                    
                }

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    try
                    {
                        
                        #region Archive daily forecast data
                        Logging.log.Debug("Archiving DailyForecastedArrivalTime");
                        cmdText = @"INSERT INTO [dbo].[DailyForecastedArrivalTimeArchive]
                           ([ID]
                           ,[TrainNo]
                           ,[EmuNo]
                           ,[Time]
                           ,[PlannedStationTime]
                           ,[Platform]
                           ,[stn]
                           ,[bnd]
                           ,[OpsDate])
                           SELECT [ID]
                           ,[TrainNo]
                           ,[EmuNo]
                           ,[Time]
                           ,[PlannedStationTime]
                           ,[Platform]
                           ,[stn]
                           ,[bnd]
                           ,@opsDate FROM DailyForecastedArrivalTime";

                        Logging.log.Debug("opsDate: " + oldDate + "\n");

                        cmd = new SqlCommand(cmdText, conn, t);
                        cmd.CommandTimeout = 900000;
                        Logging.log.Debug("Demand created");
                        cmd.Parameters.AddWithValue("@opsDate", oldDate);
                        Logging.log.Debug("Parameter added");
                        cmd.ExecuteNonQuery();

                        Logging.log.Debug("Archiving done");

                        cmdText = "TRUNCATE TABLE DailyForecastedArrivalTime";
                        cmd = new SqlCommand(cmdText, conn, t);
                        cmd.ExecuteNonQuery();
                        Logging.log.Debug("Truncate Done");
                        #endregion
                        
                        t.Commit();
                    }
                    catch (SqlException ex1)
                    {
                        Logging.log.Debug("Unable to archive old data due to error: " + ex1.Message);
                        t.Rollback();
                        throw new Exception("Unable to archive old data due to error: " + ex1.Message);
                    }
                    catch (Exception ex)
                    {
                        Logging.log.Debug("Unable to archive old data due to error: " + ex.Message);
                        t.Rollback();
                        throw new Exception("Unable to archive old data due to error: " + ex.Message);
                    }
                }

                conn.Close();
            }

        }
    }

    public class CandidateEMU
    {
        public string emu;
        public string trainno;
        public short gap;
        public short stationid;
        public int timeDiff;
    }
}