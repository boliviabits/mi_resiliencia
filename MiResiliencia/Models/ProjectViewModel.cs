using MiResiliencia.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MiResiliencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MiResiliencia.Models
{
    public class ProjectViewModel
    {
        public List<SelectListItem> optionList = new List<SelectListItem>();

        [Display(Name = "Project")]
        public Project Projekt { get; set; }
        public List<Project> _projects;


        public ProjectViewModel(ApplicationUser applicationUser)
        {
            _projects = new List<Project>();
            foreach (Company c in applicationUser.IsCompanyUserOf.Select(m=>m.Company))
            {
                foreach (Project p in c.Projects)
                {
                    SelectListItem s = new SelectListItem();
                    s.Text = p.Name;
                    s.Value = p.Id.ToString();

                    if ((applicationUser.MyWorkingProjekt != null) && (p.Id == applicationUser.MyWorkingProjekt.Id))
                    {
                        s.Selected = true;
                    }
                    _projects.Add(p);
                    optionList.Add(s);
                }
            }
        }
    }
}