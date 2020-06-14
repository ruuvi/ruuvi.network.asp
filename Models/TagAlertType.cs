using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RuuviTagApp.Models
{
    public class TagAlertType
    {
        [Key]
        public int AlertTypeId { get; set; }
        [StringLength(50)]
        public string TypeName { get; set; }
    }
}