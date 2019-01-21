using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class Crewpoint
    {
        public short ID { get; set; }
        public string Code { get; set; }

        //Added by Suraj 
        public string Description { get; set; }

        public List<CrewPointChildDetails> cpChdDetails { get; set; }

        public short lnId { get; set; } /* this property used while retriving CP for Data access control in Manage Role screen */
    }

    public class CrewPointChildDetails
    {
        public string Station { get; set; }
        public string TrainLine { get; set; }
        public string Stations { get; set; }
        public short LineID { get; set; }
    }
}
