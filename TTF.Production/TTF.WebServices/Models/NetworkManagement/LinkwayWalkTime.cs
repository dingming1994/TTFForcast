using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class LinkwayWalkTime
    {
        public short Stn1 { get; set; }
        public short Stn2 { get; set; }
        public int VerId { get; set; }
        public int WalkTime { get; set; }
        public short ID { get; set; }
    }
}