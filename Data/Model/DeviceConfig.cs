using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Model
{
    public class DeviceConfig
    {
        [AutoIncrement, PrimaryKey]
        public int ID { get; set; }
        public string DefaultAccWH { get; set; }
        public string DefaultRejWH { get; set; }
        public string ConnectionS { get; set; }
    }
}
