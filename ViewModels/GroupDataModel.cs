using RuuviTagApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RuuviTagApp.ViewModels
{
    public class GroupDataModel
    {
        public int TagID { get; set; }
        public string TagDisplay { get; set; }
        public List<UnpackData> TagData { get; set; }
    }
}