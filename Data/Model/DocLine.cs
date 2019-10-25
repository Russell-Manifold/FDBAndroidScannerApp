using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Data.Model
{
    class DocLine
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public string DocNum { get; set; }
        public string ItemBarcode { get; set; }
        public string ItemDesc { get; set; }
        public int ItemQty { get; set; }
        public int PostQty { get; set; }
    }
}
