///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>StnToStnTravelTime.cs</file>
///<description>
///StnToStnTravelTime stores historical records of the travel time between stations
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
    public class StnToStnTravelTime
    {
        private string _fromPlat;
        public string FromPlat
        {
            get { return _fromPlat; }
            set { _fromPlat = value; }
        }

        private string _toPlat;
        public string ToPlat
        {
            get { return _toPlat; }
            set { _toPlat = value; }
        }

        

        private int _travelTime;
        public int TravelTime
        {
            get { return _travelTime; }
            set { _travelTime = value; }
        }

        private DateTime _arrivalTime;
        public DateTime ArrivalTime
        {
            get { return _arrivalTime; }
            set { _arrivalTime = value; }
        }

        
    }

}