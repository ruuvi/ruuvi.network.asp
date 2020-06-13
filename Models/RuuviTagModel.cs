using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RuuviTagApp.Models
{
    [Table("RuuviTags")]
    public class RuuviTagModel
    {
        [Key]
        public int TagId { get; set; }
        [Display(Name = "Tag mac address")]
        [Required(ErrorMessage = "Mac address is required")]
        [StringLength(12, MinimumLength = 12, ErrorMessage = "A valid mac address is 12 characters long")]
        public string TagMacAddress { get; set; }
        [Required]
        public bool TagActive { get; set; } = true;
        [StringLength(50, MinimumLength = 3, ErrorMessage = "A valid name is between 3 and 50 characters")]
        public string TagName { get; set; }
        [StringLength(128)]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser ApplicationUser { get; set; }
    }
}