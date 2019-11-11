using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Model
{
    public class DBInfo
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public string DBPath { get; set; }
        public string APIPath { get; set; }
        public string SerNum { get; set; }
        public string Auth { get; set; }
    }
}
