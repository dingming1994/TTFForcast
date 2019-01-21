using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class Bound
    {
        public short ID { get; set; }
        public short LineId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string BoundType { get; set; }
        public string Line { get; set; }
        public string StationDetails { get; set; }
        public List<Station> Stns { get; set; }

        public int FrmStn { get; set; }
        public int ToStn { get; set; }
        public int VerId { get; set; }
        public int BoundTypeId { get; set; }
    }

    public class BoundType
    {
        public int Id { get; set; }
        public string BndType { get; set; }
    }
}