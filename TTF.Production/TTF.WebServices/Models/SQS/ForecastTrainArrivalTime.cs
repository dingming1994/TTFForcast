using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models.SQS
{

    public struct ForecastedTrainArrivalTime
    {
        public String TrainNo;
        public String EmuNo;
        public DateTime Time;
        public int PlannedST;
        public String Bound;
        public String Platform;
    }
}