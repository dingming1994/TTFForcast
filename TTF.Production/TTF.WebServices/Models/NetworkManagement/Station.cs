using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class Station
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public int VerId { get; set; }
        public int Position { get; set; }

        private string title = String.Empty;
        private short id = 0;
        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                if (this.title != value)
                {
                    this.title = value;
                }
            }
        }
        public short ID
        {
            get
            {
                return this.id;
            }
            set
            {
                if (this.id != value)
                {
                    this.id = value;
                }
            }
        }

        //used in Station Management
        public string LnName { get; set; }
        public string LnCode { get; set; }
        public string StnType { get; set; }
        public string IsInterchange { get; set; }
        public string IsDutyPoint { get; set; }
        public string BelongTo { get; set; }
        public string PlatFormList { get; set; }

        public string InterchangeId { get; set; }
        public string CrewPointId { get; set; }
        public int StnNo { get; set; }
        public short lnId { get; set; }
        public short IsCrewPoint { get; set; }
        public short IsTerminal { get; set; }

        public List<Platform> PtfLst { get; set; }
        public int StnTypeId { get; set; }
        public string Type { get; set; }
    }
}