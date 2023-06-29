using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;

namespace MiResiliencia.Models
{
    public class Company
    {
        
        public int ID { get; set; }

        [DisplayName("Nombre")]
        public string CompanyName { get; set; }

        [DisplayName("Logo")]
        public int LogoID { get; set; }
        public virtual File Logo { get; set; }

        [DisplayName("FeldApp Projektnummer")]
        public long FeldAppProjectNumber { get; set; }

        [DisplayName("Texto de bienvenida")]
        public string? WelcomeMessage { get; set; }

        [DisplayName("Begrüssungstitel")]
        public string? WelcomeMessageTitle { get; set; }


        [DisplayName("Company Titel")]
        public string? CompanyTitle { get; set; }

        [DisplayName("Color de empresa (hexadecimal)")]
        public string? CompanyColor { get; set; }

        [DisplayName("Color RGBA (r,g,b,h)")]
        public string? CompanyRGBAColor { get; set; }

        public virtual List<CompanyAdmin> AdminUsers { get; set; }
        public virtual List<CompanyUser> CompanyUsers { get; set; }

        public virtual List<Project> Projects { get; set; }

        public virtual List<Company> AdminOfCompany { get; set; }
        public virtual Company? MySuperCompany { get; set; }

        public Company()
        {
            AdminUsers = new List<CompanyAdmin>();
            CompanyUsers = new List<CompanyUser>();
        }

    }
}