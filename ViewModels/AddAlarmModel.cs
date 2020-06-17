using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RuuviTagApp.ViewModels
{
    public class AddAlarmModel
    {
        public double? TempHigh { get; set; }
        public double? TempLow { get; set; }
        public double? HumidityHigh { get; set; }
        public double? HumidityLow { get; set; }
        public double? PressureHigh { get; set; }
        public double? PressureLow { get; set; }
    }
}