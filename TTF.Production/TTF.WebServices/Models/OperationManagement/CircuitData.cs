using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models.OperationManagement
{
    public class CircuitData
    {
        private string _platformCode="";

        public string PlatformCode
        {
            get { return _platformCode; }
            set { _platformCode = value; }
        }
        private string _bound = "";

        public string Bound
        {
            get { return _bound; }
            set { _bound = value; }
        }
        private string _positionCode = "";

        public string PositionCode
        {
            get { return _positionCode; }
            set { _positionCode = value; }
        }

    }
}