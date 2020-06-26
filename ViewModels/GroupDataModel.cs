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
       
         public string Time { get; set; }

         public string Temperature { get; set; }

         public string Humidity { get; set; }
     
         public string Pressure { get; set; }
    }
}