using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiResiliencia.Models;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;

namespace MiResiliencia.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private MiResilienciaContext _context;

        public ProjectController(UserManager<ApplicationUser> userManager, MiResilienciaContext context)
        {
            _userManager = userManager;
            _context = context;
        }


        public ActionResult Details(int id)
        {
            Project p = _context.Projects.Include(m => m.Intesities).Include(m => m.ProtectionMeasure).Include(m => m.ProjectState).Include(m => m.PrAs).ThenInclude(m=>m.IKClasses).Include(m => m.PrAs).ThenInclude(m=>m.NatHazard).Where(m => m.Id == id).FirstOrDefault();

            if (p.ProjectState.ID < 3) ViewBag.ProjectWrite = true;
            else ViewBag.ProjectWrite = false;

            if (p.ProtectionMeasure == null)
            {
                ProtectionMeasure pm = new ProtectionMeasure();
                pm.Project = p;
                p.ProtectionMeasure = pm;
                _context.Entry(pm).State = EntityState.Added;
                _context.SaveChanges();
            }

            HttpContext.Session.SetInt32("Project", id);

            return View(p);
        }

        public JsonResult MarkForClose(int id)
        {
            Project p = _context.Projects.Find(id);
            ProjectState ps = _context.ProjectStates.Find(3);
            p.ProjectState = ps;
            _context.Entry(p).State = EntityState.Modified;
            _context.SaveChanges();
            return Json("ok");
        }

        public JsonResult Close(int id)
        {
            Project p = _context.Projects.Find(id);
            ProjectState ps = _context.ProjectStates.Find(4);
            p.ProjectState = ps;
            _context.Entry(p).State = EntityState.Modified;
            _context.SaveChanges();
            return Json("ok");
        }

        public JsonResult Delete(int id)
        {
            Project p = _context.Projects.Include(m => m.ProjectState).Where(m => m.Id == id).SingleOrDefault();
            ProjectState ps = _context.ProjectStates.Find(5);
            p.ProjectState = ps;
            _context.Entry(p).State = EntityState.Modified;
            _context.SaveChanges();
            return Json("ok");
        }

        public JsonResult ReOpen(int id)
        {
            Project p = _context.Projects.Find(id);
            ProjectState ps = _context.ProjectStates.Find(2);
            p.ProjectState = ps;
            _context.Entry(p).State = EntityState.Modified;
            _context.SaveChanges();
            return Json("ok");
        }

        public JsonResult CopyProject(int id)
        {
            // TODO
            /*Project p = _context.Projects.Include(m => m.MappedObjects.Select(x => x.Objectparameter)).Where(m => m.Id == id).FirstOrDefault();

            Npgsql.NpgsqlParameter parm = new Npgsql.NpgsqlParameter()
            {
                Direction = System.Data.ParameterDirection.Output,
                ParameterName = "Id"
            };

            _context.Database.ExecuteSqlCommand("INSERT INTO \"Project\" (\"Name\",\"Number\",\"CoordinateSystem\",geometry,\"Company_ID\",\"ProjectState_ID\",\"Description\") select \"Name\" || ' (réplica)', \"Number\", \"CoordinateSystem\", geometry, \"Company_ID\", 1, \"Description\" from \"Project\" where \"Project\".\"Id\" = " + id + " RETURNING \"Id\"", parm);
            int newid = (int)parm.Value;

            _context.Database.ExecuteSqlCommand("INSERT INTO \"Intensity\"(\"BeforeAction\", \"IKClasses_ID\", \"NatHazard_ID\", geometry, \"ProjectId\", \"IntensityDegree\") select \"BeforeAction\",\"IKClasses_ID\",\"NatHazard_ID\",geometry," + newid + ",\"IntensityDegree\" from \"Intensity\" where \"ProjectId\" = " + id);
            _context.Database.ExecuteSqlCommand("INSERT INTO \"PrA\" (\"NatHazardId\",\"IKClassesId\" ,\"Value\" ,\"ProjectId\") select \"NatHazardId\",\"IKClassesId\" ,\"Value\" ," + newid + " from \"PrA\" where \"ProjectId\" = " + id);
            _context.Database.ExecuteSqlCommand("INSERT INTO \"ProtectionMeasure\" (\"Costs\",\"LifeSpan\" ,\"OperatingCosts\",\"MaintenanceCosts\",\"RateOfReturn\",\"ProjectID\",geometry,\"Description\",\"ValueAddedTax\") select \"Costs\",\"LifeSpan\" ,\"OperatingCosts\",\"MaintenanceCosts\",\"RateOfReturn\"," + newid + ",geometry,\"Description\",\"ValueAddedTax\" from \"ProtectionMeasure\" where \"ProjectID\" = " + id);

            foreach (MappedObject m in p.MappedObjects)
            {
                Npgsql.NpgsqlParameter newmId = new Npgsql.NpgsqlParameter()
                {
                    Direction = System.Data.ParameterDirection.Output,
                    ParameterName = "Id"
                };


                _context.Database.ExecuteSqlCommand("insert into \"MappedObject\" (\"ObjectparameterID\",\"ProjectId\",geometry,\"FreeFillParameter_ID\") select \"ObjectparameterID\"," + newid + ",geometry,\"FreeFillParameter_ID\" from \"MappedObject\" where \"ID\" = " + m.ID + " RETURNING \"ID\"", newmId);
                int newmappedid = (int)newmId.Value;

                _context.Database.ExecuteSqlCommand("insert into \"ResilienceValues\" (\"Value\",\"MappedObject_ID\" ,\"ResilienceWeight_ID\",\"OverwrittenWeight\") select \"Value\"," + newmappedid + " ,\"ResilienceWeight_ID\",\"OverwrittenWeight\" from \"ResilienceValues\" where \"MappedObject_ID\" = " + m.ID);


            }
            */

            return Json("ok");

        }


        /// <summary>
        /// Save the PrA Values. Take all values in the same order as on the page and compare it with current values.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult PraEdit(int id, [Bind(Prefix = "pra.Value")] double[] values)
        {
            Project p = _context.Projects.Include(m => m.Intesities).Include(m => m.ProtectionMeasure).Include(m => m.PrAs.Select(x => x.IKClasses)).Include(m => m.PrAs.Select(x => x.NatHazard)).Where(m => m.Id == id).FirstOrDefault();
            var pralist = p.PrAs.OrderBy(m => m.IKClasses.Value).OrderBy(m => m.NatHazard).GroupBy(u => u.NatHazard)
                            .Select(grp => grp.ToList()).ToList();
            int i = 0;

            foreach (List<MiResiliencia.Models.PrA> intPerPra in pralist)
            {
                var intInHazard = intPerPra.GroupBy(u => u.IKClasses).Select(grp => grp.ToList()).ToList();
                foreach (List<MiResiliencia.Models.PrA> intPerHazardPerIK in intInHazard)
                {

                    foreach (MiResiliencia.Models.PrA pra in intPerHazardPerIK)
                    {
                        if (pra.Value != values[i])
                        {
                            pra.Value = values[i];
                            _context.Entry(pra).State = EntityState.Modified;
                        }
                        i++;
                    }
                }
            }
            _context.SaveChanges();
            return Json("ok");
        }

        public JsonResult EditInsideProject(int id, string desc)
        {
            Project p = _context.Projects.Find(id);
            p.Description = desc;
            _context.Entry(p).State = EntityState.Modified;
            _context.SaveChanges();
            return Json(desc);
        }

        public JsonResult GetProjectState()
        {
            try
            {
                int projectNumber = HttpContext.Session.GetInt32("Project").Value;
                Project p = _context.Projects.Include(m => m.ProjectState).Where(m => m.Id == projectNumber).FirstOrDefault();
                if (p == null) return Json(Resources.Global.ProjectNotFound);

                ProjectStateModel psm = new ProjectStateModel();
                psm.ProjectID = p.Id;
                psm.StateID = p.ProjectState.ID;


                return Json(psm);

            }
            catch (Exception e)
            {
                return Json(Resources.Global.SessionOut);
            }
        }



        [HttpPost]
        public async Task<ActionResult> SetSession(int myVariable)
        {
            // Set to Session here.
            HttpContext.Session.SetInt32("Project", myVariable);

            var id = _userManager.GetUserId(User);
            var applicationUser = await _userManager.GetUserAsync(User);
            await _context.Entry(applicationUser).Reference(m => m.MyWorkingProjekt).LoadAsync();
            Project p = await _context.Projects.FindAsync(myVariable);
            applicationUser.MyWorkingProjekt = p;
            _context.Entry(applicationUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Json(p.CoordinateSystem.ToString());
        }


        public async Task<JsonResult> SaveWindowPosition(int window, int x, int y, int width, int height)
        {
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            var id = _userManager.GetUserId(User);
            var applicationUser = await _userManager.GetUserAsync(User);
            await _context.Entry(applicationUser).Reference(m => m.UserSettings).LoadAsync();
            if (applicationUser.UserSettings == null) applicationUser.UserSettings = new UserSettings();
            // Project Layer Window
            if (window == 1)
            {
                applicationUser.UserSettings.LayerWindowX = x;
                applicationUser.UserSettings.LayerWindowY = y;
                applicationUser.UserSettings.LayerWindowWidth = width;
                applicationUser.UserSettings.LayerWindowHeight = height;
            }
            else if (window == 2)
            {
                applicationUser.UserSettings.DrawWindowX = x;
                applicationUser.UserSettings.DrawWindowY = y;
                applicationUser.UserSettings.DrawWindowWidth = width;
                applicationUser.UserSettings.DrawWindowHeight = height;
            }
            else if (window == 3)
            {
                applicationUser.UserSettings.ShpWindowX = x;
                applicationUser.UserSettings.ShpWindowY = y;
                applicationUser.UserSettings.ShpWindowWidth = width;
                applicationUser.UserSettings.ShpWindowHeight = height;
            }
            _context.Entry(applicationUser).State = EntityState.Modified;
            _context.SaveChanges();

            return Json("OK");
        }


        public async Task<JsonResult> GetWindowPosition(int window)
        {
            var id = _userManager.GetUserId(User);
            var applicationUser = await _userManager.GetUserAsync(User);
            await _context.Entry(applicationUser).Reference(m => m.UserSettings).LoadAsync();
            if (applicationUser.UserSettings == null) return Json(new WindowPosition() { X = 0, Y = 0, Width = 0, Height = 0 });
            WindowPosition w = new WindowPosition();
            if (window == 1)
            {
                w.X = applicationUser.UserSettings.LayerWindowX;
                w.Y = applicationUser.UserSettings.LayerWindowY;
                w.Width = applicationUser.UserSettings.LayerWindowWidth;
                w.Height = applicationUser.UserSettings.LayerWindowHeight;
            }
            else if (window == 2)
            {
                w.X = applicationUser.UserSettings.DrawWindowX;
                w.Y = applicationUser.UserSettings.DrawWindowY;
                w.Width = applicationUser.UserSettings.DrawWindowWidth;
                w.Height = applicationUser.UserSettings.DrawWindowHeight;
            }
            else if (window == 3)
            {
                w.X = applicationUser.UserSettings.ShpWindowX;
                w.Y = applicationUser.UserSettings.ShpWindowY;
                w.Width = applicationUser.UserSettings.ShpWindowWidth;
                w.Height = applicationUser.UserSettings.ShpWindowHeight;
            }
            return Json(w);
        }





        // Project-List
        public async Task<IActionResult> ProjectList()
        {
            var id = _userManager.GetUserId(User);
            var applicationUser = await _userManager.GetUserAsync(User);
            await _context.Entry(applicationUser).Reference(m => m.MyWorkingProjekt).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.MainCompany).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.UserSettings).LoadAsync();

            await _context.Entry(applicationUser).Collection(m => m.IsCompanyUserOf).Query().Include(m=>m.Company).ThenInclude(m=>m.Projects).LoadAsync();

            ProjectViewModel model = new ProjectViewModel(applicationUser);
            ViewBag.ProjectList = new SelectList(model._projects.OrderBy(m => m.Name), "Id", "Name");

            return PartialView(model);

        }

        /// <summary>
        /// Get All Projects and check if user can edit it
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ProjectTable(string idlist)
        {
            if (idlist == null)
            {
                ViewBag.HideDiv = true;
                return PartialView(getMyProjectList());
            }

            ViewBag.HideDiv = false;
            List<string> ids = idlist.Split(',').ToList();
            List<ProjectTableViewModel> myps = await getMyProjectList();
            List<ProjectTableViewModel> idps = myps.Where(m => ids.Contains(m.Project.Id.ToString())).ToList();

            return PartialView(idps);
        }


        public async Task<List<ProjectTableViewModel>> getMyProjectList()
        {
            List<Project> AllProjects = _context.Projects.Include(m => m.ProjectState).Where(m => m.ProjectState.Description != "Deleted").ToList();

            var id = _userManager.GetUserId(User);
            var applicationUser = await _userManager.GetUserAsync(User);
            await _context.Entry(applicationUser).Reference(m => m.MyWorkingProjekt).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.MainCompany).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.UserSettings).LoadAsync();

            await _context.Entry(applicationUser).Collection(m => m.IsCompanyUserOf).Query().Include(m => m.Company).ThenInclude(m => m.Projects).LoadAsync();

            List<Project> MyProjects = new List<Project>();
            MyProjects.AddRange(applicationUser.IsCompanyUserOf.Select(m => m.Company).SelectMany(x=>x.Projects));
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
    public class WindowPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ProjectStateModel
    {
        public int StateID { get; set; }
        public int ProjectID { get; set; }
    }

}
