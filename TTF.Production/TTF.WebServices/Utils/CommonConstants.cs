using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Utils
{
    public class CommonConstants
    {
        public const string DEFAULT_DATE_FORMAT = "yyyy-MM-dd";

        public const string DEFAULT_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

        public const string TIME_PART_FORMAT = "HH:mm:ss";
        
        public static DateTime PLANNED_PIECE_START_POINT = DateTime.Parse("1900-01-01 00:00:00");
    }
}