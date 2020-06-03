using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RuuviTagApp.Models
{
    [Table("TagListRows")]
    public class TagListRowModel
    {
        [Key]
        public int ListRowId { get; set; }
        [Required]
        public int TagId { get; set; }
        [ForeignKey("TagId")]
        public RuuviTagModel RuuviTagModel { get; set; }
        [Required]
        public int ListId { get; set; }
        [ForeignKey("ListId")]
        public UserTagListModel UserTagListModel { get; set; }
    }
}