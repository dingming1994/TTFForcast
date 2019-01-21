using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace TTF.Models
{
      
    public class Line
    {
        public short ID {get; set;}
        public string Name { get; set; }

        // Added by Suraj
        public string Description { get; set; }
        public int VersionId { get; set; }

        public List<Bound> BoundDetails { get; set; }

    }

   
}