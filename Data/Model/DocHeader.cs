﻿using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Model
{
    public class DocHeader
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public string  DocNum { get; set; }
        public string AcctCode { get; set; }
        public string AccName { get; set; }
        public string User { get; set; }
    }
}
