///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>VersionTreeDL.cs</file>
///<description>
///VersionTreeDL is the class that is the data access layer of version control parameters
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
using System.Web;

using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using TTF.Models;
using System.Globalization;
using TTF.Utils;

namespace TTF.DataLayer
{
    public class VersionTreeDL
    {
        /// <summary>
        /// Get all the versions from the database
        /// </summary>
        /// <returns>List of VersionTree objects</returns>
        public List<VersionTree> GetAllVersions()
        {
            List<VersionTree> versions = new List<VersionTree>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [Version], EffectiveFrom, EffectiveTo FROM VersionTree "
                    + " where [Valid] = 1)", conn))
                {
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        VersionTree ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);

                        if (!(r.GetValue(10) is DBNull))
                            ver.EffectiveFrom = r.GetDateTime(10);


                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);

                        versions.Add(ver);
                    }
                    r.Close();
                }
            }
                


            return versions;
        }

        /// <summary>
        /// Get all the versions of a given type.
        /// </summary>
        /// <param name="type">Version Type</param>
        /// <returns>List of VersionTree objects</returns>
        public List<VersionTree> GetVersionsByType(string type)
        {
            List<VersionTree> versions = new List<VersionTree>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate],[Version], "
                    + "EffectiveFrom, EffectiveTo FROM VersionTree where [Type]=@type and [Valid]=1", conn))

          
                {
                    cmd.Parameters.AddWithValue("type", type);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        VersionTree ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);

                        if (!(r.GetValue(10) is DBNull))
                        {
                            ver.EffectiveFrom =  r.GetDateTime(10);
                            ver.EffectiveFrm = r.GetDateTime(10).ToShortDateString();
                        }
                            

                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);

                        versions.Add(ver);
                    }
                    r.Close();
                }
            }

            return versions;
        }

        /// <summary>
        /// Get the version with a given ID.
        /// </summary>
        /// <param name="id">Version ID</param>
        /// <returns>VersionTree object</returns>
        public VersionTree GetVersionByID(int id)
        {
            VersionTree ver = null;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate],[Version], "
                    + "EffectiveFrom, EffectiveTo FROM VersionTree where [id]=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);

                        if (!(r.GetValue(10) is DBNull))
                            ver.EffectiveFrom = r.GetDateTime(10);
                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);
                    }
                    r.Close();
                }
            }

            return ver;
        }

        /// <summary>
        /// A special version is defined as a version that has both effectivefrom and effectiveto values specified. 
        /// A special version has higher priority over normal version when deciding which version is effective for a given date.
        /// </summary>
        /// <param name="type">version type</param>
        /// <param name="date">date that the version is supposed to be effective</param>
        /// <returns>Special version that is effective on the given date</returns>
        public VersionTree GetEffectiveSpecialVersion(string type, string date)
        {
            VersionTree ver = null;
           
            DateTime tDate = Conversion.GetDateFromUIDateString(date);

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate],[Version],EffectiveFrom, EffectiveTo FROM VersionTree " +
                    " where [type]=@type and [valid]=1 and effectiveto is not null and effectivefrom <= @date and effectiveto>=@date order by createddate desc", conn))
                {
                    cmd.Parameters.AddWithValue("type", type);
                    cmd.Parameters.AddWithValue("date", tDate);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);

                        if (!(r.GetValue(10) is DBNull))
                            ver.EffectiveFrom = r.GetDateTime(10);
                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);

                        break;
                    }
                    r.Close();
                }
            }

            return ver;
        }

        /// <summary>
        /// Check if there is special version of a given type defined for the date
        /// </summary>
        /// <param name="type">The type input should be in form of wildcard</param>
        /// <param name="date"></param>
        /// <returns></returns>
        public bool HasSpecialVersion(string type, string date)
        {
            bool has = false;

            DateTime tDate = Convert.ToDateTime(date);

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID] FROM VersionTree " +
                    " where [type] like @type and [Valid]=1 and effectiveto is not null and effectivefrom <= @date and effectiveto>=@date order by createddate desc", conn))
                {
                    cmd.Parameters.AddWithValue("type", type);
                    cmd.Parameters.AddWithValue("date", tDate);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        has = true;
                    }
                    r.Close();
                }
            }

            return has;
        }

        /// <summary>
        /// A normal version is the version that does not have effectiveto value.
        /// This function finds the most effective version of the give type on the given date.
        /// </summary>
        /// <param name="type">version type</param>
        /// <param name="date">date when the version is supposed to be effective</param>
        /// <returns>Effective normal version</returns>
        public VersionTree GetEffectiveNormalVersion(string type, string date)
        {
            VersionTree ver = null;
            DateTime tDate = Convert.ToDateTime(date);

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate],[Version],EffectiveFrom, EffectiveTo FROM VersionTree " +
                    " where [type]=@type and [Valid]=1 " +
                "and effectiveto is null and effectivefrom is not null and effectivefrom <= @date order by EffectiveFrom desc, createddate desc", conn))
                {
                    cmd.Parameters.AddWithValue("type", type);
                    cmd.Parameters.AddWithValue("date", tDate);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);

                        if (!(r.GetValue(10) is DBNull))
                            ver.EffectiveFrom = r.GetDateTime(10);
                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);

                        break;
                    }
                    r.Close();
                }
            }

            return ver;
        }

        /// <summary>
        /// Get effective version of given type on the give date. The function will first check if there is effective special version.
        /// If not, it will then check effective normal version
        /// </summary>
        /// <param name="type">version type</param>
        /// <param name="date">version date</param>
        /// <returns>Effective version</returns>
        public VersionTree GetEffectiveVersion(string type, string date)
        {
            VersionTree ver = GetEffectiveSpecialVersion(type, date);

            if (ver == null)
                ver = GetEffectiveNormalVersion(type, date);

            return ver;
        }

        /// <summary>
        /// Get all the child versions of a given version.
        /// </summary>
        /// <param name="parentID">ID of the parent version</param>
        /// <returns>List of VersionTree objects</returns>
        public List<VersionTree> GetAllChildren(int parentID)
        {
            List<VersionTree> versions = new List<VersionTree>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate],[Version],EffectiveFrom, EffectiveTo FROM VersionTree where [parentID]=@parentID and [valid]=1 ", conn))
                {
                    cmd.Parameters.AddWithValue("parentID", parentID);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        VersionTree ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);
                        if (!(r.GetValue(10) is DBNull))
                            ver.EffectiveFrom = r.GetDateTime(10);
                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);
                        versions.Add(ver);
                    }
                    r.Close();
                }
            }

            return versions;
        }

        /// <summary>
        /// Get all the first level versions of a given type.
        /// </summary>
        /// <param name="type">Version Type</param>
        /// <returns>List of VersionTree objects</returns>
        public List<VersionTree> GetFirstLevelVersionsByType(string type)
        {
            List<VersionTree> versions = new List<VersionTree>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate],[Version], EffectiveFrom, EffectiveTo "
                    + "FROM VersionTree where [Type]=@type and [Valid]=1 and parentID is null", conn))
                {
                    cmd.Parameters.AddWithValue("type", type);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        VersionTree ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);

                        if (!(r.GetValue(10) is DBNull))
                            ver.EffectiveFrom = r.GetDateTime(10);

                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);

                        versions.Add(ver);
                    }
                    r.Close();
                }
            }

            return versions;
        }

        /// <summary>
        /// Get all the versions of a given name.
        /// </summary>
        /// <param name="name">Version name</param>
        /// <returns>List of VersionTree objects</returns>
        public List<VersionTree> GetVersionsByName(string name)
        {
            List<VersionTree> versions = new List<VersionTree>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate],[Version], " +
                    "EffectiveFrom, EffectiveTo FROM VersionTree where [Name]=@name and [Valid]=1 ", conn))
                {
                    cmd.Parameters.AddWithValue("name", name);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        VersionTree ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);
                        if (!(r.GetValue(10) is DBNull))
                            ver.EffectiveFrom = r.GetDateTime(10);

                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);
                        versions.Add(ver);
                    }
                    r.Close();
                }
            }

            return versions;
        }

        /// <summary>
        /// Get all the versions of a given name and a given type
        /// </summary>
        /// <param name="name">Name of the version</param>
        /// <param name="type">Type of the version</param>
        /// <returns>List of versions satisfying the search criteria.</returns>
        public List<VersionTree> GetVersionsByNameAndType(string name, string type)
        {
            List<VersionTree> versions = new List<VersionTree>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [ID],[ParentID],[Name],[Type], [Remark], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], " +
                    "[Version], EffectiveFrom, EffectiveTo FROM VersionTree where [Type]=@type and [Name]=@name and [Valid]=1", conn))
                {
                    cmd.Parameters.AddWithValue("type", type);
                    cmd.Parameters.AddWithValue("name", name);
                    conn.Open();
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        VersionTree ver = new VersionTree();
                        ver.ID = r.GetInt32(0);
                        if (!(r.GetValue(1) is DBNull))
                            ver.ParentID = r.GetInt32(1);
                        ver.Name = r.GetString(2);
                        ver.Type = r.GetString(3);
                        if (!(r.GetValue(4) is DBNull))
                            ver.Remark = r.GetString(4);
                        ver.CreatedBy = r.GetInt16(5);
                        ver.CreatedDate = r.GetDateTime(6);
                        ver.UpdatedBy = r.GetInt16(7);
                        ver.UpdatedDate = r.GetDateTime(8);
                        ver.Version = r.GetString(9);

                        if (!(r.GetValue(10) is DBNull))
                            ver.EffectiveFrom = r.GetDateTime(10);

                        if (!(r.GetValue(11) is DBNull))
                            ver.EffectiveTo = r.GetDateTime(11);

                        versions.Add(ver);
                    }
                    r.Close();
                }
            }

            return versions;
        }

       /// <summary>
       /// Add a version with given attributes into the database
       /// </summary>
       /// <param name="parentID">Parent id of the version</param>
       /// <param name="type">version type</param>
       /// <param name="name">version name</param>
       /// <param name="remark">version remark</param>
       /// <param name="createdBy">The user id who created the version</param>
        /// <param name="effectiveFrom">The date when the version is effective from</param>
       /// <param name="effectiveTo">The date when the version is effective to</param>
       /// <returns>id of the version added</returns>
        public int AddVersion(int? parentID, string version, string type, string name, string remark, short createdBy, string effectiveFrom, string effectiveTo,SqlConnection conn,SqlTransaction trans)
        {
            int result = -1;

            if (effectiveTo != null && effectiveFrom == null)
            {
                throw new ApplicationException("The EffectiveTo date is set but the EffectiveFrom date is not set!");
            }

            
                SqlCommand cmd = new SqlCommand("", conn,trans);
                string headSql = "INSERT INTO VersionTree(";
                string valueSql = " Values (";

                if (parentID != null)
                {
                    headSql += "[parentID],";
                    valueSql += "@parentID,";
                    cmd.Parameters.Add(new SqlParameter("@parentID", parentID));
                }
                if (remark != null)
                {
                    headSql += "[remark],";
                    valueSql += "@remark,";
                    cmd.Parameters.Add(new SqlParameter("@remark", remark));
                }

                if (effectiveFrom != null)
                {
                    DateTime eFrom = Conversion.GetDateFromUIDateString(effectiveFrom);
                    headSql += "[effectivefrom],";
                    valueSql += "@effectiveFrom,";
                    cmd.Parameters.Add(new SqlParameter("@effectiveFrom", eFrom));
                }

                if (effectiveTo != null)
                {
                    DateTime eTo = Conversion.GetDateFromUIDateString(effectiveTo);
                    headSql += "[effectiveTo],";
                    valueSql += "@effectiveTo,";
                    cmd.Parameters.Add(new SqlParameter("@effectiveTo", eTo));
                }

                headSql += "[type],";
                valueSql += "@type,";
                cmd.Parameters.Add(new SqlParameter("@type", type));

                headSql += "[version],";
                valueSql += "@version,";
                cmd.Parameters.Add(new SqlParameter("@version", version));

                headSql += "[name],";
                valueSql += "@name,";
                cmd.Parameters.Add(new SqlParameter("@name", name));

                headSql += "[createdBy],";
                valueSql += "@createdBy,";
                cmd.Parameters.Add(new SqlParameter("@createdBy", createdBy));

                headSql += "[CreatedDate],";
                valueSql += "@CreatedDate,";
                cmd.Parameters.Add(new SqlParameter("@CreatedDate", DateTime.Now));

                headSql += "[UpdatedBy],";
                valueSql += "@UpdatedBy,";
                cmd.Parameters.Add(new SqlParameter("@UpdatedBy", createdBy));

                headSql += "[Valid],";
                valueSql += "1,";

                headSql += "[UpdatedDate]) ";
                valueSql += "@UpdatedDate) ";
                cmd.Parameters.Add(new SqlParameter("@UpdatedDate", DateTime.Now));

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
        /// Update a version record with given values.Note that only name and remark are modifiable
        /// </summary>
        /// <param name="id">id of the version</param>
        /// <param name="name">version name</param>
        /// <param name="remark">version remark</param>
        /// <param name="updatedBy">the user id who updated the version</param>
        /// <returns>number of rows affected</returns>
        public int UpdateVersion(int id, string name, string remark, Int16 updatedBy, string effectiveFrom, string effectiveTo)
        {
            int result = -1;

            if (effectiveTo != null && effectiveFrom == null)
            {
                throw new ApplicationException("The EffectiveTo date is set but the EffectiveFrom date is not set!");
            }

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("", conn);
                string cmdText = "UPDATE VersionTree set ";

                
                if (remark != null)
                {
                    cmdText += "[remark] = @remark,";
                    cmd.Parameters.Add(new SqlParameter("@remark", remark));
                }

                if (effectiveFrom != null)
                {
                    DateTime eFrom = Convert.ToDateTime(effectiveFrom);
                    cmdText += "[effectiveFrom] = @effectiveFrom,";
                    cmd.Parameters.Add(new SqlParameter("@effectiveFrom", eFrom));
                }
                else
                {
                    cmdText += "[effectiveFrom] = null,";
                }

                if (effectiveTo != null)
                {
                    DateTime eTo = Convert.ToDateTime(effectiveTo);
                    cmdText += "[effectiveTo] = @effectiveTo,";
                    cmd.Parameters.Add(new SqlParameter("@effectiveTo", eTo));
                }
                else
                {
                    cmdText += "[effectiveTo] = null,";
                }

                cmdText += "[name] = @name,";
                cmd.Parameters.Add(new SqlParameter("@name", name));

                cmdText += "[UpdatedBy] = @UpdatedBy,";
                cmd.Parameters.Add(new SqlParameter("@UpdatedBy", updatedBy));

                cmdText += "[UpdatedDate] = @UpdatedDate where [ID]=@ID";
                cmd.Parameters.Add(new SqlParameter("@UpdatedDate", DateTime.Now));
                cmd.Parameters.Add(new SqlParameter("@ID", id));

                cmd.CommandText = cmdText;

                result = cmd.ExecuteNonQuery();
            }

            return result;
        }

        /// <summary>
        /// Delete a version from the database by ID
        /// </summary>
        /// <param name="id">version ID</param>
        /// <returns>Number of rows affected</returns>
        public int DeleteVersionByID(int id)
        {
            int result = -1;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("Update VersionTree Set [Valid]=0 WHERE ID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("ID", id);

                    result = cmd.ExecuteNonQuery();
                }
            }


            return result;
        }

        /// <summary>
        /// Delete a version from the database by type
        /// </summary>
        /// <param name="id">version type</param>
        /// <returns>Number of rows affected</returns>
        public int DeleteVersionByType(string type)
        {
            int result = -1;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("Update VersionTree set [Valid]=0 WHERE [type]=@Type", conn))
                {
                    cmd.Parameters.AddWithValue("Type", type);

                    result = cmd.ExecuteNonQuery();
                }
            }


            return result;
        }

        public List<VersionTree> GetEffectiveRotationPlans(string date)
        {
            List<VersionTree> result = new List<VersionTree>();
            DateTime tDate = Convert.ToDateTime(date);
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string cmdText = @"select v.[ID],v.[ParentID],v.[Name],v.[Type], v.[Remark], v.[CreatedBy], v.[CreatedDate], v.[UpdatedBy], v.[UpdatedDate],v.[Version],v.EffectiveFrom, v.EffectiveTo
                                   from VersionTree v inner join (
                                   select MAX(EffectiveFrom) effectFrom, type from VersionTree where TYPE like @type and EffectiveFrom < @date and valid = 1 and (EffectiveTo is null or EffectiveTo > @date)
                                   group by type
                                   ) t on v.EffectiveFrom = t.effectFrom and v.Type = t.Type";

                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("type", "%RotationPlan-%");
                cmd.Parameters.AddWithValue("date", tDate);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    VersionTree ver = new VersionTree();
                    ver.ID = r.GetInt32(0);
                    if (!(r.GetValue(1) is DBNull))
                        ver.ParentID = r.GetInt32(1);
                    ver.Name = r.GetString(2);
                    ver.Type = r.GetString(3);
                    if (!(r.GetValue(4) is DBNull))
                        ver.Remark = r.GetString(4);
                    ver.CreatedBy = r.GetInt16(5);
                    ver.CreatedDate = r.GetDateTime(6);
                    ver.UpdatedBy = r.GetInt16(7);
                    ver.UpdatedDate = r.GetDateTime(8);
                    ver.Version = r.GetString(9);

                    if (!(r.GetValue(10) is DBNull))
                        ver.EffectiveFrom = r.GetDateTime(10);
                    if (!(r.GetValue(11) is DBNull))
                        ver.EffectiveTo = r.GetDateTime(11);

                    result.Add(ver);
                }
                r.Close();                
            }

            return result;
        }

        
    }
}