///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>DailyForecastedArrivalTime.cs</file>
///<description>
///DailyForecastedArrivalTime stores forecasted arrival time at each station
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
    public class DailyForecastedArrivalTime
    {
        private string _emuNo;
        public string EMUNo
        {
            get { return _emuNo; }
            set { _emuNo = value; }
        }

        private int _id;
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _trainNo;
        public string TrainNo
        {
            get { return _trainNo; }
            set { _trainNo = value; }
        }

        private string _trainNoAlias;
        public string TrainNoAlias
        {
            get { return _trainNoAlias; }
            set { _trainNoAlias = value; }
        }

        private int? _plannedStationTime;
        public int? PlannedStationTime
        {
            get { return _plannedStationTime; }
            set { _plannedStationTime = value; }
        }

        private DateTime _time;
        public DateTime Time
        {
            get { return _time; }
            set { _time = value; }
        }

        private short _stn;
        public short Stn
        {
            get { return _stn; }
            set { _stn = value; }
        }

        private string _stnCode;
        public string StnCode
        {
            get { return _stnCode; }
            set { _stnCode = value; }
        }

        private short _bound;
        public short Bound
        {
            get { return _bound; }
            set { _bound = value; }
        }

        private bool _isService;
        public bool IsService
        {
            get { return _isService; }
            set { _isService = value; }
        }

        private string _platform;
        public string Platform
        {
            get { return _platform; }
            set { _platform = value; }
        }
    }

}