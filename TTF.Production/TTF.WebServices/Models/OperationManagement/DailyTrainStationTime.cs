///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>DailyTrainStationTime.cs</file>
///<description>
///DailyTrainStationTime stores the information about a train's planned arrival or departure at a particular station at a particular time.
///
///</description>
///

///
///<created>
///<author>Dr. Liu Qizhang</author>
///<date>18-12-2013</date>
///</created>
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class DailyTrainStationTime
    {
        private long _id;
        public long ID
        {
            get { return _id; }
            set { _id = value; }
        }

        private short _stationId;
        public short StationID
        {
            get { return _stationId; }
            set { _stationId = value; }
        }

        private string _station;
        public string Station
        {
            get { return _station; }
            set { _station = value; }
        }

        private short _lineId;
        public short LineID
        {
            get { return _lineId; }
            set { _lineId = value; }
        }

        private short _platformId;
        public short PlatformId
        {
            get { return _platformId; }
            set { _platformId = value; }
        }

        private string _platform;
        public string Platform
        {
            get { return _platform; }
            set { _platform = value; }
        }

        private short _boundId;
        public short BoundId
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

        private bool _isWPAssociated;
        public bool IsWPAssociated
        {
            get { return _isWPAssociated; }
            set { _isWPAssociated = value; }
        }
    }

}