///
///<file>NetworkManagementDL.cs</file>
///<description>
///NetworkManagementDL is the class that is the data access layer of all network settings
///</description>
///

///
///<created>
///<author>Suraj</author>
///<date>17-11-2013</date>
///</created>
///
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using TTF.Utils;
using System.Data;
using System.Configuration;
using TTF.Models;
using TTF.Models.NetworkManagement;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Gma.QrCodeNet.Encoding.Windows.Render;
using Gma.QrCodeNet.Encoding;
using TTF.BusinessLogic;

namespace TTF.DataLayer
{
    public class NetworkManagementDL
    {

        #region Line Management Functions

        /// <summary>
        /// Get all the train lines from the database
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>List of train line object</returns>
        public List<Line> GetLinesDetails(string verId)
        {
            List<Line> lines = new List<Line>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT tl.id Id, tl.code Code,tl.description Description  FROM TrainLine tl WHERE VersionId = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Line l = new Line();
                        l.Name = r["Code"].ToString();
                        l.ID = Convert.ToInt16(r["id"]);
                        l.Description = r["Description"].ToString();
                        l.BoundDetails = new List<Bound>();
                        l.BoundDetails = GetBoundList(Convert.ToInt16(r["id"]), verId);
                        lines.Add(l);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return lines;
        }
        /// <summary>
        /// Get all the train lines from the database with basic info
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>List of train line object</returns>
        public List<LineBasic> GetLinesBasicDetails(string verId)
        {
            List<LineBasic> lines = new List<LineBasic>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT tl.id Id, tl.code Code,tl.description Description  FROM TrainLine tl WHERE VersionId = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        LineBasic l = new LineBasic();
                        l.Name = r["Code"].ToString();
                        l.ID = Convert.ToInt16(r["id"]);
                        l.Description = r["Description"].ToString();

                        lines.Add(l);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return lines;
        }
        /// <summary>
        /// Get list of bounds
        /// </summary>
        /// <param name="lnId">Line Id</param>
        /// <param name="verId">Version Id</param>
        /// <returns>List of bound object</returns>
        private List<Bound> GetBoundList(int lnId, string verId)
        {
            List<Bound> blst = new List<Bound>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                string query = @"SELECT   b.code Bound,
                                    Details = STUFF((SELECT ' > ' + s.Code  FROM Bound b2  
                                    INNER JOIN Station s ON s.LineID = b2.LineID INNER JOIN BoundStation bs ON bs.StationID =s.ID AND bs.BoundID = b2.ID WHERE b2.id = b.ID ORDER BY bs.Position
                                    FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'), 2, 2, '')
                                    FROM bound b 
                                    WHERE b.LineID = @lnId AND b.VersionId = @verId ";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@lnId", lnId);
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Bound b = new Bound();
                        b.Code = r["Bound"].ToString();
                        b.StationDetails = r["Details"].ToString();
                        blst.Add(b);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return blst;
        }

        /// <summary>
        /// Get the list of Stations (Currently not used)
        /// </summary>
        /// <param name="bndId">Bound Id</param>
        /// <param name="verId">Version Id</param>
        /// <returns>Station name</returns>
        private string GetStationList(short bndId, string verId)
        {
            string stnLst = string.Empty;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                //string query = "SELECT   s.Code  FROM Bound b INNER JOIN Station s ON s.LineID = b.LineID INNER JOIN BoundStation bs ON bs.StationID =s.ID AND bs.BoundID = b.ID WHERE b.id = @bndId AND s.VersionId = @verId  ORDER BY  bs.Position";
                string query = "SELECT   s.Code  FROM Bound b INNER JOIN Station s ON s.LineID = b.LineID INNER JOIN BoundStation bs ON bs.StationID =s.ID AND bs.BoundID = b.ID WHERE b.id = @bndId AND s.VersionId = @verId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@bndId", bndId);
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {
                        stnLst += r["Code"].ToString() + "->";
                    }

                    r.Close();
                }
                conn.Close();
            }
            if (stnLst.Trim().Length > 0)
            {
                return stnLst.Substring(0, stnLst.Length - 2);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Check whether the line code exists for the given Version
        /// </summary>
        /// <param name="lnCode">Line Code</param>
        /// <param name="verId">Version Id</param>
        /// <returns>true / false</returns>
        private bool CheckTrainCode(string lnCode, int verId)
        {

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM TrainLine WHERE Code LIKE @lnCode AND VersionID = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@lnCode", lnCode);
                    cmd.Parameters.AddWithValue("@verId", verId);
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
            }
        }

