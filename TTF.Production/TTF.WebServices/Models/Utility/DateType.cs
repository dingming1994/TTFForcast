///Copyright (c) 2013 3ELOGIC Consultancy Pte. Ltd.
///All rights reserved.

///
///<file>DateType.cs</file>
///<description>
///DateType is the class that holds information about a date type.
///</description>
///

///
///<created>
///<author>Dr. Liu Qizhang</author>
///<date>18-12-2013</date>
///</created>
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Models
{
    public class DateType
    {
        private int _id;

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        
        private string _code;
        public string Code
        {
            get { return _code; }
            set { _code = value; }
        }

        private bool _includeMon;
        public bool IncludeMon
        {
            get { return _includeMon; }
            set { _includeMon = value; }
        }

        private bool _includeTue;
        public bool IncludeTue
        {
            get { return _includeTue; }
            set { _includeTue = value; }
        }

        private bool _includeWed;
        public bool IncludeWed
        {
            get { return _includeWed; }
            set { _includeWed = value; }
        }
        private bool _includeThu;
        public bool IncludeThu
        {
            get { return _includeThu; }
            set { _includeThu = value; }
        }
        private bool _includeFri;
        public bool IncludeFri
        {
            get { return _includeFri; }
            set { _includeFri = value; }
        }
        private bool _includeSat;
        public bool IncludeSat
        {
            get { return _includeSat; }
            set { _includeSat = value; }
        }
        private bool _includeSun;
        public bool IncludeSun
        {
            get { return _includeSun; }
            set { _includeSun = value; }
        }
        private bool _isSpecial;
        public bool IsSpecial
        {
            get { return _isSpecial; }
            set { _isSpecial = value; }
        }
    }

}