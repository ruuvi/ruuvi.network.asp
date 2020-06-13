using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RuuviTagApp.ViewModels
{
    public class AddTagModel : MacAddressModel
    {
        [StringLength(50, ErrorMessage = "Tag name cannot exceed 50 characters.")]
        public string TagName { get; set; }
    }
}