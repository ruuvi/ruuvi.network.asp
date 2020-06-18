using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RuuviTagApp.ViewModels
{
    public class AddAlarmModel
    {
        public double? TemperatureHigh { get; set; }
        public double? TemperatureLow { get; set; }
        public double? HumidityHigh { get; set; }
        public double? HumidityLow { get; set; }
        public double? PressureHigh { get; set; }
        public double? PressureLow { get; set; }

        public bool AllPropertiesAreNull => GetType().GetProperties().All(p => p.GetValue(this) == null);
    }
}