using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiResiliencia.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MiResiliencia.Controllers
{
    public class HomeController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private MiResilienciaContext _context;

        public HomeController(UserManager<ApplicationUser> userManager, MiResilienciaContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var id = _userManager.GetUserId(User);
            var applicationUser = await _userManager.GetUserAsync(User);
            await _context.Entry(applicationUser).Reference(m => m.MyWorkingProjekt).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.MainCompany).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.UserSettings).LoadAsync();

            if (applicationUser.UserSettings != null)
            {
                ViewBag.ProjectLayerWindowX = applicationUser.UserSettings.LayerWindowX;
                ViewBag.ProjectLayerWindowY = applicationUser.UserSettings.LayerWindowY;
                ViewBag.ProjectLayerWindowWidth = applicationUser.UserSettings.LayerWindowWidth;
                ViewBag.ProjectLayerWindowHeight = applicationUser.UserSettings.LayerWindowHeight;

                ViewBag.ProjectDrawWindowX = applicationUser.UserSettings.DrawWindowX;
                ViewBag.ProjectDrawWindowY = applicationUser.UserSettings.DrawWindowY;
                ViewBag.ProjectDrawWindowWidth = applicationUser.UserSettings.DrawWindowWidth;
                ViewBag.ProjectDrawWindowHeight = applicationUser.UserSettings.DrawWindowHeight;
            }

            Project p = applicationUser.MyWorkingProjekt;


            if ((applicationUser.UserSettings != null) && (applicationUser.UserSettings.Language != null))
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(applicationUser.UserSettings.Language);
                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
            }

            if (applicationUser.MainCompany == null)
            {
                List<Company> c = _context.Companies.Where(m => m.AdminUsers.Any(x => x.AdminRefId == applicationUser.Id)).ToList();
                if ((c == null) || (c.Count == 0)) c = _context.Companies.Where(m => m.CompanyUsers.Any(x => x.CompanyUserRefId == applicationUser.Id)).ToList();

                ViewBag.Company = c[0];
                ViewBag.CompanyColor = c[0].CompanyColor;
                ViewBag.CompanyRGBAColor = c[0].CompanyRGBAColor;
                if (p != null)
                {
                    HttpContext.Session.SetInt32("Project", applicationUser.MyWorkingProjekt.Id);
                }


                else if ((c[0].Projects != null) && (c[0].Projects.Count > 0))
                {
                    HttpContext.Session.SetInt32("Project", c[0].Projects[0].Id);
                }
            }
            else
            {
                ViewBag.Company = applicationUser.MainCompany;
                ViewBag.CompanyColor = applicationUser.MainCompany.CompanyColor;
                ViewBag.CompanyRGBAColor = applicationUser.MainCompany.CompanyRGBAColor;
                if (p != null)
                {
                    HttpContext.Session.SetInt32("Project", applicationUser.MyWorkingProjekt.Id);
                }
                else if ((applicationUser.MainCompany.Projects != null) && (applicationUser.MainCompany.Projects.Count > 0))
                {
                    HttpContext.Session.SetInt32("Project", applicationUser.MainCompany.Projects[0].Id);
                }
            }

            /*ViewBag.Geoserver = WebConfigurationManager.AppSettings["GeoServerURL"];

            System.Configuration.Configuration rootWebConfig =
                System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/");
            System.Configuration.ConnectionStringSettings connString;
            if (rootWebConfig.ConnectionStrings.ConnectionStrings.Count > 0)
            {
                connString =
                    rootWebConfig.ConnectionStrings.ConnectionStrings["DefaultConnection"];
                if (connString != null)
                {
                    // server=geobrowser.ch;Port=5432;Database=miresiliencia;User Id=postgres;Password=sleepless
                    string server = connString.ConnectionString.Substring(connString.ConnectionString.IndexOf("server=") + 7);
                    server = server.Substring(0, server.IndexOf(";"));

                    string database = connString.ConnectionString.Substring(connString.ConnectionString.IndexOf("Database=") + 9);
                    database = database.Substring(0, database.IndexOf(";"));

                    ViewBag.Database = server + " (Database: " + database + ")";
                }
            }
            */

            /*var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = WebConfigurationManager.AppSettings["SHPImportRunPath"],
                    Arguments = "Version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            try
            {
                proc.Start();
                string outputContent = "";
                while (!proc.StandardOutput.EndOfStream)
                {
                    outputContent += proc.StandardOutput.ReadLine();
                }

                ViewBag.ShpImportVersion = outputContent;
            }
            catch (Exception exxx)
            {
                ViewBag.ShpImportVersion = "Not found";
            }*/

            return View();
        }
    }
}
