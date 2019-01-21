///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>StnToStnTravelTimeToday.cs</file>
///<description>
///StnToStnTravelTimeToday stores latest records of the travel time between stations
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
    public class StnToStnTravelTimeToday
    {
        private string _fromStn;
        public string FromStn
        {
            get { return _fromStn; }
            set { _fromStn = value; }
        }

        private string _toStn;
        public string ToStn
        {
            get { return _toStn; }
            set { _toStn = value; }
        }

        

        private short _travelTime;
        public short TravelTime
        {
            get { return _travelTime; }
            set { _travelTime = value; }
        }

        private DateTime _timeCreated;
        public DateTime TimeCreated
        {
            get { return _timeCreated; }
            set { _timeCreated = value; }
        }
    }

}