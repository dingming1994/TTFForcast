///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>DataManager.cs</file>
///<description>
///DataManager is the data repository for daily Operation Management. It keeps the dynamic status of train and tc. Together with the data saved in database, it provides 
///all data necessary for operation management.
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
using TTF.Models;
using TTF.DataLayer;

namespace TTF.Models
{
    public class DataManager
    {
        public const int MaxGapBtwPlanAndActual = 900; //The maximum possible discrepancy between planned stationtime and actual stationtime

        //This dictionary is to store all those trainnos who cannot find mapping emu number
        //Only the first time a trainno cannot find mapping emu number will be "Guessed" the emu number
        private Dictionary<string, string> mapUnmappedTrainNo;
        public Dictionary<string, string> MapUnmappedTrainNo
        {
            get { return mapUnmappedTrainNo; }
            set { mapUnmappedTrainNo = value; }
        }

        private VersionTree _networkVersion;
        public VersionTree NetworkVersion
        {
            get { return _networkVersion; }
            set { _networkVersion = value; }
        }

        //private int _dateType;
        //public int DateType
        //{
        //    get { return _dateType; }
        //    set { _dateType = value; }
        //}


        private Dictionary<string, List<DailyTrainStationTime>> _mapTrainStationTime; //key: train no, value: list of stationtime of the train
        public Dictionary<string, List<DailyTrainStationTime>> MapTrainStationTime
        {
            get { return _mapTrainStationTime; }
            set { _mapTrainStationTime = value; }
        }

        private Dictionary<string, Platform> _mapPlatform; //key: platform code, value: platform object.
        public Dictionary<string, Platform> MapPlatform
        {
            get { return _mapPlatform; }
            set { _mapPlatform = value; }
        }

        private Dictionary<short, Station> _mapStation; //key: station id, value: station object.
        public Dictionary<short, Station> MapStation
        {
            get { return _mapStation; }
            set { _mapStation = value; }
        }

        private List<string> _listTerminalPlatforms; //key: station id, value: station object.
        public List<string> ListTerminalPlatforms
        {
            get { return _listTerminalPlatforms; }
            set { _listTerminalPlatforms = value; }
        }

        private bool _hasServiceStarted=false;
        public bool HasServiceStarted
        {
            get { return _hasServiceStarted; }
            set { _hasServiceStarted = value; }
        }

        private static DataManager _instance;
        private DataManager()
        {
       //     (new UtilityDL()).GetTimeTableDateTypeID(DateTime.Now.ToString("yyyy-MM-dd"));
            mapUnmappedTrainNo = new Dictionary<string, string>();
        }

        public static DataManager Instance()
        {
            if (_instance == null)
            {
                _instance = new DataManager();
            }

            if (_instance.MapUnmappedTrainNo == null)
                _instance.MapUnmappedTrainNo = new Dictionary<string, string>();

            return _instance;
        }

        public void Reload(string date)
        {
            _hasServiceStarted = false;
            _mapTrainStationTime = new Dictionary<string, List<DailyTrainStationTime>>();
            _mapPlatform = new Dictionary<string, Platform>();
            _mapStation = new Dictionary<short, Station>();
            _listTerminalPlatforms = new List<string>();
        }

        public DateTime OperationDate()
        {
            return DateTime.Now;
        }
    }

}