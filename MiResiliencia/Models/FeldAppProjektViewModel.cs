
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MiResiliencia.Models
{
    public class FeldAppProjektViewModel
    {
        public Project Projekt { get; set; }

        //public Project Project { get; set; }

        public string ProjectID { get; set; }

        public Company Company { get; set; }
        public ApplicationUser User { get; set; }
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        public int CompanyID { get; set; }

    }
}