        /// <summary>
        /// To save train line data into the database
        /// </summary>
        /// <param name="lnCode">Line Code</param>
        /// <param name="lnDesc">Line description</param>
        /// <param name="verId">Version Id</param>
        /// <returns>returns Line Id</returns>
        public int AddTrainLine(string lnCode, string lnDesc, int verId)
        {
            int result = -1;

            if (!CheckTrainCode(lnCode, verId))
            {

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    string queryinsert = @"INSERT INTO TrainLine(Code,Description,VersionID)VALUES(@Code,@Desc,@verId) SELECT SCOPE_IDENTITY()";
                    SqlCommand cmd = new SqlCommand(queryinsert, conn);

                    cmd.Parameters.AddWithValue("Code", lnCode);
                    cmd.Parameters.AddWithValue("Desc", lnDesc);
                    cmd.Parameters.AddWithValue("verId", verId);

                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        result = int.Parse(r[0].ToString());
                    }

                    r.Close();
                    conn.Close();
                }
            }
            else
            {
                throw new System.ArgumentException("Line code already exists");
            }

            return result;
        }

        /// <summary>
        /// Get particular line data
        /// </summary>
        /// <param name="id">Line Id</param>
        /// <returns>Line object</returns>
        public Line GetLineData(short id)
        {
            Line ln = new Line();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand("SELECT tl.id Id, tl.code Code,tl.description Description  FROM TrainLine tl WHERE tl.id = @Id", conn))
                {
                    conn.Open();
                    SqlParameter p1 = new SqlParameter("Id", id);
                    cmd.Parameters.Add(p1);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {

                        ln.Name = r["Code"].ToString();
                        ln.ID = Convert.ToInt16(r["id"]);
                        ln.Description = r["Description"].ToString();
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return ln;
        }

        /// <summary>
        /// Delete particular line data from the table
        /// </summary>
        /// <param name="id">Line Id</param>
        public void DeleteLineData(short id)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                string queryinsert = @"DELETE FROM TrainLine WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(queryinsert, conn);

                SqlParameter p1 = new SqlParameter("Id", id);
                cmd.Parameters.Add(p1);

                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Update the particular line data 
        /// </summary>
        /// <param name="Id">Line Id</param>
        /// <param name="lnCode">Line Code</param>
        /// <param name="lnDesc">Line Description</param>
        /// <param name="verId">Version Id</param>
        public void EditLineData(short Id, string lnCode, string lnDesc, int verId)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string queryinsert = @"UPDATE TrainLine SET Code = @lnCode, Description = @lnDesc WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(queryinsert, conn);
                cmd.Parameters.AddWithValue("Id", Id);
                cmd.Parameters.AddWithValue("lnCode", lnCode);
                cmd.Parameters.AddWithValue("lnDesc", lnDesc);

                cmd.ExecuteNonQuery();
                conn.Close();
            }

        }
        #endregion

        #region Interchangemanagement

        /// <summary>
        /// Get the list of interchange codes from the database
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>List of interchange Object</returns>
        public List<Interchange> GetInterchangeList(string verId)
        {

            List<Interchange> icLst = new List<Interchange>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT  ID, Code, Description FROM  Interchange WHERE VersionId = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Interchange ic = new Interchange();

                        ic.ID = Convert.ToInt16(r["id"]);
                        ic.Code = r["Code"].ToString();
                        ic.Description = r["Description"].ToString();
                        ic.LineDetails = new List<Line>();
                        ic.LineDetails = GetTrainLineForIc(Convert.ToInt16(r["id"]), verId);
                        icLst.Add(ic);
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return icLst;
        }

        /// <summary>
        /// Get the train line list for the given interchange id
        /// </summary>
        /// <param name="icId">Interchange Id</param>
        /// <param name="verId">Version Id</param>
        /// <returns>List of train line object</returns>
        private List<Line> GetTrainLineForIc(short icId, string verId)
        {
            List<Line> lnLst = new List<Line>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT tl.Code, tl.[Description] FROM TrainLine tl INNER JOIN Station s ON s.LineID = tl.ID WHERE s.InterchangeID = @icId AND tl.VersionID = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@icId", icId);
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Line ln = new Line();

                        ln.Name = r["Code"].ToString();
                        ln.Description = r["Description"].ToString();

                        lnLst.Add(ln);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return lnLst;
        }
        /// <summary>
        /// To check whether the interchange code already exists or not for the given version
        /// </summary>
        /// <param name="icCode">Interchange Code</param>
        /// <param name="verId">Version Id</param>
        /// <returns>True or False</returns>
        private bool CheckInterchangeCode(string icCode, int verId)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Interchange WHERE Code LIKE @icCode AND VersionID = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@icCode", icCode);
                    cmd.Parameters.AddWithValue("@verId", verId);

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
            }
        }

        /// <summary>
        /// Save interchange details into the database
        /// </summary>
        /// <param name="icCode">Interchange Code</param>
        /// <param name="icDesc">Interchange Description</param>
        /// <param name="verId">Version Id</param>
        /// <returns>Id value</returns>
        public int AddInterchange(string icCode, string icDesc, int verId)
        {
            int result = -1;
            if (!CheckInterchangeCode(icCode, verId))
            {

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    string queryinsert = @"INSERT INTO Interchange(Code,Description,VersionID)VALUES(@Code,@Desc,@verId) SELECT SCOPE_IDENTITY()";
                    SqlCommand cmd = new SqlCommand(queryinsert, conn);

                    cmd.Parameters.AddWithValue("Code", icCode);
                    cmd.Parameters.AddWithValue("Desc", icDesc);
                    cmd.Parameters.AddWithValue("verId", verId);

                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        result = int.Parse(r[0].ToString());
                    }

                    r.Close();
                    conn.Close();
                }
            }
            else
            {
                throw new System.ArgumentException("Interchange code already exists");
            }
            return result;
        }

        /// <summary>
        /// Get a particular interchange data from the database
        /// </summary>
        /// <param name="id">Interchange Id</param>
        /// <returns>Interchange Object</returns>
        public Interchange GetInterchange(short id)
        {

            Interchange ic = new Interchange();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT Code, Description FROM Interchange WHERE ID= @id", conn))
                {
                    conn.Open();
                    SqlParameter p1 = new SqlParameter("id", id);
                    cmd.Parameters.Add(p1);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        ic.ID = id;
                        ic.Code = r["Code"].ToString();
                        ic.Description = r["Description"].ToString();
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return ic;
        }

        /// <summary>
        /// Delete an Interchange details from the database
        /// </summary>
        /// <param name="id">Interchange Id</param>
        public void DeleteInterchange(short id)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                string queryinsert = @"DELETE FROM Interchange WHERE Id = @id";

                SqlCommand cmd = new SqlCommand(queryinsert, conn);

                SqlParameter p1 = new SqlParameter("Id", id);
                cmd.Parameters.Add(p1);

                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Update given interchange details 
        /// </summary>
        /// <param name="id">Id</param>
        /// <param name="icCode">Interchange Code</param>
        /// <param name="icFN">Interchange description</param>
        /// <param name="verId">Version id</param>
        public void EditInterchange(short id, string icCode, string icFN, int verId)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string queryinsert = @"UPDATE Interchange SET Code = @icCode, Description = @icFN WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(queryinsert, conn);
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("icCode", icCode);
                cmd.Parameters.AddWithValue("icFN", icFN);
                cmd.ExecuteNonQuery();
                conn.Close();
            }

        }
        #endregion

        #region Crew Point Management
        /// <summary>
        ///  Get the list of crew points
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>List of Crewpoint object</returns>
        public List<Crewpoint> GetCrewPointList(string verId)
        {
            List<Crewpoint> cpLst = new List<Crewpoint>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT  ID, Code, Description FROM  CrewPoint WHERE VersionId = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Crewpoint cp = new Crewpoint();
                        cp.ID = Convert.ToInt16(r["id"]);
                        cp.Code = r["Code"].ToString();
                        cp.Description = r["Description"].ToString();
                        cp.cpChdDetails = new List<CrewPointChildDetails>();
                        cp.cpChdDetails = GetCrewPointChdDetails(cp.ID);
                        cpLst.Add(cp);
                    }
                    r.Close();
                }
            }
            return cpLst;
        }



        /// <summary>
        ///  Get the list of crew points
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>List of Crewpoint object</returns>
        public List<CrewpointBasic> GetCrewPointBasicList(string verId)
        {
            List<CrewpointBasic> cpLst = new List<CrewpointBasic>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT  ID, Code, Description FROM  CrewPoint WHERE VersionId = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        CrewpointBasic cp = new CrewpointBasic();
                        cp.ID = Convert.ToInt16(r["id"]);
                        cp.Code = r["Code"].ToString();
                        cp.Description = r["Description"].ToString();

                        cpLst.Add(cp);
                    }
                    r.Close();
                }
            }
            return cpLst;
        }

        public List<Crewpoint> GetCrewPointListForPermission(string verId)
        {
            List<Crewpoint> cpLst = new List<Crewpoint>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand("SELECT  Distinct  cp.id,cp.Code,s.lineid from Crewpoint cp INNER JOIN Station s ON s.crewpointid = cp.Id WHERE cp.VersionId = @verId AND s.isterminal is null and s.iscrewpoint = 1", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Crewpoint cp = new Crewpoint();
                        cp.ID = Convert.ToInt16(r["id"]);
                        cp.Code = r["Code"].ToString();
                        cp.lnId = Convert.ToInt16(r["lineid"]);

                        cpLst.Add(cp);
                    }
                    r.Close();
                }
            }
            return cpLst;
        }


        /// <summary>
        /// Get Stations details as child for the given Crew Point Id
        /// </summary>
        /// <param name="cpId"></param>
        /// <returns></returns>
        public List<CrewPointChildDetails> GetCrewPointChdDetails(int cpId)
        {
            List<CrewPointChildDetails> cpChdLst = new List<CrewPointChildDetails>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                string query = @"SELECT s1.id, s1.Code Station,tl.code TrainLine, tl.ID LineID,
                                    Stations = STUFF((SELECT ', ' + code  FROM station s2  WHERE s2.lineid = s1.lineid AND s2.crewpointid = s1.crewpointid AND s2.iscrewpoint IS NULL  FOR XML PATH('')), 1, 2, '')
                                    FROM station s1 
                                    INNER JOIN trainline tl ON s1.lineid = tl.id
                                    WHERE s1.crewpointid = @cpId  AND s1.iscrewpoint = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@cpId", cpId);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        CrewPointChildDetails cpChd = new CrewPointChildDetails();
                        cpChd.Station = r["Station"].ToString();
                        cpChd.TrainLine = r["TrainLine"].ToString();
                        cpChd.LineID = Int16.Parse(r["LineID"].ToString());
                        cpChd.Stations = r["Stations"].ToString();

                        cpChdLst.Add(cpChd);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return cpChdLst;
        }

        /// <summary>
        /// Check whether the crewpoint already exists or not for the given version
        /// </summary>
        /// <param name="cpCode">Crew Point Code</param>
        /// <param name="verId">Version Id</param>
        /// <returns>True / False</returns>

        private bool CheckCrewPointCode(string cpCode, int verId)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM CrewPoint WHERE Code LIKE @cpCode AND VersionID = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@cpCode", cpCode);
                    cmd.Parameters.AddWithValue("@verId", verId);

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
            }
        }

        /// <summary>
        /// Add a crew point
        /// </summary>
        /// <param name="cpCode">Crew point Code</param>
        /// <param name="cpDesc">Crew point description</param>
        /// <param name="verId">Version Id</param>
        /// <returns>Crew point Id</returns>
        public int AddCrewPoint(string cpCode, string cpDesc, int verId)
        {
            int result = -1;
            if (!CheckCrewPointCode(cpCode, verId))
            {

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    string queryinsert = @"INSERT INTO CrewPoint(Code,Description,VersionID)VALUES(@Code,@Desc,@verId) SELECT SCOPE_IDENTITY()";
                    SqlCommand cmd = new SqlCommand(queryinsert, conn);

                    cmd.Parameters.AddWithValue("Code", cpCode);
                    cmd.Parameters.AddWithValue("Desc", cpDesc);
                    cmd.Parameters.AddWithValue("verId", verId);

                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        result = int.Parse(r[0].ToString());
                    }

                    r.Close();
                    conn.Close();
                }
            }
            else
            {
                throw new System.ArgumentException("Crewpoint already exists");
            }
            return result;
        }

        /// <summary>
        /// Get a particular crew point details
        /// </summary>
        /// <param name="id">Crew Point Id</param>
        /// <returns></returns>
        public Crewpoint GetCrewPoint(short id)
        {

            Crewpoint cp = new Crewpoint();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT Code, Description FROM CrewPoint WHERE ID= @id", conn))
                {
                    conn.Open();
                    SqlParameter p1 = new SqlParameter("id", id);
                    cmd.Parameters.Add(p1);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        cp.ID = id;
                        cp.Code = r["Code"].ToString();
                        cp.Description = r["Description"].ToString();
                    }
                    r.Close();
                }
            }
            return cp;
        }

        /// <summary>
        /// Delete a crew point
        /// </summary>
        /// <param name="id">Crew Point Id</param>
        public void DeleteCrewPoint(short id)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string queryinsert = @"DELETE FROM CrewPoint WHERE Id = @id";

                SqlCommand cmd = new SqlCommand(queryinsert, conn);

                SqlParameter p1 = new SqlParameter("Id", id);
                cmd.Parameters.Add(p1);

                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Edit crew point details
        /// </summary>
        /// <param name="id">Crew Point Id</param>
        /// <param name="cpDesc">Crew Point Description</param>
        public void EditCrewPoint(short id, string cpCode, string cpDesc)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string queryinsert = @"UPDATE CrewPoint SET Code = @cpCode, Description = @cpDesc WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(queryinsert, conn);
                cmd.Parameters.AddWithValue("Id", id);
                cmd.Parameters.AddWithValue("cpCode", cpCode);
                cmd.Parameters.AddWithValue("cpDesc", cpDesc);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Get the list of Crew point codes for the given Line Id
        /// </summary>
        /// <param name="lnId">Line Id</param>
        /// <returns>Crew Point list object</returns>
        public List<CrewpointBasic> GetCrewPointListByLineId(string lnId)
        {
            List<CrewpointBasic> cpLst = new List<CrewpointBasic>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT cp.ID,cp.Code,cp.[Description] FROM Crewpoint cp
                                                                                        INNER JOIN Station s ON s.CrewpointID = cp.ID and s.IsCrewpoint = 1
                                                                                        INNER JOIN TrainLine tl ON tl.ID = s.LineID
                                                                                        WHERE tl.ID = @lnId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@lnId", lnId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        CrewpointBasic cp = new CrewpointBasic();
                        cp.ID = Convert.ToInt16(r["id"]);
                        cp.Code = r["Code"].ToString();
                        cp.Description = r["Description"].ToString();

                        cpLst.Add(cp);
                    }
                    r.Close();
                }
            }
            return cpLst;
        }
        #endregion

        #region Bound Management

        /// <summary>
        /// Get the bound list from database
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>Bound list object</returns>
        public List<Bound> GetBoundList(string verId)
        {
            List<Bound> bndLst = new List<Bound>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT b.Id, b.Code Bound,tl.Code Line, slk.Value Type FROM Bound b 
                                                                                    INNER JOIN TrainLine tl on tl.Id = b.LineID 
                                                                                    INNER JOIN SystemLookUp slk on slk.id= b.BoundType WHERE b.VersionId = @verId ORDER BY tl.code", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Bound bnd = new Bound();

                        bnd.ID = Convert.ToInt16(r["id"]);
                        bnd.Code = r["Bound"].ToString();
                        bnd.Line = r["Line"].ToString();
                        bnd.BoundType = r["Type"].ToString();
                        bnd.Stns = new List<Station>();
                        bnd.Stns = GetStationList(bnd.ID);
                        bndLst.Add(bnd);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return bndLst;
        }

        /// <summary>
        /// Get station list for the given bound id from database as one string
        /// </summary>
        /// <param name="bndId">Bound Id</param>
        /// <returns>Station list object</returns>
        private List<Station> GetStationList(short bndId)
        {
            List<Station> stnLst = new List<Station>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                string query = @"SELECT  stns = STUFF((SELECT ' > ' + s.Code  FROM Bound b2  
                                    INNER JOIN Station s ON s.LineID = b2.LineID INNER JOIN BoundStation bs ON bs.StationID =s.ID AND bs.BoundID = b2.ID WHERE b2.id = b.ID ORDER BY bs.Position
                                    FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'), 2, 2, '')
                                    FROM bound b WHERE b.ID = @bndId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@bndId", bndId);

                    SqlDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {
                        Station stn = new Station();
                        //   stnLst = r["stns"].ToString();
                        stn.Code = r["stns"].ToString();
                        stnLst.Add(stn);
                    }

                    r.Close();
                }
                conn.Close();
            }
            return stnLst;
        }

        /// <summary>
        /// Check whether the bound code exists for the given Version
        /// </summary>
        /// <param name="bndCode">Bound Code</param>
        /// <param name="verId">Version Id</param>
        /// <returns>True / False</returns>
        private bool CheckBoundCode(string bndCode, int verId)
        {

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Bound WHERE Code LIKE @bndCode AND VersionID = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@bndCode", bndCode);
                    cmd.Parameters.AddWithValue("@verId", verId);
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
            }
        }

        /// <summary>
        ///  To save Bound data into the database
        /// </summary>
        /// <param name="code">Bound Code</param>
        /// <param name="bndTypeId">Bound Type Id</param>
        /// <param name="lineId">Line Id</param>
        /// <param name="frmStn">From Station</param>
        /// <param name="toStn">To Station</param>
        /// <param name="verId">Version Id</param>
        /// <param name="Stns">Station List</param>
        /// <returns>Bound Id</returns>
        public int AddBound(string code, int bndTypeId, short lineId, int frmStn, int toStn, int verId, List<Station> Stns)
        {
            int bndId = -1, result = -1;
           

            if (!CheckBoundCode(code, verId))
            {
            
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    using (SqlTransaction t = conn.BeginTransaction())
                    {
                        //// Insert into Bound table and get Bound id
                        string query = @"INSERT INTO Bound(Code,LineId,FromStation,ToStation,VersionID,BoundType)VALUES(@code,@lineId,@frmStn,@toStn,@verId,@bndTypeId) SELECT SCOPE_IDENTITY()";
                        SqlCommand cmd = new SqlCommand(query, conn,t);

                        cmd.Parameters.AddWithValue("code", code);
                        cmd.Parameters.AddWithValue("lineId", lineId);
                        if (frmStn == 0)
                        {
                            cmd.Parameters.AddWithValue("frmStn", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("frmStn", frmStn);
                        }
                        if (toStn == 0)
                        {
                            cmd.Parameters.AddWithValue("toStn", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("toStn", toStn);
                        }
                        cmd.Parameters.AddWithValue("verId", verId);
                        cmd.Parameters.AddWithValue("bndTypeId", bndTypeId);

                        SqlDataReader r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            bndId = int.Parse(r[0].ToString());
                        }
                        r.Close();

                        // Insert into bound station table
                        foreach (Station s in Stns)
                        {
                            int sId = s.ID;
                            int pos = s.Position;
                            query = @"INSERT INTO BoundStation(BoundId,StationId, VersionId, Position)VALUES(@bndId,@sId,@verId,@pos) SELECT SCOPE_IDENTITY()";
                            cmd = new SqlCommand(query, conn,t);

                            cmd.Parameters.AddWithValue("bndId", bndId);
                            cmd.Parameters.AddWithValue("sId", sId);
                            cmd.Parameters.AddWithValue("verId", verId);
                            cmd.Parameters.AddWithValue("pos", pos);

                            r = cmd.ExecuteReader();
                            if (r.Read())
                            {
                                result = int.Parse(r[0].ToString());
                            }
                            r.Close();
                        }
                    //    conn.Close();

                        t.Commit();
                    }
                }
            }
            else
            {
                throw new System.ArgumentException("Bound code already exists");
            }

            return result;
        }

        /// <summary>
        /// Get particular Bound data
        /// </summary>
        /// <param name="id">Bound Id</param>
        /// <returns>Bound object</returns>
        public Bound GetBound(short id)
        {
            Bound bnd = new Bound();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand(@"SELECT b.Id, b.Code Bound,tl.Id lnId, slk.Id bndTypeId FROM Bound b 
                                                                                    INNER JOIN TrainLine tl on tl.Id = b.LineID 
                                                                                    INNER JOIN SystemLookUp slk on slk.id= b.BoundType WHERE b.Id = @id ORDER BY tl.code", conn))
                {
                    conn.Open();
                    SqlParameter p1 = new SqlParameter("@id", id);
                    cmd.Parameters.Add(p1);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        bnd.ID = Convert.ToInt16(r["id"]);
                        bnd.Code = r["Bound"].ToString();
                        //  bnd.Line = r["Line"].ToString();
                        //  bnd.BoundType = r["Type"].ToString();
                        bnd.LineId = Convert.ToInt16(r["lnId"].ToString());
                        bnd.BoundTypeId = Convert.ToInt32(r["bndTypeId"].ToString());

                        bnd.Stns = new List<Station>();
                        bnd.Stns = GetBoundStationList(bnd.ID);
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return bnd;
        }

        /// <summary>
        /// Get the bound where the two given stations are adjacent stations in sequence in the bound.
        /// </summary>
        /// <param name="stn1"></param>
        /// <param name="stn2"></param>
        /// <returns></returns>
        public short? GetBoundByStnPair(short stn1, short stn2)
        {
            short? bound = null;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand(@"select bound from (select a.stationID as stn1, a.position as pos1, b.stationID as stn2,b.position as pos2,
                                                            a.boundID as bound from boundstation a inner join 
                                                            boundstation b on a.boundID=b.boundID where b.position = a.position+1) temp 
                                                                where temp.stn1=@stn1 and temp.stn2=@stn2", conn))
                {
                    conn.Open();
                    SqlParameter p1 = new SqlParameter("@stn1", stn1);
                    SqlParameter p2 = new SqlParameter("@stn2", stn2);
                    cmd.Parameters.Add(p1);
                    cmd.Parameters.Add(p2);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        bound = r.GetInt16(0);
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return bound;
        }

        /// <summary>
        /// Get the bound station data from database
        /// </summary>
        /// <param name="bndId">Bound Id</param>
        /// <returns>Station List object</returns>
        private List<Station> GetBoundStationList(short bndId)
        {
            List<Station> stnLst = new List<Station>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                string query = @"SELECT s.id, s.code FROM Station s INNER JOIN BoundStation bs on bs.StationID = s.ID WHERE bs.BoundID = @bndId ORDER BY bs.Position";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@bndId", bndId);

                    SqlDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {
                        Station stn = new Station();
                        stn.ID = Convert.ToInt16(r["id"].ToString());
                        stn.Code = r["code"].ToString();
                        stnLst.Add(stn);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return stnLst;
        }

        /// <summary>
        /// Delete particular Bound data from the table
        /// </summary>
        /// <param name="id">Bound Id</param>
        public void DeleteBound(short id)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string queryinsert = @"DELETE FROM Bound WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(queryinsert, conn);

                SqlParameter p1 = new SqlParameter("Id", id);
                cmd.Parameters.Add(p1);

                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        /// <summary>
        /// Update the bound details
        /// </summary>
        /// <param name="Id">Bound Id</param>
        /// <param name="code">Bound Code</param>
        /// <param name="bndTypeId">Bound Type Id</param>
        /// <param name="lnId">Line Id</param>
        /// <param name="frmStn">From Station</param>
        /// <param name="toStn">To Station</param>
        /// <param name="verId">Version Id</param>
        /// <param name="stns">Staions List</param>
        public void EditBound(short Id, string code, int bndTypeId, short lnId, int frmStn, int toStn, int verId, List<Station> stns)
        {
            int result = -1;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {

                    // Updating Bound Table
                    string queryinsert = @"UPDATE Bound SET Code = @code, lineId = @lnId, FromStation = @frmStn, ToStation = @toStn, BoundType = @bndTypeId WHERE Id = @Id";
                    SqlCommand cmd = new SqlCommand(queryinsert, conn,t);
                    cmd.Parameters.AddWithValue("Id", Id);
                    cmd.Parameters.AddWithValue("Code", code);
                    cmd.Parameters.AddWithValue("lnId", lnId);
                    if (frmStn == 0)
                    {
                        cmd.Parameters.AddWithValue("frmStn", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("frmStn", frmStn);
                    }
                    if (toStn == 0)
                    {
                        cmd.Parameters.AddWithValue("toStn", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("toStn", toStn);
                    }
                    cmd.Parameters.AddWithValue("bndTypeId", bndTypeId);
                    cmd.ExecuteNonQuery();

                    // Updating BoundStation Table
                    queryinsert = @"DELETE FROM BoundStation WHERE BoundId = @Id";
                    cmd = new SqlCommand(queryinsert, conn,t);
                    SqlParameter p1 = new SqlParameter("Id", Id);
                    cmd.Parameters.Add(p1);
                    cmd.ExecuteNonQuery();
                    //List<Station> stnLstFromDb = new List<Station>();
                    //stnLstFromDb = GetBoundStationList(Convert.ToInt16(Id));

                    foreach (Station s in stns)
                    {
                        //if (stnLstFromDb.Count(x => x.ID == s.ID) == 0)
                        //{
                            int sId = s.ID;
                            int pos = s.Position;
                            string query = @"INSERT INTO BoundStation(BoundId,StationId, VersionId, Position)VALUES(@Id,@sId,@verId,@pos) SELECT SCOPE_IDENTITY()";
                            cmd = new SqlCommand(query, conn,t);

                            cmd.Parameters.AddWithValue("Id", Id);
                            cmd.Parameters.AddWithValue("sId", sId);
                            cmd.Parameters.AddWithValue("verId", verId);
                            cmd.Parameters.AddWithValue("pos", pos);

                            SqlDataReader r = cmd.ExecuteReader();
                            if (r.Read())
                            {
                                result = int.Parse(r[0].ToString());
                            }
                            r.Close();
                     //   }
                    }

               //     conn.Close();

                    t.Commit();
                }
            }

        }

        //public List<BoundType> GetBoundType()
        //{
        //    List<BoundType> bndTypeLst = new List<BoundType>();
        //    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
        //    {
        //        using (SqlCommand cmd = new SqlCommand("SELECT id, value FROM SystemLookUp WHERE Type LIKE 'BoundType'", conn))
        //        {
        //            conn.Open();
        //            SqlDataReader r = cmd.ExecuteReader();
        //            while (r.Read())
        //            {
        //                BoundType bt = new BoundType();
        //                bt.Id = Convert.ToInt32(r["id"]);
        //                bt.BndType = r["value"].ToString();
        //                bndTypeLst.Add(bt);

        //            }
        //            r.Close();
        //            conn.Close();
        //        }
        //    }
        //    return bndTypeLst;
        //}

        /// <summary>
        /// Get the station list for the given line id
        /// </summary>
        /// <param name="lnId">Line Id</param>
        /// <returns>Station List objec</returns>
        public List<Station> GetStations(short lnId)
        {
            List<Station> stnLst = new List<Station>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT Id, Code FROM Station WHERE LineId = @lnId ", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@lnId", lnId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Station stn = new Station();
                        stn.ID = Convert.ToInt16(r["Id"]);
                        stn.Code = r["Code"].ToString();
                        stnLst.Add(stn);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return stnLst;
        }
        #endregion

        #region Station Management

        /// <summary>
        /// Get the station list from the database
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>Station List object</returns>
        public List<Station> GetStationList(string verId)
        {
            List<Station> stnLst = new List<Station>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT s.ID, s.code,s.Name, tl.code Line, s.interchangeId ic, 
                                                                                        slk.Value typeValue,
                                                                                        PlatFormList = STUFF((SELECT ', ' + code  FROM [Platform] p WHERE p.StationID = s.ID 
                                                                                        FOR XML PATH('')), 1, 2, ''), s.Type TypeID  FROM Station s
                                                                                        INNER JOIN TrainLine tl ON tl.ID = s.LineID
                                                                                        INNER JOIN SystemLookUp slk on slk.ID = s.Type
                                                                                        WHERE s.VersionID = @verId order by tl.Code", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Station stn = new Station();
                        stn.ID = Convert.ToInt16(r["ID"]);
                        stn.Code = r["Code"].ToString();
                        stn.Description = r["Name"].ToString();
                        stn.LnName = r["Line"].ToString();
                        if (r["ic"].ToString().Trim().Length > 0)
                        {
                            stn.IsInterchange = "Yes";
                        }
                        else
                        {
                            stn.IsInterchange = "-";
                        }
                      
                        stn.PlatFormList = r["PlatFormList"].ToString();
                        stn.StnType = r["typeValue"].ToString();
                        stn.StnTypeId = Convert.ToInt32(r["TypeID"]);

                        stnLst.Add(stn);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return stnLst;
        }

        /// <summary>
        /// Add the station details into the database
        /// </summary>
        /// <param name="stnCode">Station Code</param>
        /// <param name="stnFullName">Station Full Name</param>
        /// <param name="stnNo">Station No</param>
        /// <param name="stnTypeId">Station Type Id</param>
        /// <param name="icId">Interchage Id</param>
        /// <param name="cpId">Crew Point Id</param>
        /// <param name="lnId">Line Id</param>
        /// <param name="verId">Version Id</param>
        /// <param name="IsCrewPoint">Is Crew Point</param>
        /// <param name="ptfLst">Platform list</param>
        /// <returns></returns>
        public int AddStation(string stnCode, string stnFullName, int stnNo, int stnTypeId, string icId, string cpId, short lnId, int verId, short IsCrewPoint,short IsTerminal, List<Platform> ptfLst)
        {
            int stnId = -1, result = -1;

            if (!CheckStnCode(stnCode, verId))
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    using (SqlTransaction t = conn.BeginTransaction())
                    {
                        //// Insert into Station table and get Station id
                        string query = @"INSERT INTO Station(Name,Code, InterchangeID, CrewpointID, LineID, Type,StationNo, VersionID,IsCrewpoint,IsTerminal)
                                        VALUES(@stnFullName,@stnCode,@icId,@cpId,@lnId,@stnTypeId, @stnNo,@verId,@isCrewPoint,@isTerminal) SELECT SCOPE_IDENTITY()";
                        SqlCommand cmd = new SqlCommand(query, conn,t);

                        cmd.Parameters.AddWithValue("stnFullName", stnFullName);
                        cmd.Parameters.AddWithValue("stnCode", stnCode);
                        if (icId != null)
                        {
                            cmd.Parameters.AddWithValue("icId", icId);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("icId", DBNull.Value);
                        }
                        if (cpId != null)
                        {
                            cmd.Parameters.AddWithValue("cpId", cpId);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("cpId", DBNull.Value);
                        }

                        cmd.Parameters.AddWithValue("lnId", lnId);
                        cmd.Parameters.AddWithValue("stnTypeId", stnTypeId);
                        cmd.Parameters.AddWithValue("stnNo", stnNo);
                        cmd.Parameters.AddWithValue("verId", verId);
                        if (IsCrewPoint == 0)
                        {
                            cmd.Parameters.AddWithValue("isCrewPoint", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("isCrewPoint", IsCrewPoint);
                        }

                        if (IsTerminal == 0)
                        {
                            cmd.Parameters.AddWithValue("isTerminal", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("isTerminal", IsTerminal);
                        }

                        SqlDataReader r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            stnId = int.Parse(r[0].ToString());
                        }
                        r.Close();

                        // Insert into Platform table
                        foreach (Platform p in ptfLst)
                        {
                            query = @"INSERT INTO Platform(Code, StationId, Serviceable,VersionId)VALUES(@code,@stnId,@service,@verId) SELECT SCOPE_IDENTITY()";
                            cmd = new SqlCommand(query, conn,t);

                            cmd.Parameters.AddWithValue("code", p.Code);
                            cmd.Parameters.AddWithValue("stnId", stnId);
                            cmd.Parameters.AddWithValue("service", p.Serviceable);
                            cmd.Parameters.AddWithValue("verId", verId);

                            r = cmd.ExecuteReader();
                            if (r.Read())
                            {
                                result = int.Parse(r[0].ToString());
                            }
                            r.Close();
                        }
                  //      conn.Close();

                        t.Commit();
                    }
                }
            }
            else
            {
                throw new System.ArgumentException("Station code already exists");
            }

            return result;
        }

        /// <summary>
        /// Check station code exists or not
        /// </summary>
        /// <param name="stnCode">Station Code</param>
        /// <param name="verId">Version Id</param>
        /// <returns>True / False</returns>
        private bool CheckStnCode(string stnCode, int verId)
        {

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Station WHERE Code LIKE @stnCode AND VersionID = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@stnCode", stnCode);
                    cmd.Parameters.AddWithValue("@verId", verId);
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
            }
        }

        /// <summary>
        /// Delete a particular station
        /// </summary>
        /// <param name="id">Station Id</param>
        public void DeleteStation(short id)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string queryinsert = @"DELETE FROM Station WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(queryinsert, conn);

                SqlParameter p1 = new SqlParameter("Id", id);
                cmd.Parameters.Add(p1);

                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        /// <summary>
        /// Get particular station details
        /// </summary>
        /// <param name="id">Station Id</param>
        /// <returns>Station object</returns>
        public Station GetStation(short id)
        {
            Station stn = new Station();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand(@"SELECT ID, Code, Name, StationNo, LineID, [Type], InterchangeID,CrewpointID, IsCrewpoint FROM STATION WHERE ID = @id", conn))
                {
                    conn.Open();
                    SqlParameter p1 = new SqlParameter("@id", id);
                    cmd.Parameters.Add(p1);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        stn.ID = id;
                        stn.Code = r["Code"].ToString();
                        stn.Description = r["Name"].ToString();
                        stn.StnNo = Convert.ToInt32(r["StationNo"].ToString());
                        stn.lnId = Convert.ToInt16(r["LineID"].ToString());
                        stn.StnTypeId = Convert.ToInt32(r["Type"].ToString());
                        if (r["InterchangeID"].ToString().Trim().Length > 0)
                        {
                            stn.InterchangeId = r["InterchangeID"].ToString();
                        }
                        else
                        {
                            stn.InterchangeId = "0";
                        }
                        if (r["CrewpointID"].ToString().Trim().Length > 0)
                        {
                            stn.CrewPointId = r["CrewpointID"].ToString();
                        }
                        else
                        {
                            stn.CrewPointId = "0";
                        }
                        //   if (r["IsCrewpoint"].ToString().Trim().Length > 0)
                        if (r["IsCrewpoint"].ToString() == "True")
                        {
                            stn.IsCrewPoint = 1;
                        }
                        else
                        {
                            stn.IsCrewPoint = 0;
                        }
                        stn.PtfLst = new List<Platform>();
                        stn.PtfLst = GetPlatformList(id);
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return stn;
        }

        /// <summary>
        /// Get the Platform list for the given station id
        /// </summary>
        /// <param name="stnId">Station Id</param>
        /// <returns>Platform list object</returns>
        public List<Platform> GetPlatformList(short stnId)
        {
            List<Platform> pltLst = new List<Platform>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                string query = @"SELECT Code,Serviceable FROM Platform WHERE StationID = @stnId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@stnId", stnId);

                    SqlDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {
                        Platform plt = new Platform();
                        plt.Code = r["Code"].ToString();
                        if (r["Serviceable"].ToString() == "True")
                        {
                            plt.Serviceable = true;
                        }
                        else
                        {
                            plt.Serviceable = false;
                        }
                        pltLst.Add(plt);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return pltLst;
        }

        /// <summary>
        /// Edit the station details
        /// </summary>
        /// <param name="Id">Station Id</param>
        /// <param name="stnCode">Station Code</param>
        /// <param name="stnFullName">Full Name</param>
        /// <param name="stnNo">Station No</param>
        /// <param name="stnTypeId">Station Type Id</param>
        /// <param name="icId">Interchange Id</param>
        /// <param name="cpId">Crew point id</param>
        /// <param name="lnId">Line id</param>
        /// <param name="verId">Version Id</param>
        /// <param name="isCrewPoint">IsCrewpoint</param>
        /// <param name="ptfLst">Platform list</param>
        public void EditStation(int Id, string stnCode, string stnFullName, int stnNo, int stnTypeId, string icId, string cpId, short lnId, int verId, short isCrewPoint,short IsTerminal, List<Platform> ptfLst)
        {
            int result = -1;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    // Updating Station Table
                    string queryinsert = @"UPDATE Station SET Code = @stnCode, Name = @stnFullName, StationNo = @stnNo, LineID = @lnId, Type = @stnTypeId, InterchangeId = @icId, CrewpointId = @cpId, IsCrewpoint = @isCrewPoint, IsTerminal = @IsTerminal  WHERE Id = @Id";
                    SqlCommand cmd = new SqlCommand(queryinsert, conn,t);
                    cmd.Parameters.AddWithValue("Id", Id);
                    cmd.Parameters.AddWithValue("stnCode", stnCode);
                    cmd.Parameters.AddWithValue("stnFullName", stnFullName);
                    cmd.Parameters.AddWithValue("stnNo", stnNo);
                    cmd.Parameters.AddWithValue("lnId", lnId);
                    cmd.Parameters.AddWithValue("stnTypeId", stnTypeId);
                    if (icId != null)
                    {
                        cmd.Parameters.AddWithValue("icId", icId);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("icId", DBNull.Value);
                    }
                    if (cpId != null)
                    {
                        cmd.Parameters.AddWithValue("cpId", cpId);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("cpId", DBNull.Value);
                    }

                    if (isCrewPoint == 0)
                    {
                        cmd.Parameters.AddWithValue("isCrewpoint", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("isCrewpoint", isCrewPoint);
                    }

                    if (IsTerminal == 0)
                    {
                        cmd.Parameters.AddWithValue("IsTerminal", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("IsTerminal", IsTerminal);
                    }
                    cmd.ExecuteNonQuery();

                    // Updating Platform Table
                    List<Platform> pfLstFrmDB = GetPlatformList(Convert.ToInt16(Id));
                    foreach (Platform p in ptfLst)
                    {
                        if (pfLstFrmDB == null || pfLstFrmDB.Count(x => x.Code == p.Code) == 0)
                        {
                            string query = @"INSERT INTO Platform(Code, StationId, Serviceable,VersionId)VALUES(@code,@Id,@service,@verId) SELECT SCOPE_IDENTITY()";
                            cmd = new SqlCommand(query, conn,t);

                            cmd.Parameters.AddWithValue("code", p.Code);
                            cmd.Parameters.AddWithValue("Id", Id);
                            cmd.Parameters.AddWithValue("service", p.Serviceable);
                            cmd.Parameters.AddWithValue("verId", verId);

                            SqlDataReader r = cmd.ExecuteReader();
                            if (r.Read())
                            {
                                result = int.Parse(r[0].ToString());
                            }
                            r.Close();
                        }
                    }

                    if (pfLstFrmDB != null && pfLstFrmDB.Count > 0)
                    {
                        foreach (Platform dbPlatform in pfLstFrmDB)
                        {
                            if(ptfLst.Count(x => x.Code == dbPlatform.Code) == 0)
                            {
                                string query = @"DELETE FROM Platform WHERE StationId=@stationId and VersionId=@verId and Code=@code";
                                cmd = new SqlCommand(query, conn, t);

                                cmd.Parameters.AddWithValue("code", dbPlatform.Code);
                                cmd.Parameters.AddWithValue("stationId", Id);
                                cmd.Parameters.AddWithValue("verId", verId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    t.Commit();
                }
            }

        }

        /// <summary>
        /// Get the list of stations for the given Line Id
        /// </summary>
        /// <param name="lnId">LIne Id</param>
        /// <returns>Station list object</returns>
        public List<Station> GetStationListByLineId(string lnId)
        {
            List<Station> stnLst = new List<Station>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT s.ID, s.code FROM station s WHERE s.LineId = @lnId order by s.ID", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@lnId", lnId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Station stn = new Station();
                        stn.ID = Convert.ToInt16(r["ID"]);
                        stn.Code = r["Code"].ToString();
                        stnLst.Add(stn);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return stnLst;
        }

        public List<Station> GetTerminalListByLineId(string lnId)
        {
            List<Station> stnLst = new List<Station>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT s.ID, s.code FROM station s WHERE isTerminal = 1 AND  s.LineId = @lnId order by s.ID", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@lnId", lnId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Station stn = new Station();
                        stn.ID = Convert.ToInt16(r["ID"]);
                        stn.Code = r["Code"].ToString();
                        stnLst.Add(stn);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return stnLst;
        }
        #endregion

        #region Version Management
        /// <summary>
        /// To get the previous version id
        /// </summary>
        /// <param name="curVerId">New version Id</param>
        /// <returns>Version Id</returns>
        private int GetPreviousVersion(int curVerId)
        {
            int result = -1;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                string queryinsert = @"SELECT TOP(1) ID FROM VersionTree WHERE [type] ='NetworkSettings' AND ID <> @curVerId ORDER BY EffectiveFrom DESC";
                SqlCommand cmd = new SqlCommand(queryinsert, conn);

                cmd.Parameters.AddWithValue("@curVerId", curVerId);


                SqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    result = int.Parse(r[0].ToString());
                }

                r.Close();
                conn.Close();
            }

            return result;
        }

        /// <summary>
        /// Add new version data for the given version id
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>Id</returns>
        public int AddVersionData(int? parentID, string type, string name, string remark, Int16 createdBy, string effectiveFrom, string effectiveTo)
        {
            int result = -1;
            
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {

                    int verId = (new VersionTreeBL()).AddVersion(parentID, type, name, remark, createdBy, effectiveFrom, effectiveTo,conn,trans);
                    result = verId;
                    SqlCommand cmd = new SqlCommand("uspNetworkVerData", conn,trans);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Transaction = trans;
                    cmd.Parameters.Add(new SqlParameter("@VerId", verId));
                    cmd.Parameters.Add(new SqlParameter("@PrevVerId", parentID));
                    cmd.Parameters.Add("@Status", SqlDbType.Int);
                    cmd.Parameters["@Status"].Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();
                    result = (int)cmd.Parameters["@Status"].Value;
                    
                    trans.Commit();
                }
            }
            return result;
        }

        public void EditVersionData(int parentID, string name, string remark, Int16 createdBy, string effectiveFrom, string effectiveTo)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string queryUpdate = @"UPDATE versiontree SET Name = @name, Remark = @remark, EffectiveFrom =@effectiveFrom, UpdatedBy = @createdBy WHERE ID = @parentID";

                SqlCommand cmd = new SqlCommand(queryUpdate, conn);
                cmd.Parameters.AddWithValue("parentID", parentID);
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue("remark", remark);
                cmd.Parameters.AddWithValue("effectiveFrom", effectiveFrom);
                cmd.Parameters.AddWithValue("createdBy", createdBy);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
             
        }

        /// <summary>
        /// Delete version data from the various table
        /// </summary>
        /// <param name="verId">Version Id</param>
        public void DeleteVersionData(int verId)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                using (SqlTransaction t = conn.BeginTransaction())
                {
                    //Bound
                    string queryinsert = @"DELETE FROM Bound WHERE VersionId = @verId";
                    SqlCommand cmd = new SqlCommand(queryinsert, conn,t);
                    SqlParameter p1 = new SqlParameter("verId", verId);
                    cmd.Parameters.Add(p1);
                    cmd.ExecuteNonQuery();

                    //Station
                    queryinsert = @"DELETE FROM Station WHERE VersionId = @verId";
                    cmd = new SqlCommand(queryinsert, conn,t);
                    p1 = new SqlParameter("verId", verId);
                    cmd.Parameters.Add(p1);
                    cmd.ExecuteNonQuery();
                    //Crewpoint
                    queryinsert = @"DELETE FROM Crewpoint WHERE VersionId = @verId";
                    cmd = new SqlCommand(queryinsert, conn,t);
                    p1 = new SqlParameter("verId", verId);
                    cmd.Parameters.Add(p1);
                    cmd.ExecuteNonQuery();
                    //Interchange
                    queryinsert = @"DELETE FROM Interchange WHERE VersionId = @verId";
                    cmd = new SqlCommand(queryinsert, conn,t);
                    p1 = new SqlParameter("verId", verId);
                    cmd.Parameters.Add(p1);
                    cmd.ExecuteNonQuery();
                    //Train Line
                    queryinsert = @"DELETE FROM TrainLine WHERE VersionId = @verId";
                    cmd = new SqlCommand(queryinsert, conn,t);
                    p1 = new SqlParameter("verId", verId);
                    cmd.Parameters.Add(p1);
                    cmd.ExecuteNonQuery();


                    //     conn.Close();
                    t.Commit();
                }
            }
        }
        #endregion

        #region Platform Management
        /// <summary>
        /// Get the platform ID for the given platform code and given network version
        /// </summary>
        /// <param name="code"></param>
        /// <param name="versionID"></param>
        /// <returns></returns>
        public short GetPlatformIDByCode(string code, int versionID)
        {
            short ID = -1;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            { 
                using (SqlCommand cmd = new SqlCommand("SELECT [id] FROM [platform] WHERE [code]=@code and versionid = @verID", conn))
                {
                    conn.Open();
                    SqlParameter p1 = new SqlParameter("code", code);
                    cmd.Parameters.Add(p1);
                    SqlParameter p2 = new SqlParameter("verID", versionID);
                    cmd.Parameters.Add(p2);

                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        ID = r.GetInt16(0);
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return ID;
        }

        public List<Platform> GetPlatformList(int verId)
        {
            List<Platform> resultList = new List<Platform>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT [ID], [code],StationID, Serviceable FROM [Platform]  " +
                                " where VersionID = @verId order by code ", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        Platform platform = new Platform();
                        platform.ID = r.GetInt16(0);
                        platform.Code = r.GetString(1);
                        platform.StationID = r.GetInt16(2);
                        platform.Serviceable = r.GetBoolean(3);
                        resultList.Add(platform);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return resultList;
        }

        public List<string> GetTerminalPatforms(int verId)
        {
            List<string> resultList = new List<string>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                string cmdText = @"select plt.code, stn.IsTerminal from platform plt inner join station stn on plt.StationID=stn.id 
                                    where plt.VersionID=@versionID ";
                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@versionID", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        string plat = r.GetString(0);
                        if(!r.IsDBNull(1) && r.GetBoolean(1))
                            resultList.Add(plat);
                    }
                    r.Close();
                }
                conn.Close();
            }
            resultList.Add("TNM3");
            return resultList;
        }

        /// <summary>
        /// Get the station id by platform code
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public Station GetStnByPlatform(string platform, int version)
        {
            Station stn = null;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT pt.[StationID], stn.[code], s.[key] FROM [Platform] pt  
                                                        inner join station stn on pt.stationid=stn.id
                                                        inner join systemlookup s on stn.[type]=s.id
                                                        where pt.VersionID = @verId and pt.[code] = @platform ", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", version);
                    cmd.Parameters.AddWithValue("@platform", platform);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        stn = new Station();
                        stn.ID = r.GetInt16(0);
                        stn.Code = r.GetString(1);
                        stn.Type = r.GetString(2);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return stn;
        }

        #endregion

        #region LinkwayWalkTime Management

        /// <summary>
        /// Get the list of linkwaywalktime from the database
        /// </summary>
        /// <param name="verId">Version Id</param>
        /// <returns>linkwaywalktime List object</returns>
        public List<LinkwayWalkTime> GetLinkwayWalkTimeList(string verId)
        {
            List<LinkwayWalkTime> walktimeList = new List<LinkwayWalkTime>();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT s.ID, s.STN1,s.STN2, s.WalkTime FROM LinkwayWalkTime s
                                                                                        WHERE s.VersionID = @verId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@verId", verId);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        LinkwayWalkTime time = new LinkwayWalkTime();
                        time.ID = Convert.ToInt16(r["ID"]);
                        time.Stn1 = Convert.ToInt16(r["STN1"]);
                        time.Stn2 = Convert.ToInt16(r["STN2"]);
                        time.WalkTime = Convert.ToInt32(r["WalkTime"]);

                        walktimeList.Add(time);
                    }
                    r.Close();
                }
                conn.Close();
            }
            return walktimeList;
        }

       /// <summary>
       /// Add link way walk time record into database
       /// </summary>
       /// <param name="stn1"></param>
       /// <param name="stn2"></param>
       /// <param name="walkTime"></param>
       /// <param name="verId"></param>
       /// <returns></returns>
        public int AddLinkwayWalkTime(short stn1, short stn2, int walkTime, int verId)
        {
            int  result = -1;

            if (!CheckLinkwayWalkTime(stn1, stn2))
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
                {
                    conn.Open();

                    using (SqlTransaction t = conn.BeginTransaction())
                    {
                        //// Insert into Station table and get Station id
                        string query = @"INSERT INTO LinkwayWalkTime(stn1, stn2, walktime, VersionID)
                                        VALUES(@stn1,@stn2,@walkTime,@verId) SELECT SCOPE_IDENTITY()";
                        SqlCommand cmd = new SqlCommand(query, conn, t);

                        cmd.Parameters.AddWithValue("stn1", stn1);
                        cmd.Parameters.AddWithValue("stn2", stn2);
                        cmd.Parameters.AddWithValue("walkTime", walkTime);
                        cmd.Parameters.AddWithValue("verId", verId);
                        

                        SqlDataReader r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            result = int.Parse(r[0].ToString());
                        }
                        r.Close();

                       
                        t.Commit();
                    }
                }
            }
            else
            {
                throw new System.ArgumentException("Link way walk time record for the given stations already exists");
            }

            return result;
        }

        /// <summary>
        /// Check if the record for the given pair of stn1 and stn2 exists in the database
        /// </summary>
        /// <param name="stn1"></param>
        /// <param name="stn2"></param>
        /// <returns></returns>
        private bool CheckLinkwayWalkTime(short stn1, short stn2)
        {

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("select count(*) from linkwaywalktime where (stn1 = @stn1 and stn2=@stn2) or (stn1 = @stn2 and stn2=@stn1)", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@stn1", stn1);
                    cmd.Parameters.AddWithValue("@stn2", stn2);
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
            }
        }

        /// <summary>
        /// Delete a particular record
        /// </summary>
        /// <param name="id">linkwaywalktime Id</param>
        public void DeleteLinkwayWalkTime(short id)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string queryinsert = @"DELETE FROM LinkwayWalkTime WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(queryinsert, conn);

                SqlParameter p1 = new SqlParameter("Id", id);
                cmd.Parameters.Add(p1);

                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        /// <summary>
        /// Get particular Linkwaywalktime details
        /// </summary>
        /// <param name="id">Linkwaywalktime Id</param>
        /// <returns>Linkwaywalktime object</returns>
        public LinkwayWalkTime GetLinkwayWalktime(short id)
        {
            LinkwayWalkTime time = new LinkwayWalkTime();
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand(@"SELECT ID, stn1, stn2, walktime FROM LinkwayWalkTime WHERE ID = @id", conn))
                {
                    conn.Open();
                    SqlParameter p1 = new SqlParameter("@id", id);
                    cmd.Parameters.Add(p1);

                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        time.ID = Convert.ToInt16(r["ID"]);
                        time.Stn1 = Convert.ToInt16(r["stn1"]);
                        time.Stn2 = Convert.ToInt16(r["stn2"]);
                        time.WalkTime = Convert.ToInt32(r["walktime"]);
                    }
                    r.Close();
                    conn.Close();
                }
            }
            return time;
        }

        

        /// <summary>
        /// Edit the walk time of the linkwaywalktime object with given id
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="time"></param>
        public void EditLinkwayWalkTime(short Id, int time)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();

                using (SqlTransaction t = conn.BeginTransaction())
                {
                    // Updating Station Table
                    string queryinsert = @"UPDATE LinkwayWalkTime SET walktime = @time WHERE Id = @Id";
                    SqlCommand cmd = new SqlCommand(queryinsert, conn, t);
                    cmd.Parameters.AddWithValue("Id", Id);
                    cmd.Parameters.AddWithValue("time", time);
                    
                    cmd.ExecuteNonQuery();

                    t.Commit();
                }
            }

        }

       /// <summary>
        /// Get the LinkwayWalkTime for the given station pair
       /// </summary>
       /// <param name="stn1"></param>
       /// <param name="stn2"></param>
       /// <returns></returns>
        public LinkwayWalkTime GetLinkwayWalkTimeByStnPair(short stn1, short stn2)
        {
            LinkwayWalkTime time = null;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT s.ID, s.walktime FROM LinkwayWalkTime s WHERE (s.stn1 = @stn1 and s.stn2 = @stn2) or (s.stn1 = @stn2 and s.stn2 = @stn1)", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@stn1", stn1);
                    cmd.Parameters.AddWithValue("@stn2", stn2);
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        time = new LinkwayWalkTime();
                        time.ID = Convert.ToInt16(r["ID"]);
                        time.WalkTime = Convert.ToInt32(r["walktime"]);
                        time.Stn1 = stn1;
                        time.Stn2 = stn2;
                        break;
                    }
                    r.Close();
                }
                conn.Close();
            }
            return time;
        }

        #endregion

       

    }  
}