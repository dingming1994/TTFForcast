using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTF.Utils
{
    public class TimeHelper
    {

        public static DateTime ToExactTimeInDate(string timeStr, DateTime sourceDate, DateTime targetDate)
        {
            DateTime time = Convert.ToDateTime(timeStr);
            double seconds = (time - sourceDate).TotalSeconds;
            string dateStr = targetDate.ToString(CommonConstants.DEFAULT_DATE_FORMAT);
            return Convert.ToDateTime(dateStr + " 00:00:00").AddSeconds(seconds);
        }

    }
}
