using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RuuviTagApp.ViewModels
{
    public class AddTagToListModel
    {
        [Required]
        public int TagsId { get; set; }
        [Required]
        public int ListsId { get; set; }
    }
}