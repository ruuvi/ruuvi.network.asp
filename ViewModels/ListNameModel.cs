using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RuuviTagApp.ViewModels
{
    public class ListNameModel
    {
        [Required(ErrorMessage = "List name is required.")]
        [MaxLength(50, ErrorMessage = "Tag list name character limit is 50.")]
        public string ListName { get; set; }
    }
}