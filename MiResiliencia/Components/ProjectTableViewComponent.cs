using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiResiliencia.Models;
using System.Security.Claims;

namespace MiResiliencia.Components
{
    public class ProjectTableViewComponent : ViewComponent
    {
        private UserManager<ApplicationUser> _userManager;
        private MiResilienciaContext _context;

        public ProjectTableViewComponent(UserManager<ApplicationUser> userManager, MiResilienciaContext context)
        {
            _userManager = userManager;
            _context = context;
        }


        /// <summary>
        /// Get All Projects and check if user can edit it
        /// </summary>
        /// <returns></returns>
        public async Task<IViewComponentResult> InvokeAsync(string idlist)
        {
            if (idlist == null)
            {
                ViewBag.HideDiv = true;
                return View(await getMyProjectList());
            }

            ViewBag.HideDiv = false;
            List<string> ids = idlist.Split(',').ToList();
            List<ProjectTableViewModel> myps = await getMyProjectList();
            List<ProjectTableViewModel> idps = myps.Where(m => ids.Contains(m.Project.Id.ToString())).ToList();

            return View(idps);
        }


        public async Task<List<ProjectTableViewModel>> getMyProjectList()
        {
            List<Project> AllProjects = _context.Projects.Include(m => m.ProjectState).Where(m => m.ProjectState.Description != "Deleted").ToList();

            var id = _userManager.GetUserId((ClaimsPrincipal)User);
            var applicationUser = await _userManager.GetUserAsync((ClaimsPrincipal)User);
            await _context.Entry(applicationUser).Reference(m => m.MyWorkingProjekt).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.MainCompany).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.UserSettings).LoadAsync();

            await _context.Entry(applicationUser).Collection(m => m.IsCompanyUserOf).Query().Include(m => m.Company).ThenInclude(m => m.Projects).LoadAsync();

            List<Project> MyProjects = new List<Project>();
            MyProjects.AddRange(applicationUser.IsCompanyUserOf.Select(m => m.Company).SelectMany(x => x.Projects));
            MyProjects.AddRange(applicationUser.IsCompanyAdminOf.Select(m => m.Company).SelectMany(x => x.Projects));


            List<ProjectTableViewModel> allProjectsVM = new List<ProjectTableViewModel>();
            foreach (Project pro in AllProjects)
            {
                ProjectTableViewModel ptvm = new ProjectTableViewModel();
                ptvm.Project = pro;
                if (MyProjects.Where(m => m.Id == pro.Id).Count() > 0) ptvm.CanUserEdit = true;
                else ptvm.CanUserEdit = false;

                allProjectsVM.Add(ptvm);
            }
            return allProjectsVM;
        }


    }
}
