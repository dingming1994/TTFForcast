///
///<file>UtilityDL.cs</file>
///<description>
/// Data layer for managing basic data that is not maintained by other dedicated DL classes, but are commonly used by others.
/// So far, it handles data of the following table:
/// 
///  - DateType
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
namespace TTF.DataLayer
{
    public class UtilityDL
    {
        public int GetUserID()
        {
            // return 1;
            int uid = 0;
            try
            {



            }
            catch (Exception ex)
            {
                throw new ApplicationException("Fail to get the ID of the current user");
            }
            return uid;
        }

        /// <summary>
        /// Get DateType id of timetable for a given day
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public int GetTimeTableDateTypeID(string day)
        {
            bool hasSpecialVersion = (new VersionTreeDL()).HasSpecialVersion("Train Timetable%", day);

            if (hasSpecialVersion)
            {
                return GetEventDateTypeID();
            }
            else
            {
                DateTime date = Convert.ToDateTime(day);
                return GetDateTypeID(date.DayOfWeek);
            }
        }

        /// <summary>
        /// Get the datetype id for event date type
        /// </summary>
        /// <returns></returns>
        public int GetEventDateTypeID()
        {
            int id = -1;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [id]  FROM [DateType] WHERE isSpecial = 1", conn))
                {
                    conn.Open();
                    
                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        id = r.GetInt32(0);
                    }
                    r.Close();
                }
                conn.Close();
            }

            return id;
        }

        /// <summary>
        /// Get Datetype id for a given day of the week
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public int GetDateTypeID(DayOfWeek day)
        {
            int id = -1;
            string dayofweek = "";
            switch (day)
            {
                case DayOfWeek.Monday:
                    dayofweek = "IncludeMon";
                    break;
                case DayOfWeek.Tuesday:
                    dayofweek = "IncludeTue";
                    break;
                case DayOfWeek.Wednesday:
                    dayofweek = "IncludeWed";
                    break;
                case DayOfWeek.Thursday:
                    dayofweek = "IncludeTHU";
                    break;
                case DayOfWeek.Friday:
                    dayofweek = "IncludeFri";
                    break;
                case DayOfWeek.Saturday:
                    dayofweek = "IncludeSat";
                    break;
                case DayOfWeek.Sunday:
                    dayofweek = "IncludeSun";
                    break;
                default:
                    break;
            }
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT [id]  FROM [DateType] WHERE " + dayofweek + " = 1", conn))
                {
                    conn.Open();

                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        id = r.GetInt32(0);
                    }
                    r.Close();
                }
                conn.Close();
            }

            return id;
        }

        public List<DateType> GetDateTypes()
        {
            List<DateType> list = new List<DateType>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["TTFDB"].ConnectionString))
            {
                conn.Open();
                string cmdText = "select ID, [code], IncludeMon, IncludeTue, IncludeWed, IncludeThu,IncludeFri,IncludeSat,IncludeSun,IsSpecial " +
                                "from DateType"; 

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    SqlDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {
                        DateType piece = new DateType();

                        piece.Id = r.GetInt32(0);
                        piece.Code = r.GetString(1);
                        piece.IncludeMon = r.GetBoolean(2);
                        piece.IncludeTue = r.GetBoolean(3);
                        piece.IncludeWed = r.GetBoolean(4);
                        piece.IncludeThu = r.GetBoolean(5);
                        piece.IncludeFri = r.GetBoolean(6);
                        piece.IncludeSat = r.GetBoolean(7);
                        piece.IncludeSun = r.GetBoolean(8);
                        piece.IsSpecial = r.GetBoolean(9);
                        list.Add(piece);
                    }
                    r.Close();
                }
            }

            return list;
        }
    }
}