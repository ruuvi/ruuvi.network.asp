using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RuuviTagApp.Models
{
    [Table("UserTagLists")]
    public class UserTagListModel
    {
        [Key]
        public int UserTagListId { get; set; }
        [Required]
        [StringLength(50)]
        [Index("ListNameIndex", IsClustered = false, IsUnique = false)]
        public string ListName { get; set; }
        [Required]
        [StringLength(128)]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser ApplicationUser { get; set; }
        public virtual ICollection<TagListRowModel> TagListRowModels { get; set; }
    }
}