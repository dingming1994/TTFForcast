///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>DailyActualStaitonTime.cs</file>
///<description>
///DailyActualStaitonTime stores the information about a train's actual arrival or departure at a particular station at a particular time.
///
///</description>
///

///
///<created>
///<author>Dr. Liu Qizhang</author>
///<date>11-01-2014</date>
///</created>
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class DailyActualStationTime
    {
        private int _id;
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }

        private short? _lineId;
        public short? LineID
        {
            get { return _lineId; }
            set { _lineId = value; }
        }

        private string _station;
        public string Station
        {
            get { return _station; }
            set { _station = value; }
        }

       
        private string _platform;
        public string Platform
        {
            get { return _platform; }
            set { _platform = value; }
        }

        private short? _boundId;
        public short? BoundId
        {
            get { return _boundId; }
            set { _boundId = value; }
        }

        private DateTime _time;
        public DateTime Time
        {
            get { return _time; }
            set { _time = value; }
        }

        private string _type;
        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private int? _trainCaptainId;
        public int? TrainCaptainId
        {
            get { return _trainCaptainId; }
            set { _trainCaptainId = value; }
        }

        private string _trainno;
        public string TrainNo
        {
            get { return _trainno; }
            set { _trainno = value; }
        }

        private string _trainnoAlias;
        public string TrainNoAlias
        {
            get { return _trainnoAlias; }
            set { _trainnoAlias = value; }
        }

        private string _EMUNo;
        public string EMUNo
        {
            get { return _EMUNo; }
            set { _EMUNo = value; }
        }

        private string _scheduleNo;
        public string ScheduleNo
        {
            get { return _scheduleNo; }
            set { _scheduleNo = value; }
        }

        private long? _plannedStationTime;
        public long? PlannedStationTime
        {
            get { return _plannedStationTime; }
            set { _plannedStationTime = value; }
        }

        private int? _deviation;
        public int? Deviation
        {
            get { return _deviation; }
            set { _deviation = value; }
        }

        private string _alertID;
        public string AlertID
        {
            get { return _alertID; }
            set { _alertID = value; }
        }

        public string PlannedTrainNo()
        {
            if (_trainnoAlias == null || _trainnoAlias == "")
                return _trainno;
            else
                return _trainnoAlias;
        }
    }

}