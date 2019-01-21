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
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using System.Globalization;

namespace TTF.SetupDataJob
{
    class Program
    {
        public static DateTime runDate = DateTime.Now.Date;
     //   public static DateTime runDate = DateTime.Parse("2017-11-03");
        static void Main(string[] args)
        {

            //This job will retrieve the network data, lookup data and dailytrainstationtime from ATOMS for a specified day (defaulted to today)
            //runDate = new DateTime(2014, 12, 19);
            if (args.Length > 0)
            {
                runDate = DateTime.Parse(args[0]);
            }

       //     GenerateAccuracyReport();
            
            GetDataFromTrainload(runDate); 
            GetDataFromATOMS(runDate);
            InitiateForecastForNewDay();

            SendInitialForecastsToSQS();
            SendInitialTravelTimesToSQS();
        //    SendInitialLinkwayWalkTimesToSQS();

            
            
        }

        static void InitiateForecastForNewDay()
        {
            
                try
                {
                    var httpWebRequest1 = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["TTFWebServerURL"] + @"/TTFForecastManagement.svc/InitiateForecast");


                    httpWebRequest1.ContentType = "application/json; charset=utf-8";
                    httpWebRequest1.Method = "POST";
                    httpWebRequest1.Timeout = 1360000;
                    httpWebRequest1.ContentLength = 0;
                   
                    var httpResponse = (HttpWebResponse)httpWebRequest1.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                    }
                    Logging.log.Debug("Initiate forecast completed");
                    
                }
                catch (Exception ex)
                {
                    Logging.log.Debug("Initiate forecast failed",ex);
                }
            
        }
       
        static void GenerateAccuracyReport()
        {

            try
            {
                var httpWebRequest1 = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["TTFWebServerURL"] + @"/TTFForecastManagement.svc/GenAccuracyReport");


                httpWebRequest1.ContentType = "application/json; charset=utf-8";
                httpWebRequest1.Method = "POST";
                httpWebRequest1.Timeout = 1360000;
                httpWebRequest1.ContentLength = 0;

                var httpResponse = (HttpWebResponse)httpWebRequest1.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
                Logging.log.Debug("Initiate forecast completed");

            }
            catch (Exception ex)
            {
                Logging.log.Debug("Initiate forecast failed", ex);
            }

        }


        #region reprocess ATSS signal for simulation
        public static void ReprocessATSSLog(string fileName)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                fileName = "C:\\TTF\\atssnov20171120\\" + fileName;
                System.IO.StreamReader reader = new StreamReader(fileName);
                string txtLine = null;

                while ((txtLine = reader.ReadLine()) != null)
                {
                    if (txtLine.Trim().Length == 0)
                    {
                        continue;
                    }//end if line is empty

                    try
                    {
                        string timestamp = txtLine.Substring(0, 19);
                        string signal = txtLine.Substring(19);

                        string atssSignal = GetReadableATSSSignal(signal, timestamp);

                        DateTime time = DateTime.ParseExact(timestamp, "yyyy-MM-dd HH:mm:ss",
                                                                                   new CultureInfo("en-US"),
                                                                                   DateTimeStyles.None);

                        if (atssSignal != "")
                        {
                            string cmdText = "INSERT INTO atssinputlog([AtssSignal],[Timestamp]) " +
                            "VALUES (@input, @dt) SELECT SCOPE_IDENTITY() ";

                            SqlCommand cmd = new SqlCommand(cmdText, conn);
                            cmd.Parameters.AddWithValue("input", atssSignal);
                            cmd.Parameters.AddWithValue("dt", time);


                            SqlDataReader r = cmd.ExecuteReader();
                            if (r.Read())
                            {
                                int id = int.Parse(r[0].ToString());
                            }
                            r.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        int n = 0;
                    }


                    //if (!done)
                    //    break;
                }

                reader.Close();
            }
        }

        public static string GetReadableATSSSignal(string line, string timestamp)
        {

            string result = "";
            //Message format is
            // [SOM][Type][Message][BCC - Check byte][EOM]
            // [SOM] - 8D
            // [Type] - A for TrainPosition B for Train Consist
            // [BCC] - 1s compliment of XOR of all messgage bytes excluding SOM EOM and BCC

            // TrainPosition message format
            // [Number of trains][TrainNo][Timetable Deviation Time][Track Circuit ID]

            //line = line.Trim((char)141);

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

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

                                    string platformCode = "";
                                    string bound = "";
                                    string positionCode = "";

                                    using (SqlCommand com = new SqlCommand(@"SELECT PlatformCode,Bound,PositionCode FROM ATSSCircuitMapping WHERE CircuitCode=@CC
                                            ", conn))
                                    {
                                        com.Parameters.AddWithValue("CC", trackCircuit);
                                        SqlDataReader reader = com.ExecuteReader();

                                        if (reader.Read())
                                        {

                                            platformCode = reader[0].ToString();
                                            if (!reader.IsDBNull(1))
                                                bound = reader[1].ToString();
                                            if (!reader.IsDBNull(2))
                                                positionCode = reader[2].ToString();
                                        }
                                        reader.Close();
                                    }


                                    if (positionCode.ToUpper() == "PLATFORM")
                                    {
                                        result = "A," + timestamp + "," + trainNo + "," + deviation + "," + platformCode + "," + bound + ",ARR" + "," + trackCircuit;
                                    }
                                    else if (positionCode.ToUpper() == "AFTERPLATFORM")
                                    {
                                        result = "A," + timestamp + "," + trainNo + "," + deviation + "," + platformCode + "," + bound + ",DEP" + "," + trackCircuit;
                                    }
                                    else
                                    {
                                        //result = "A," + timestamp + "," + trainNo + "," + deviation + "," + cd.PlatformCode + "," + cd.Bound + ",DEP" + "," + trackCircuit;
                                    }
                                }
                                else
                                {
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
            }

            return result;
        }
        #endregion

        static void GetDataFromATOMS(DateTime runDate)
        {
            DataSet dsVersionTree = new DataSet();
            DataSet dsTrainLine = new DataSet();
            DataSet dsBound = new DataSet();
            DataSet dsStation = new DataSet();
            DataSet dsPlatform = new DataSet();
            DataSet dsInterchange = new DataSet();
            DataSet dsBoundStation = new DataSet();
            DataSet dsDateType = new DataSet();
            DataSet dsSystemLookup = new DataSet();
            DataSet dsATSSCircuitMapping = new DataSet();
            DataSet dsDailyTrainStationTime = new DataSet();

            try
            {
                Logging.log.Debug("Starting to retrieve data from ATOMS");
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ATOMSDB"].ConnectionString))
                {

                    conn.Open();
                    Logging.log.Debug("Starting to retrieve effective version");
                    #region Find the effective network version from ATOMS
                    int effectiveNetworkVersion = 0;
                    //Get the effective Network Version
                    using (SqlCommand cmd = new SqlCommand(@"
                            SELECT TOP 1 [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], 
                            [UpdatedDate],[Version],EffectiveFrom, EffectiveTo FROM [VersionTree] 
                            where [type]='NetworkSettings' and [Valid]=1 
                            and effectiveto is null and effectivefrom is not null and effectivefrom <= @RunDate order by EffectiveFrom desc, createddate desc", conn))
                    {

                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("RunDate", runDate);

                        SqlDataReader r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            effectiveNetworkVersion = r.GetInt32(0);
                        }
                        r.Close();
                    }

                    if (effectiveNetworkVersion == 0)
                    {
                        //Log error and exit
                        Logging.log.Error("No Network version found for date " + runDate);
                        return;
                    }
                    #endregion
                    Logging.log.Debug("Starting to retrieve versioned data");
                    #region Get Versioned data

                    using (SqlCommand cmd = new SqlCommand(@"SELECT [ID]
                      ,[ParentID]
                      ,[Type]
                      ,[Remark]
                      ,[CreatedBy]
                      ,[CreatedDate]
                      ,[UpdatedBy]
                      ,[UpdatedDate]
                      ,[Name]
                      ,[version]
                      ,[EffectiveFrom]
                      ,[EffectiveTo]
                      ,[Valid] FROM VersionTree WHERE ID =@EffectiveVersion", conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("EffectiveVersion", effectiveNetworkVersion);

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsVersionTree);
                        }
                    }
                 
                    using (SqlCommand cmd = new SqlCommand(@"SELECT [ID]
                      ,[Code]
                      ,[Description]
                      ,[Remark]
                      ,[VersionID] FROM TrainLine WHERE VersionID =@EffectiveVersion", conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("EffectiveVersion", effectiveNetworkVersion);

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsTrainLine);
                        }
                    }
                   
                    using (SqlCommand cmd = new SqlCommand(@"SELECT  [ID]
                      ,[LineID]
                      ,[Code]
                      ,[Description]
                      ,[FromStation]
                      ,[ToStation]
                      ,[VersionID]
                      ,[BoundType] FROM Bound WHERE VersionID =@EffectiveVersion", conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("EffectiveVersion", effectiveNetworkVersion);

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsBound);
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(@"SELECT   stn.[ID]
                      ,[Name]
                      ,stn.[Code] as Code
                      ,[InterchangeID]
                      ,[CrewpointID]
                      ,tl.Code as LineCode
                      ,syl.[Key] as Type
                      ,[StationNo]
                      ,stn.[VersionID]
                      ,[IsCrewpoint]
                      ,[IsTerminal] FROM Station stn
					  inner join SystemLookUp syl on stn.Type = syl.id
					  inner join trainline tl on stn.LineID = tl.id WHERE stn.VersionID =@EffectiveVersion", conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("EffectiveVersion", effectiveNetworkVersion);

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsStation);
                        }
                    }

                   
                    using (SqlCommand cmd = new SqlCommand(@"SELECT  [ID]
                      ,[Code]
                      ,[StationID]
                      ,[Serviceable]
                      ,[VersionID] FROM Platform WHERE VersionID =@EffectiveVersion", conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("EffectiveVersion", effectiveNetworkVersion);

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsPlatform);
                        }
                    }

                     
                    using (SqlCommand cmd = new SqlCommand(@"SELECT  [ID]
                      ,[Code]
                      ,[Description]
                      ,[VersionID] FROM Interchange WHERE VersionID =@EffectiveVersion", conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("EffectiveVersion", effectiveNetworkVersion);

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsInterchange );
                        }
                    }


                    using (SqlCommand cmd = new SqlCommand(@"select bs.ID as ID, bnd.code as bnd, stn.code as stn, 
tl.code as line, bs.position as position from BoundStation bs
inner join bound bnd on bs.BoundID = bnd.id
inner join station stn on bs.StationID = stn.id
inner join trainline tl on bnd.LineID = tl.id where bs.VersionID = @EffectiveVersion 
order by bs.BoundID, Position", conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("EffectiveVersion", effectiveNetworkVersion);

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsBoundStation );
                        }
                    }
                     

                    #endregion
                    Logging.log.Debug("Starting to retrieve unversioned lookup data");
                    #region Get unversioned lookup data

                    
                    using (SqlCommand cmd = new SqlCommand(@"SELECT [ID]
                      ,[Code]
                      ,[IncludeMon]
                      ,[IncludeTue]
                      ,[IncludeWed]
                      ,[IncludeThu]
                      ,[IncludeFri]
                      ,[IncludeSat]
                      ,[IncludeSun]
                      ,[IsSpecial] FROM DateType", conn))
                    {
                        cmd.CommandTimeout = 0;
                       
                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsDateType);
                        }
                    }


                    
                    using (SqlCommand cmd = new SqlCommand(@"SELECT [ID]
                      ,[Key]
                      ,[Value]
                      ,[Type]
                      ,[ModifiedBy]
                      ,[ModifiedDate] FROM SystemLookup", conn))
                    {
                        cmd.CommandTimeout = 0;

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsSystemLookup);
                        }
                    }
                    
                    using (SqlCommand cmd = new SqlCommand(@"SELECT [ID]
                  ,[PlatformCode]
                  ,[Type]
                  ,[CircuitCode]
                  ,[Bound]
                  ,[PositionCode] FROM ATSSCircuitMapping", conn))
                    {
                        cmd.CommandTimeout = 0;

                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsATSSCircuitMapping);
                        }
                    }


                    #endregion
                    Logging.log.Debug("Starting to retrieve dailytrainstationtime");
                    #region DailyTrainStationTime

                    using (SqlCommand cmd = new SqlCommand(@"select dst.id, dst.TrainNo, dst.scheduleno, dst.[time], 0 as covered, dst.OpsDate, tl.Code as line, b.Code as bnd, 
plt.Code as plt, stn.Code as stn, sl.[key] as type  from DailyTrainStationTime dst with (nolock)
inner join trainline tl on dst.LineID = tl.ID
inner join bound b on dst.BoundID = b.id
inner join Platform plt on dst.PlatformID = plt.ID
inner join station stn on plt.StationID = stn.id 
inner join SystemLookUp sl on dst.Type = sl.ID
inner join SystemLookUp sl2 on b.BoundType = sl2.id
where  sl2.[key]='Normal' and sl.[key]='ARR'", conn))

                    //OpsDate = @RunDate and
                    {
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("RunDate", runDate);
                        using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                        {
                            ad.Fill(dsDailyTrainStationTime);
                        }
                        
                    }

                    #endregion

                }
                Logging.log.Debug(dsDailyTrainStationTime.Tables[0].Rows.Count + " rows found for daily train station time for rundate " +  runDate.ToString("yyyy-MM-dd"));
                Logging.log.Debug("Finished retrieving data from ATOMS");
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {

                    conn.Open();
                    using (SqlTransaction t = conn.BeginTransaction())
                    {
                        #region Truncate and insert versioned data

                        #region Truncate tables
                        using (SqlCommand cmd = new SqlCommand(@"
                           TRUNCATE TABLE DailyActualStationTime", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                           DELETE FROM  DailyTrainStationTime", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                           DELETE FROM  LinkwayWalkTime", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                           TRUNCATE TABLE Trainline", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }


                        using (SqlCommand cmd = new SqlCommand(@"
                          DELETE FROM  Bound", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();

                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           DELETE FROM BoundStation", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }


                        using (SqlCommand cmd = new SqlCommand(@"
                           DELETE FROM Platform", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                           DELETE FROM  Station", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                       

                        
                        using (SqlCommand cmd = new SqlCommand(@"
                          DELETE FROM  Interchange", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                           DELETE FROM  VersionTree", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }

                        #endregion

                        #region Populate VersionTree
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT VersionTree ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsVersionTree.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO VersionTree([ID]
                          ,[ParentID]
                          ,[Type]
                          ,[Remark]
                          ,[CreatedBy]
                          ,[CreatedDate]
                          ,[UpdatedBy]
                          ,[UpdatedDate]
                          ,[Name]
                          ,[version]
                          ,[EffectiveFrom]
                          ,[EffectiveTo]
                          ,[Valid]) VALUES(@ID
                          ,@ParentID
                          ,@Type
                          ,@Remark
                          ,@CreatedBy
                          ,@CreatedDate
                          ,@UpdatedBy
                          ,@UpdatedDate
                          ,@Name
                          ,@version
                          ,@EffectiveFrom
                          ,@EffectiveTo
                          ,@Valid) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID",r["ID"]);
                                cmd.Parameters.AddWithValue("@ParentID",r["ParentID"]);
                                cmd.Parameters.AddWithValue("@Type", r["Type"]);
                                cmd.Parameters.AddWithValue("@Remark",r["Remark"]);
                                cmd.Parameters.AddWithValue("@CreatedBy",r["CreatedBy"]);
                                cmd.Parameters.AddWithValue("@CreatedDate",r["CreatedDate"]);
                                cmd.Parameters.AddWithValue("@UpdatedBy",r["UpdatedBy"]);
                                cmd.Parameters.AddWithValue("@UpdatedDate",r["UpdatedDate"]);
                                cmd.Parameters.AddWithValue("@Name",r["Name"]);
                                cmd.Parameters.AddWithValue("@version",r["version"]);
                                cmd.Parameters.AddWithValue("@EffectiveFrom",r["EffectiveFrom"]);
                                cmd.Parameters.AddWithValue("@EffectiveTo",r["EffectiveTo"]);
                                cmd.Parameters.AddWithValue("@Valid",r["Valid"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT VersionTree OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate Trainline
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Trainline ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsTrainLine.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO Trainline([ID]
                      ,[Code]
                      ,[Description]
                      ,[Remark]
                      ,[VersionID]) VALUES(@ID
                          ,@Code
                          ,@Description
                          ,@Remark
                          ,@VersionID) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@Code", r["Code"]);
                                cmd.Parameters.AddWithValue("@Description", r["Description"]);
                                cmd.Parameters.AddWithValue("@Remark", r["Remark"]);
                                cmd.Parameters.AddWithValue("@VersionID", r["VersionID"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Trainline OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate Interchange
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Interchange ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsInterchange.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO Interchange([ID]
                      ,[Code]
                      ,[Description]
                      ,[VersionID] ) VALUES(@ID
                      ,@Code
                      ,@Description
                      ,@VersionID ) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@Code", r["Code"]);
                                cmd.Parameters.AddWithValue("@Description", r["Description"]);
                                cmd.Parameters.AddWithValue("@VersionID", r["VersionID"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Interchange OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate Station
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Station ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsStation.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO Station([ID]
                      ,[Name]
                      ,[Code]
                      ,[InterchangeID]
                      ,[CrewpointID]
                      ,[LineCode]
                      ,[Type]
                      ,[StationNo]
                      ,[VersionID]
                      ,[IsCrewpoint]
                      ,[IsTerminal]) VALUES(@ID
                      ,@Name
                      ,@Code
                      ,@InterchangeID
                      ,@CrewpointID
                      ,@LineCode
                      ,@Type
                      ,@StationNo
                      ,@VersionID
                      ,@IsCrewpoint
                      ,@IsTerminal) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@Name", r["Name"]);
                                cmd.Parameters.AddWithValue("@Code", r["Code"]);
                                cmd.Parameters.AddWithValue("@InterchangeID", r["InterchangeID"]);
                                cmd.Parameters.AddWithValue("@CrewpointID", r["CrewpointID"]);
                                cmd.Parameters.AddWithValue("@LineCode", r["LineCode"]);
                                cmd.Parameters.AddWithValue("@Type", r["Type"]);
                                cmd.Parameters.AddWithValue("@StationNo", r["StationNo"]);
                                cmd.Parameters.AddWithValue("@VersionID", r["VersionID"]);
                                cmd.Parameters.AddWithValue("@IsCrewpoint", r["IsCrewpoint"]);
                                cmd.Parameters.AddWithValue("@IsTerminal", r["IsTerminal"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Station OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate Bound
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Bound ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsBound.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO Bound([ID]
                      ,[LineID]
                      ,[Code]
                      ,[Description]
                      ,[FromStation]
                      ,[ToStation]
                      ,[VersionID]
                      ,[BoundType]) VALUES(@ID
                      ,@LineID
                      ,@Code
                      ,@Description
                      ,@FromStation
                      ,@ToStation
                      ,@VersionID
                      ,@BoundType) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@LineID", r["LineID"]);
                                cmd.Parameters.AddWithValue("@Code", r["Code"]);
                                cmd.Parameters.AddWithValue("@Description", r["Description"]);
                                cmd.Parameters.AddWithValue("@FromStation", r["FromStation"]);
                                cmd.Parameters.AddWithValue("@ToStation", r["ToStation"]);
                                cmd.Parameters.AddWithValue("@VersionID", r["VersionID"]);
                                cmd.Parameters.AddWithValue("@BoundType", r["BoundType"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Bound OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate Platform
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Platform ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsPlatform.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO Platform([ID]
                      ,[Code]
                      ,[StationID]
                      ,[Serviceable]
                      ,[VersionID]) VALUES(@ID
                      ,@Code
                      ,@StationID
                      ,@Serviceable
                      ,@VersionID) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                string platform = ((string)r["Code"]).Replace(" ", String.Empty);
                                cmd.Parameters.AddWithValue("@Code", platform);
                                cmd.Parameters.AddWithValue("@StationID", r["StationID"]);
                                cmd.Parameters.AddWithValue("@Serviceable", r["Serviceable"]);
                                cmd.Parameters.AddWithValue("@VersionID", r["VersionID"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT Platform OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate BoundStation
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT BoundStation ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsBoundStation.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO BoundStation([ID]
                  ,[bnd]
                  ,[stn]
                  ,[line]
                  ,[Position]) VALUES(@ID
                  ,@bnd
                  ,@stn
                  ,@line
                  ,@Position) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@bnd", r["bnd"]);
                                cmd.Parameters.AddWithValue("@stn", r["stn"]);
                                cmd.Parameters.AddWithValue("@line", r["line"]);
                                cmd.Parameters.AddWithValue("@Position", r["position"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT BoundStation OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion
                        #endregion

                        #region Truncate and insert lookup data
                        #region Truncate tables
                        using (SqlCommand cmd = new SqlCommand(@"
                           TRUNCATE TABLE DateType ", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           TRUNCATE TABLE ATSSCircuitMapping ", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           TRUNCATE TABLE SystemLookup", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate DateType
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT DateType ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsDateType.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO DateType( [ID]
                      ,[Code]
                      ,[IncludeMon]
                      ,[IncludeTue]
                      ,[IncludeWed]
                      ,[IncludeThu]
                      ,[IncludeFri]
                      ,[IncludeSat]
                      ,[IncludeSun]
                      ,[IsSpecial]) VALUES( @ID
                      ,@Code
                      ,@IncludeMon
                      ,@IncludeTue
                      ,@IncludeWed
                      ,@IncludeThu
                      ,@IncludeFri
                      ,@IncludeSat
                      ,@IncludeSun
                      ,@IsSpecial) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@Code", r["Code"]);
                                cmd.Parameters.AddWithValue("@IncludeMon", r["IncludeMon"]);
                                cmd.Parameters.AddWithValue("@IncludeTue", r["IncludeTue"]);
                                cmd.Parameters.AddWithValue("@IncludeWed", r["IncludeWed"]);
                                cmd.Parameters.AddWithValue("@IncludeThu", r["IncludeThu"]);
                                cmd.Parameters.AddWithValue("@IncludeFri", r["IncludeFri"]);
                                cmd.Parameters.AddWithValue("@IncludeSat", r["IncludeSat"]);
                                cmd.Parameters.AddWithValue("@IncludeSun", r["IncludeSun"]);
                                cmd.Parameters.AddWithValue("@IsSpecial", r["IsSpecial"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT DateType OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate SystemLookup
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT SystemLookup ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsSystemLookup.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO SystemLookup( [ID]
                      ,[Key]
                      ,[Value]
                      ,[Type]
                      ,[ModifiedBy]
                      ,[ModifiedDate]) VALUES( @ID
                      ,@Key
                      ,@Value
                      ,@Type
                      ,@ModifiedBy
                      ,@ModifiedDate) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@Key", r["Key"]);
                                cmd.Parameters.AddWithValue("@Value", r["Value"]);
                                cmd.Parameters.AddWithValue("@Type", r["Type"]);
                                cmd.Parameters.AddWithValue("@ModifiedBy", r["ModifiedBy"]);
                                cmd.Parameters.AddWithValue("@ModifiedDate", r["ModifiedDate"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT SystemLookup OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #region Populate ATSSCircuitMapping
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT ATSSCircuitMapping  ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsATSSCircuitMapping.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO ATSSCircuitMapping ( [ID]
                          ,[PlatformCode]
                          ,[Type]
                          ,[CircuitCode]
                          ,[Bound]
                          ,[PositionCode]) VALUES(  @ID
                          ,@PlatformCode
                          ,@Type
                          ,@CircuitCode
                          ,@Bound
                          ,@PositionCode) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@PlatformCode", r["PlatformCode"]);
                                cmd.Parameters.AddWithValue("@Type", r["Type"]);
                                cmd.Parameters.AddWithValue("@CircuitCode", r["CircuitCode"]);
                                cmd.Parameters.AddWithValue("@Bound", r["Bound"]);
                                cmd.Parameters.AddWithValue("@PositionCode", r["PositionCode"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT ATSSCircuitMapping  OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion

                        #endregion

                        #region Truncate and insert Daily data

                        #region Truncate tables
                        using (SqlCommand cmd = new SqlCommand(@"
                          TRUNCATE TABLE DailyTrainStationTime  ", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion


                        #region Populate DailyTrainStationTime
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT DailyTrainStationTime  ON;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        foreach (DataRow r in dsDailyTrainStationTime.Tables[0].Rows)
                        {

                            using (SqlCommand cmd = new SqlCommand(@"
                           INSERT INTO DailyTrainStationTime (  [ID]
                      ,[TrainNo]
                      ,[ScheduleNo]
                      ,[line]
                      ,[bnd]
                      ,[plt]
                      ,[Time]
                      ,[Type]
                      ,[Covered]
                      ,[OpsDate],
                        [stn]) VALUES(  @ID
                      ,@TrainNo
                      ,@ScheduleNo
                      ,@line
                      ,@bnd
                      ,@plt
                      ,@Time
                      ,@Type
                      ,@Covered
                      ,@OpsDate
                      ,@stn) ", conn, t))
                            {

                                cmd.CommandTimeout = 0;
                                cmd.Parameters.AddWithValue("@ID", r["ID"]);
                                cmd.Parameters.AddWithValue("@TrainNo", r["TrainNo"]);
                                cmd.Parameters.AddWithValue("@ScheduleNo", r["ScheduleNo"]);
                                cmd.Parameters.AddWithValue("@line", r["line"]);
                                cmd.Parameters.AddWithValue("@bnd", r["bnd"]);
                                string platform = ((string)r["plt"]).Replace(" ", String.Empty);
                                cmd.Parameters.AddWithValue("@plt", platform);
                                cmd.Parameters.AddWithValue("@Time", r["Time"]);
                                cmd.Parameters.AddWithValue("@Type", r["Type"]);
                                cmd.Parameters.AddWithValue("@Covered", r["Covered"]);
                                cmd.Parameters.AddWithValue("@OpsDate", r["OpsDate"]);
                                cmd.Parameters.AddWithValue("@stn", r["stn"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand(@"
                           SET IDENTITY_INSERT DailyTrainStationTime  OFF;", conn, t))
                        {

                            cmd.CommandTimeout = 0;
                            cmd.ExecuteNonQuery();
                        }
                        #endregion
                        #endregion


                        t.Commit();
                    }
                    
                }
                Logging.log.Debug("Finished saving data to TTF");

            }
            catch (Exception ex)
            {
                    Logging.log.Debug("Error retrieving data from ATOMS", ex);
            }
        }

        static string GetDateType(DateTime runDate)
        {
            if (runDate.DayOfWeek == DayOfWeek.Friday)
            {
                return "FRIDAY";
            }
            if (runDate.DayOfWeek == DayOfWeek.Saturday)
            {
                return "SAT";
            }
            if (runDate.DayOfWeek == DayOfWeek.Saturday)
            {
                return "SUN";
            }
            return "WKDAY";
        }
        static void GetDataFromTrainload(DateTime runDate)
        {
            DataSet dsWalkwayData = new DataSet();

            List<LinkwayWalkTime> walkTimes = new List<LinkwayWalkTime>();

            try
            {
                Logging.log.Debug("Starting to retrieve data from Trainload");
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TrainloadDB"].ConnectionString))
                {

                    conn.Open();

                    #region Get Linkway walktime data

                    using (SqlCommand cmd = new SqlCommand(@" SELECT * FROM (
SELECT 
  S1.Code AS Station,L1.Code AS FromLine,L2.Code AS ToLine,TS.StartTime AS FromTime,TS.EndTime AS ToTime,Param2.Value  AS Walkingtime
  ,ROW_NUMBER() over (partition by S1.Code,L1.Code,L2.Code,TS.StartTime,TS.EndTime,TS.DateType,Param2.Value
order by Param2.Value DESC) AS RowNum
FROM Parameter2D Param2
INNER JOIN Platform p1 ON Param2.ObjectId1= P1.ID
INNER JOIN Platform p2 ON Param2.ObjectId2= P2.ID
INNER JOIN Station S1 on S1.ID = P1.StationNo
INNER JOIN RailLine L1 ON L1.ID = S1.[LineNo]
INNER JOIN Station S2 on S2.ID = P2.StationNo
INNER JOIN RailLine L2 ON L2.ID = s2.[LineNo]
INNER JOIN TimeSlot TS ON TS.ID = Param2.TimeSlot
INNER JOIN DataType DT ON DT.ID = TS.DateType
WHERE Param2.name='Link way walk time'
AND L1.VersionID = (SELECT MAX(ID) FROM VersionTree WHERE FunctionID=155)
AND L2.VersionID = (SELECT MAX(ID) FROM VersionTree WHERE FunctionID=155)
AND L1.Code<>L2.Code
AND DT.Code=@DateType
)DATA WHERE RowNum=1", conn))
                    {
                        cmd.CommandTimeout = 0;
                       cmd.Parameters.AddWithValue("DateType",GetDateType(runDate));

                       
                       SqlDataReader r = cmd.ExecuteReader();
                       while (r.Read())
                       {
                           LinkwayWalkTime wt = new LinkwayWalkTime();
             //              Logging.log.Debug("Walktime: " + r.GetString(5));
                           if (!r.IsDBNull(0))
                               wt.Station = r.GetString(0);
                           if (!r.IsDBNull(1))
                               wt.FromLine = r.GetString(1);
                           if (!r.IsDBNull(2))
                               wt.ToLine = r.GetString(2);
                           if (!r.IsDBNull(3))
                               wt.FromTime = r.GetDateTime(3).ToString("HH:mm:ss");
                           if (!r.IsDBNull(4))
                               wt.ToTime = r.GetDateTime(4).ToString("HH:mm:ss");
                           if (!r.IsDBNull(5))
                           {
                               string time = r.GetString(5);
                               wt.WalkTime = Convert.ToInt32(time);
                           }
                           
                           walkTimes.Add(wt);
                       }
                       r.Close();

                    }



                    #endregion
                  

                }
                Logging.log.Debug("Finished retrieving data from Trainload");

                try
                {
             //       Logging.log.Debug(walkTimes.Count + " walk times found");
                    List<LinkwayWalkTime> tempList = new List<LinkwayWalkTime>();
                    for (int i = 0; i < walkTimes.Count; i++)
                    {
                        if (((i % 1000) == 0) && (i > 0))
                        {
                            SendMessages(tempList);
                            tempList = new List<LinkwayWalkTime>();
                        }
                        else
                        {
                            tempList.Add(walkTimes[i]);
                        }
                    }
                    if (tempList.Count > 0)
                        SendMessages(tempList);
                }
                catch (Exception ex)
                {
                    Logging.log.Error("Error sending walkway times to SQS", ex);
                }

                Logging.log.Debug("Finished saving data to TTF");

            }
            catch (Exception ex)
            {
                Logging.log.Debug("Error retrieving data from Trainload", ex);
            }
        }

        #region Send Forecasts to SQS

        static void SendInitialForecastsToSQS()
        {
            // preparing sqs client

            try
            {

                List<ForecastedTrainArrivalTime> initialForecasts = GetInitialForecasts();
                Logging.log.Debug(initialForecasts.Count + " forecasts found");
                List<ForecastedTrainArrivalTime> tempList = new List<ForecastedTrainArrivalTime>();
                for (int i = 0; i < initialForecasts.Count; i++)
                {
                    if (((i % 1000) == 0) && (i > 0))
                    {
                        SendMessages(tempList);
                        tempList = new List<ForecastedTrainArrivalTime>();
                    }
                    else
                    {
                        tempList.Add(initialForecasts[i]);
                    }
                }
                if (tempList.Count > 0)
                    SendMessages(tempList);
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error sending forecasts to SQS", ex);
            }
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

          //  Logging.log.Debug("Sending message: \n" + req.MessageBody);
            Logging.log.Debug("Sending message: \n");

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
        private static List<ForecastedTrainArrivalTime> GetInitialForecasts()
        {
            List<ForecastedTrainArrivalTime> forecasts = new List<ForecastedTrainArrivalTime>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                conn.Open();
                using (SqlCommand c = new SqlCommand(@"SELECT DFAT.TrainNo,DFAT.EMUNo,DFAT.Time,DFAT.PlannedStationTime,DFAT.bnd,DFAT.Platform FROM DailyForecastedArrivalTime DFAT ", conn))
                {
                    SqlDataReader r = c.ExecuteReader();
                    while (r.Read())
                    {
                        ForecastedTrainArrivalTime f = new ForecastedTrainArrivalTime();
                        f.TrainNo = r.GetString(0);
                        if (!r.IsDBNull(1))
                            f.EmuNo = r.GetString(1);
                        if (!r.IsDBNull(2))
                            f.Time = r.GetDateTime(2);
                        if (!r.IsDBNull(3))
                            f.PlannedST = r.GetInt64(3);
                        if (!r.IsDBNull(4))
                            f.Bound = r.GetString(4);
                        if (!r.IsDBNull(5))
                            f.Platform = r.GetString(5);
                        forecasts.Add(f);
                    }
                    r.Close();
                }

            }
            return forecasts;
        }


        #endregion

        #region Send Travel Times to SQS

        
        private static List<AvgStnToStnTravelTime> GetStnToStnTravelTimes()
        {
            List<AvgStnToStnTravelTime> travelTimes = new List<AvgStnToStnTravelTime>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                conn.Open();
                using (SqlCommand c = new SqlCommand(@"SELECT  ASTS.FromPlat,ASTS.ToPlat,ASTS.FromTime,ASTS.ToTime,ASTS.TravelTime FROM [AvgStnToStnTravelTime] ASTS 
", conn))
                {
                    SqlDataReader r = c.ExecuteReader();
                    while (r.Read())
                    {
                        AvgStnToStnTravelTime tt = new AvgStnToStnTravelTime();
                        if (!r.IsDBNull(0))
                            tt.FromPlat = r.GetString(0);
                        if (!r.IsDBNull(1))
                            tt.ToPlat = r.GetString(1);
                        if (!r.IsDBNull(2))
                            tt.FromTime = r[2].ToString().Substring(0, 8) ;
                        if (!r.IsDBNull(3))
                            tt.ToTime = r[3].ToString().Substring(0, 8);
                        if (!r.IsDBNull(4))
                            tt.TravelTime = r.GetInt32(4);

                        travelTimes.Add(tt);
                    }
                    r.Close();
                }

            }
            return travelTimes;
        }

        static void SendInitialTravelTimesToSQS()
        {
            // preparing sqs client

            try
            {

                List<AvgStnToStnTravelTime> travelTimes = GetStnToStnTravelTimes();
                Logging.log.Debug(travelTimes.Count + " travel times found");
                List<AvgStnToStnTravelTime> tempList = new List<AvgStnToStnTravelTime>();
                for (int i = 0; i < travelTimes.Count; i++)
                {
                    if (((i % 1000) == 0) && (i > 0))
                    {
                        SendMessages(tempList);
                        tempList = new List<AvgStnToStnTravelTime>();
                    }
                    else
                    {
                        tempList.Add(travelTimes[i]);
                    }
                }
                if (tempList.Count > 0)
                    SendMessages(tempList);
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error sending forecasts to SQS", ex);
            }
        }
        private static void SendMessages(List<AvgStnToStnTravelTime> messages)
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
        private static string GetMessageBody(List<AvgStnToStnTravelTime> items)
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
            foreach (AvgStnToStnTravelTime f in items)
            {
                list.Add(new Dictionary<string, object> {
                { "type", "AvgStnToStnTravelTime" },
                { "data", f }
            });
            }

            return JsonConvert.SerializeObject(list);
        }
        #endregion

        #region Send LinkWalkwayTimes to SQS


        private static List<LinkwayWalkTime> GetLinkwayWalkTimes()
        {
            List<LinkwayWalkTime> walkTimes = new List<LinkwayWalkTime>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                conn.Open();
                using (SqlCommand c = new SqlCommand(@"SELECT 
Station,FromLine,ToLine,FromTime,ToTime,walkingTime
 FROM WalkTime 
", conn))
                {
                    SqlDataReader r = c.ExecuteReader();
                    while (r.Read())
                    {
                        LinkwayWalkTime wt = new LinkwayWalkTime();
                        if (!r.IsDBNull(0))
                            wt.Station = r.GetString(0);
                        if (!r.IsDBNull(1))
                            wt.FromLine = r.GetString(1);
                        if (!r.IsDBNull(2))
                            wt.ToLine = r.GetString(2);
                        if (!r.IsDBNull(3))
                            wt.FromTime = r.GetDateTime(3).ToString("HH:mm:ss");
                        if (!r.IsDBNull(4))
                            wt.ToTime = r.GetDateTime(4).ToString("HH:mm:ss");
                        if (!r.IsDBNull(5))
                            wt.WalkTime = r.GetInt32(5);

                        walkTimes.Add(wt);
                    }
                    r.Close();
                }

            }
            return walkTimes;
        }

        static void SendInitialLinkwayWalkTimesToSQS()
        {
            // preparing sqs client

            try
            {

                List<LinkwayWalkTime> walkTimes = GetLinkwayWalkTimes();
                Logging.log.Debug(walkTimes.Count + " walk times found");
                List<LinkwayWalkTime> tempList = new List<LinkwayWalkTime>();
                for (int i = 0; i < walkTimes.Count; i++)
                {
                    if (((i % 1000) == 0) && (i > 0))
                    {
                        SendMessages(tempList);
                        tempList = new List<LinkwayWalkTime>();
                    }
                    else
                    {
                        tempList.Add(walkTimes[i]);
                    }
                }
                if (tempList.Count > 0)
                    SendMessages(tempList);
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error sending walkway times to SQS", ex);
            }
        }
        private static void SendMessages(List<LinkwayWalkTime> messages)
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
        private static string GetMessageBody(List<LinkwayWalkTime> items)
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
            foreach (LinkwayWalkTime f in items)
            {
                list.Add(new Dictionary<string, object> {
                { "type", "LinkwayWalkTime" },
                { "data", f }
            });
            }

            return JsonConvert.SerializeObject(list);
        }
        #endregion


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
    }
}
