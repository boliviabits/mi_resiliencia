using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Data.SqlClient.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MiResiliencia.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NuGet.Packaging;
using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace MiResiliencia.Controllers
{
    public class AdminController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private MiResilienciaContext _context;
        private IConfiguration _configuration { get; }

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,  MiResilienciaContext context, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            Rights r = await GetMyRights();
            ViewBag.Rights = r;
            return View();
        }


        public async Task<IActionResult> ProjectManager()
        {
            ProjectManagerViewModel p = new ProjectManagerViewModel();
            p.Rights = await GetMyRights();

            p.Projects = new List<Project>();
            List<SelectListItem> editableCompanies = new List<SelectListItem>();
            foreach (Company c in p.Rights.ProjectForCompanies.Distinct())
            {
                editableCompanies.Add(new SelectListItem { Text = c.CompanyTitle, Value = c.ID.ToString() });

                List<Project> ps = c.Projects;
                foreach (Project pp in ps)
                {
                    _context.Entry(pp).Reference(m => m.Company).Load();
                    _context.Entry(pp).Reference(m => m.ProjectState).Load();
                    if (pp.ProjectState.ID != 5) p.Projects.Add(pp);
                }

            }

            p.Companies = new SelectList(editableCompanies, "Value", "Text");



            return View(p);
        }
        [HttpPost]
        public async Task<IActionResult> ProjectManager(ProjectManagerViewModel p)
        {
            try
            {
                p.EditCompany = _context.Companies.Find(p.EditCompanyId);

                List<Standard_PrA> standardPrA = _context.StandardPrAs.Include(m => m.IKClasses).Include(m => m.NatHazard).ToList();
                p.NewProject.PrAs = new List<PrA>();
                foreach (Standard_PrA s in standardPrA)
                {
                    PrA npra = new PrA();
                    npra.IKClassesId = s.IKClassesId;
                    npra.NatHazardId = s.NatHazardId;
                    npra.Project = p.NewProject;
                    npra.Value = s.Value;
                    p.NewProject.PrAs.Add(npra);
                }


                p.NewProject.Company = p.EditCompany;
                p.NewProject.CoordinateSystem = CoordinateSystem.WGS84;
                p.NewProject.ProjectState = _context.ProjectStates.Where(u => u.ID == 1).FirstOrDefault();
                _context.Projects.Add(p.NewProject);

                _context.SaveChanges();
                p.Rights = await GetMyRights();


                ViewBag.TheResult = true;
                ViewBag.SuccessMessage = "Proyecto creado con éxito";

            }
            catch (Exception e)
            {
                ViewBag.TheResult = false;
                ViewBag.Error = e.ToString();
            }

            p = new ProjectManagerViewModel();
            p.Rights = await GetMyRights();

            p.Projects = new List<Project>();
            List<SelectListItem> editableCompanies = new List<SelectListItem>();
            foreach (Company c in p.Rights.ProjectForCompanies.Distinct())
            {
                editableCompanies.Add(new SelectListItem { Text = c.CompanyTitle, Value = c.ID.ToString() });
                List<Project> ps = c.Projects;
                foreach (Project pp in ps)
                {
                    _context.Entry(pp).Reference(m => m.Company).Load();
                    _context.Entry(pp).Reference(m => m.ProjectState).Load();

                    if (pp.ProjectState.ID != 5) p.Projects.Add(pp);
                }
            }

            p.Companies = new SelectList(editableCompanies, "Value", "Text");



            return View(p);

        }

        public async Task<IActionResult> UserManager()
        {
            UserManagerViewModel p = new UserManagerViewModel();
            p.Rights = await GetMyRights();

            List<SelectListItem> editableCompanies = new List<SelectListItem>();
            foreach (Company c in p.Rights.UserForCompanies.Distinct())
            {
                editableCompanies.Add(new SelectListItem { Text = c.CompanyTitle, Value = c.ID.ToString() });
            }

            List<ApplicationUser> userlist = new List<ApplicationUser>();
            foreach (Company c in p.Rights.UserForCompanies.Distinct())
            {
                _context.Entry(c).Collection(x => x.AdminUsers).Load();
                _context.Entry(c).Collection(x => x.CompanyUsers).Load();
                userlist.AddRange(c.AdminUsers.Select(m=>m.User));
                userlist.AddRange(c.CompanyUsers.Select(m=>m.User));
            }

            ViewBag.UserList = userlist.Distinct().OrderBy(m => m.FirstName);
            p.Companies = new SelectList(editableCompanies, "Value", "Text");

            return View(p);

        }
        [HttpPost]
        public async Task<IActionResult> UserManager(UserManagerViewModel p)
        {
            ModelState["Rights"]?.Errors.Clear();
            ModelState["Companies"]?.Errors.Clear();

                ApplicationUser newUser = new ApplicationUser();
                newUser.UserName = p.NewUser.Username;
                newUser.Email = p.NewUser.Email;
                newUser.LastName = p.NewUser.LastName;
                newUser.FirstName = p.NewUser.FirstName;
                newUser.EmailConfirmed = true;

                var result = await _userManager.CreateAsync(newUser, p.NewUser.Password);
                if (result.Succeeded)
                {
                    Company c = _context.Companies.Find(p.NewUser.CompanyID);

                    var newUserCreated = (ApplicationUser)_context.Users.Find(newUser.Id);
                    CompanyUser cm = new CompanyUser() { User = newUserCreated, Company = c };
                    newUserCreated.IsCompanyUserOf.Add(cm);
                    newUserCreated.MainCompany = c;

                    if (p.NewUser.isAdmin)
                    {
                        CompanyAdmin ca = new CompanyAdmin() { Company = c, User = newUserCreated };
                        newUserCreated.IsCompanyAdminOf.Add(ca);
                        /*if (c.MySuperCompany == null)
                        {
                            await _userManager.AddToRoleAsync(newUser, "CompanyCreator");
                        }*/


                    }


                    UserSettings us = new UserSettings();
                    us.DrawWindowX = 1129; us.DrawWindowY = 9; us.DrawWindowWidth = 350; us.DrawWindowHeight = 294; us.LayerWindowX = 1129; us.LayerWindowY = 312; us.LayerWindowWidth = 350; us.LayerWindowHeight = 388;



                    newUserCreated.UserSettings = us;


                    _context.Entry(newUserCreated).State = EntityState.Modified;

                    _context.SaveChanges();
                    ViewBag.TheResult = true;
                    ViewBag.SuccessMessage = "Usuario creado con éxito";
                
            }
            else
            {
                ViewBag.TheResult = false;
                //ViewBag.Error = ModelState.Values.SelectMany(x => x.Errors).First().ErrorMessage;
                string error = result.Errors.First().Description;
                if (error.Contains("already exists"))  error = "El nombre de usuario ya existe";
                else error = "Se ha producido un error, por favor compruebe la entrada de nuevo.";
                ViewBag.Error = error;

            }

            p.Rights = await GetMyRights();
            List<SelectListItem> editableCompanies = new List<SelectListItem>();
            foreach (Company c in p.Rights.ProjectForCompanies.Distinct())
            {
                editableCompanies.Add(new SelectListItem { Text = c.CompanyTitle, Value = c.ID.ToString() });
            }
            List<ApplicationUser> userlist = new List<ApplicationUser>();
            foreach (Company c in p.Rights.UserForCompanies.Distinct())
            {
                _context.Entry(c).Collection(x => x.AdminUsers).Load();
                _context.Entry(c).Collection(x => x.CompanyUsers).Load();
                userlist.AddRange(c.AdminUsers.Select(m=>m.User));
                userlist.AddRange(c.CompanyUsers.Select(m => m.User));
            }

            ViewBag.UserList = userlist.Distinct();

            p.Companies = new SelectList(editableCompanies, "Value", "Text");

            return View(p);

        }

        public async Task<IActionResult> UserEdit(string id)
        {
            ApplicationUser u = (ApplicationUser)_context.Users.Find(id);
            if (u == null) return new NotFoundResult();
            UserEditViewModel r = new UserEditViewModel();
            r.Email = u.Email;
            r.FirstName = u.FirstName;
            r.LastName = u.LastName;
            r.Username = u.UserName;
            r.Id = u.Id;
            _context.Entry(u).Collection(x => x.IsCompanyAdminOf).Load();
            if (u.IsCompanyAdminOf.Count > 0) r.isAdmin = true;
            return View(r);
        }
        [HttpPost]
        public async Task<ActionResult> UserEdit(UserEditViewModel r)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser u = (ApplicationUser)_context.Users.Find(r.Id);
                if (u == null) return new NotFoundResult();
                u.Email = r.Email;
                u.FirstName = r.FirstName;
                u.LastName = r.LastName;
                u.UserName = r.Username;
                if (r.Password != null)
                {
                    await _userManager.RemovePasswordAsync(u);
                    IdentityResult i = await _userManager.AddPasswordAsync(u, r.Password);
                    if (i.Errors.Count() > 0)
                    {

                    }
                }

                if (r.isAdmin)
                {
                    _context.Entry(u).Collection(x => x.IsCompanyAdminOf).Load();
                    _context.Entry(u).Collection(x => x.IsCompanyUserOf).Load();
                    
                    // chs TODO
                    /*if (u.IsCompanyAdminOf.Count == 0)
                    {
                        u.IsCompanyAdminOf.Add(u.IsCompanyUserOf.First());
                    }
                    if (u.IsCompanyUserOf.First().MySuperCompany == null)
                    {

                        AppUserManager.AddToRole(u.Id, "CompanyCreator");
                    }*/
                }
                else
                {
                    if (u.IsCompanyAdminOf.Count > 0) u.IsCompanyAdminOf.Clear();
                }

                _context.Entry(u).State = EntityState.Modified;
                _context.SaveChanges();
                return RedirectToAction("UserManager");
            }
            return View(r);

        }

        public async Task<IActionResult> CompanyManager()
        {
            CompanyManagerViewModel cmvm = new CompanyManagerViewModel();
            cmvm.Rights = await GetMyRights();

            cmvm.editableCompanies = new List<Company>();
            foreach (Company c in cmvm.Rights.EditableCompanies.Distinct())
            {
                cmvm.editableCompanies.Add(c);
            }

            return View(cmvm);

        }
        [HttpPost]
        public async Task<IActionResult> CompanyManager(CompanyManagerViewModel p)
        {
            ModelState["Rights"]?.Errors.Clear();
            ModelState["Companies"]?.Errors.Clear();
try {
            Company c = new Company();
            c.CompanyName = p.NewCompany.CompanyName;
            c.CompanyTitle = p.NewCompany.CompanyName;
            c.LogoID = 1;
            _context.Companies.Add(c);

            var id = _userManager.GetUserId(User);
            ApplicationUser u = await _userManager.GetUserAsync(User);
            CompanyAdmin ca = new CompanyAdmin() {Company = c, User = u};
            u.IsCompanyAdminOf.Add(ca);
            _context.Entry(u).State = EntityState.Modified;

            _context.SaveChanges();

}
catch (Exception ex)
{
                ViewBag.TheResult = false;
                //ViewBag.Error = ModelState.Values.SelectMany(x => x.Errors).First().ErrorMessage;
                string error = ex.ToString();
                if (error.Contains("already exists"))  error = "El nombre de usuario ya existe";
                else error = "Se ha producido un error, por favor compruebe la entrada de nuevo.";
                ViewBag.Error = error;

            }

            CompanyManagerViewModel cmvm = new CompanyManagerViewModel();
            cmvm.Rights = await GetMyRights();

            cmvm.editableCompanies = new List<Company>();
            foreach (Company c in cmvm.Rights.EditableCompanies.Distinct())
            {
                cmvm.editableCompanies.Add(c);
            }

            return View(cmvm);

        }


        private async Task<Rights> GetMyRights()
        {
            Rights r = new Rights();

            var id = _userManager.GetUserId(User);
            var applicationUser = await _userManager.GetUserAsync(User);
            await _context.Entry(applicationUser).Reference(m => m.MyWorkingProjekt).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.MainCompany).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.UserSettings).LoadAsync();

            await _context.Entry(applicationUser).Collection(m => m.IsCompanyUserOf).Query().Include(m => m.Company).ThenInclude(m => m.Projects).LoadAsync();
            await _context.Entry(applicationUser).Collection(m => m.IsCompanyAdminOf).Query().Include(m => m.Company).ThenInclude(m => m.Projects).LoadAsync();

            var roles = await _userManager.GetRolesAsync(applicationUser);



            // SuperAdmin. Can do everything
            if (roles.Where(m => m.Contains("Admin")).Any())
            {
                r.CanCreateCompany = true;
                r.CanCreateProjects = true;
                r.CanCreateUsers = true;
                r.EditableCompanies = await _context.Companies.ToListAsync();
                r.UserForCompanies = r.EditableCompanies;
                return r;
            }

            if (applicationUser.IsCompanyAdminOf.Count > 0)
            {
                r.CanCreateUsers = true;
                r.EditableCompanies = applicationUser.IsCompanyAdminOf.Select(m => m.Company).ToList();
                r.UserForCompanies = applicationUser.IsCompanyAdminOf.Select(m=>m.Company).ToList();
                r.CanCreateProjects = true;
                r.ProjectForCompanies = applicationUser.IsCompanyAdminOf.Select(m => m.Company).ToList();
            }

            // CompanyCreator
            if (roles.Where(m => m.Contains("CompanyCreator")).Any() || (applicationUser.UserName.ToLower() == "christoph.suter@geotest.ch" ))
            {
                r.CanCreateCompany = true;
                r.EditableCompanies = await _context.Companies.ToListAsync();
            }

            // go through all subcompanies and add them to editable Companies
            foreach (Company c in applicationUser.IsCompanyAdminOf.Select(m=>m.Company).ToList())
            {
                r.CanCreateUsers = true;
                r.EditableCompanies.AddRange(c.AdminOfCompany);
                r.UserForCompanies.AddRange(c.AdminOfCompany);
                r.CanCreateProjects = true;
                r.ProjectForCompanies.AddRange(c.AdminOfCompany);

                foreach (Company co in c.AdminOfCompany)
                {
                    r.CanCreateProjects = true;
                    r.EditableCompanies.Add(co);
                    r.ProjectForCompanies.AddRange(c.AdminOfCompany);
                }
            }
            foreach (Company c in applicationUser.IsCompanyUserOf.Select(m => m.Company).ToList())
            {
                foreach (Company co in c.AdminOfCompany)
                {
                    r.CanCreateProjects = true;
                    r.EditableCompanies.Add(co);
                    r.ProjectForCompanies.AddRange(c.AdminOfCompany);
                }
            }


            return r;
        }
    }


    public class Rights
    {
        public bool CanCreateCompany = false;
        public bool CanCreateUsers = false;
        public bool CanCreateProjects = false;
        public List<Company> EditableCompanies = new List<Company>();
        public List<Company> ProjectForCompanies = new List<Company>();
        public List<Company> UserForCompanies = new List<Company>();
    }
}
