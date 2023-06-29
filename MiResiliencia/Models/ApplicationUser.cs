using Microsoft.AspNetCore.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace MiResiliencia.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public virtual List<CompanyAdmin> IsCompanyAdminOf { get; set; }
        public virtual List<CompanyUser> IsCompanyUserOf { get; set; }

        public virtual Project? MyWorkingProjekt { get; set; }


        public virtual Company MainCompany { get; set; }
        public int? MainCompanyID { get; set; }

        public virtual UserSettings? UserSettings { get; set; }

        public ApplicationUser()
        {
            IsCompanyAdminOf = new List<CompanyAdmin>();
            IsCompanyUserOf = new List<CompanyUser>();

        }

        [NotMapped]
        public string FullName { get { return FirstName + " " + LastName; } }

        /*public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.FindByIdAsync
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }*/
    }
}
