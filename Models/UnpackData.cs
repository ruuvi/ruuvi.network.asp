using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RuuviTagApp.Models
{
    public class UnpackData
    {
        public UnpackRawData Data { get; set; }
        public DateTime Time { get; set; }
    }
}