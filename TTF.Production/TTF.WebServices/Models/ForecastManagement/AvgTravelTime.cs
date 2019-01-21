///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>TrainMovement.cs</file>
///<description>
///TrainMovement stores the current status of each train in operation
///
///</description>
///

///
///<created>
///<author>Dr. Liu Qizhang</author>
///<date>02-10-2014</date>
///</created>
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class AvgTravelTime
    {
        public string fromPlat;
        public string toPlat;
        public TimeSpan fromTime;
        public TimeSpan toTime;
        public int travelTime;
    }

}