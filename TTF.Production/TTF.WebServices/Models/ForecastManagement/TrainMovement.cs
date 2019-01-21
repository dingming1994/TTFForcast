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
    public class TrainMovement
    {
        private int _id;
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }


        private string _emuNo;
        public string EMUNo
        {
            get { return _emuNo; }
            set { _emuNo = value; }
        }

        private string _trainNo;
        public string TrainNo
        {
            get { return _trainNo; }
            set { _trainNo = value; }
        }

        private string _platform;
        public string Platform
        {
            get { return _platform; }
            set { _platform = value; }
        }

        private string _trainNoAlias;
        public string TrainNoAlias
        {
            get { return _trainNoAlias; }
            set { _trainNoAlias = value; }
        }

        private short _lastStn;
        public short LastStn
        {
            get { return _lastStn; }
            set { _lastStn = value; }
        }

        private short? _bound;
        public short? Bound
        {
            get { return _bound; }
            set { _bound = value; }
        }

        private DateTime _lastSignalTime;
        public DateTime LastSignalTime
        {
            get { return _lastSignalTime; }
            set { _lastSignalTime = value; }
        }
    }

}