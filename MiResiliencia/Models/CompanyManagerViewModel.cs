using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class CompanyManagerViewModel
    {

        // chs public Controllers.AdminController.Rights Rights { get; set; }
        // chs public SelectList Companies { get; set; }
        public List<Company> editableCompanies { get; set; }
        public Company NewCompany { get; set; }

    }
}