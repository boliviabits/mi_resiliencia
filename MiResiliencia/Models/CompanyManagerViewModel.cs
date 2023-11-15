using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MiResiliencia.Models
{
    public class CompanyManagerViewModel
    {

        public Controllers.Rights Rights { get; set; }
        public List<Company> editableCompanies { get; set; }
        public Company NewCompany { get; set; }

    }
}