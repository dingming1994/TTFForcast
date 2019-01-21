using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models.NetworkManagement
{
    public class TrainCab
    {
        private string _trainType;

        public string TrainType
        {
            get { return _trainType; }
            set { _trainType = value; }
        }
        private string _emuCode;

        public string EmuCode
        {
            get { return _emuCode; }
            set { _emuCode = value; }
        }
    }
}