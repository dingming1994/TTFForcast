using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace TTF.Models
{
    public class Interchange
    {
        public short ID { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public int VersionId { get; set; }

        public List<Line> LineDetails { get; set; }
    }
}