using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;

namespace TTF.Utils
{
    public static class Conversion
    {
        public static DateTime GetDateFromUIDateString(string date)
        {
            return DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        public static string GetWSDateStringFromDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
        public static string GetFullWSDateStringFromDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public static string GetTimeStringFromDate(DateTime date)
        {
            string timestring = "";
            try
            {
                timestring = date.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error parsing date string", ex);
            }
            return timestring;
        }
        public static string GetTimeStringFromDate(string date)
        {
            string timestring = "";
            try
            {
                timestring = DateTime.Parse(date).ToString("HH:mm:ss");
            }catch(Exception ex)
            {
                Logging.log.Error("Error parsing date string",ex);
            }
            return timestring;
        }
        public static int GetIntFromString(string intvalue)
        {
            int val = 0;
            try
            {
                val = int.Parse(intvalue);
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error parsing integer", ex);
            }
            return val;
        }
        public static long GetLongFromString(string longvalue)
        {
            long val = 0;
            try
            {
                val = long.Parse(longvalue);
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error parsing long ", ex);
            }
            return val;
        }
        public static float GetFloatFromString(string floatvalue)
        {
            float val = 0;
            try
            {
                val = float.Parse(floatvalue);
            }
            catch (Exception ex)
            {
                Logging.log.Error("Error parsing float value", ex);
            }
            return val;
        }
    }
}

