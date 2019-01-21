using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class Platform
    {
        public short ID { get; set; }
        public short StationID { get; set; }
        public string Code { get; set; }
        public bool Serviceable { get; set; }
        public int VerId { get; set; }
        
        public Platform()
        {

        }

        public Platform(short id, short stationID)
        {
            this.ID = id;
            this.StationID = stationID;
        }

    }
}