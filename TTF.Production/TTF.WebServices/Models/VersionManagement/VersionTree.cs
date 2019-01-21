///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>VersionTree.cs</file>
///<description>
///VersionTree is the class that is the model of the corresponding VersionTree table in the database
///</description>
///

///
///<created>
///<author>Dr. Liu Qizhang</author>
///<date>21-11-2013</date>
///</created>
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class VersionTree
    {
        public int ID { get; set; }

        public int? ParentID { get; set; }

        public string Version { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string Remark { get; set; }

        public Int16 CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public Int16 UpdatedBy { get; set; }

        public DateTime UpdatedDate { get; set; }

    
        public DateTime?  EffectiveFrom { get; set; }

        public string EffectiveFrm { get; set; }
        

        public DateTime? EffectiveTo { get; set; }
    }
}