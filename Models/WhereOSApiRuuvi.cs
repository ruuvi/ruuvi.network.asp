using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Web;

namespace RuuviTagApp.Models
{
    public class WhereOSApiRuuvi
    {
        public string coordinates { get; set; }
        public string data { get; set; }
        public string gwmac { get; set; }
        public string id { get; set; }
        public int rssi { get; set; }
        public int rssi_max { get; set; }
        public int rssi_min { get; set; }
        public DateTime time { get; set; }

    }
}