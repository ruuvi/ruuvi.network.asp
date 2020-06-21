using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RuuviTagApp.Models
{
    [Table("TagAlerts")]
    public class TagAlertModel
    {
        [Key]
        public int AlertId { get; set; }
        [Required]
        public double AlertLimit { get; set; }
        [Required]
        public int AlertTypeId { get; set; }
        [ForeignKey("AlertTypeId")]
        public virtual TagAlertType TagAlertType { get; set; }
        [Required]
        public int TagId { get; set; }
        [ForeignKey("TagId")]
        public virtual RuuviTagModel RuuviTagModel { get; set; }
    }
}