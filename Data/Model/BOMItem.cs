using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Data.Model
{
    class BOMItem
    {
        [AutoIncrement,PrimaryKey]
        public int ID { get; set; }
        public string PackCode { get; set; }
        public string ItemCode { get; set; }
        public int Qty { get; set; }
    }
}
