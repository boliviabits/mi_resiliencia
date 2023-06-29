using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiResiliencia.Models
{
    public class ProjectManagerViewModel
    {
        public Controllers.Rights Rights { get; set; }
        public SelectList Companies { get; set; }
        public Company EditCompany { get; set; }
        public int EditCompanyId { get; set; }
        private Project _project;

        public List<Project> Projects { get; set; }
        public Project NewProject
        {
            get
            {
                if (_project != null) return _project;
                _project = new Project();
                return _project;
            }
            set { _project = value; }
        }

    }
}