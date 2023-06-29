using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiResiliencia.Models;
using NuGet.ProjectModel;

namespace MiResiliencia.Controllers
{
    public class ToolsController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private MiResilienciaContext db;

        public ToolsController(UserManager<ApplicationUser> userManager, MiResilienciaContext context)
        {
            _userManager = userManager;
            db = context;
        }
        // GET: miresiliencia
        public ActionResult Map()
        {
            return View();
        }

        public JsonResult CopyIK(int id)
        {
            //db.Database.ExecuteSqlCommand("insert into \"Intensity\" (\"BeforeAction\", \"IKClasses_ID\", \"NatHazard_ID\", geometry, \"ProjectId\", \"IntensityDegree\") select false, \"IKClasses_ID\", \"NatHazard_ID\", geometry, \"ProjectId\", \"IntensityDegree\" from \"Intensity\" where \"ID\" = " + id);

            return Json("OK");


        }

        public PartialViewResult IKsList(int id, bool beforeAction = true)
        {
            Project p = db.Projects.Include(m => m.Intesities).ThenInclude(m=>m.NatHazard).Include(m => m.Intesities).ThenInclude(m=>m.IKClasses).Where(m => m.Id == id)
                .AsNoTracking()
                .FirstOrDefault();
            ViewBag.BeforeAction = beforeAction;

            var natHazList = p.Intesities.Where(m => m.BeforeAction == ViewBag.BeforeAction).OrderByDescending(m => m.IntensityDegree).OrderBy(m => m.IKClasses.Value).OrderBy(m => m.NatHazard)
                                                .GroupBy(u => u.NatHazard)
                                                .Select(grp => grp.ToList()).ToList();
            return PartialView(p);
        }

        public PartialViewResult IKsListSmall(int id, bool beforeAction = true)
        {
            Project p = db.Projects.Include(m => m.Intesities).ThenInclude(m => m.NatHazard).Include(m => m.Intesities).ThenInclude(m => m.IKClasses).Where(m => m.Id == id)
                .AsNoTracking()
                .FirstOrDefault();
            ViewBag.BeforeAction = beforeAction;
            return PartialView(p);
        }

        public ActionResult MappedObjectsDetails(int id, double lat = 0, double lon = 0, bool inErrorView = false)
        {
            MappedObject o = db.MappedObjects.Include(m => m.Objectparameter).Where(m => m.ID == id).FirstOrDefault();
            if (o == null) return NotFound();
            o.lat = lat;
            o.lon = lon;
            ViewBag.inErrorView = inErrorView;
            return View(o);
        }

        public ActionResult MultipleMappedObjectsDetails(int[] ids, bool inErrorView = false)
        {
            ViewBag.inErrorView = inErrorView;

            List<MappedObject> os = db.MappedObjects.Include(m => m.Objectparameter).Where(m => ids.Contains(m.ID)).ToList();

            return View(os);
        }

        private TEntity ShallowCopyEntity<TEntity>(TEntity source) where TEntity : class, new()
        {

            // Get properties from EF that are read/write and not marked witht he NotMappedAttribute
            var sourceProperties = typeof(TEntity)
                                    .GetProperties()
                                    .Where(p => p.CanRead && p.CanWrite &&
                                                p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute), true).Length == 0);
            var newObj = new TEntity();

            foreach (var property in sourceProperties)
            {

                // Copy value
                property.SetValue(newObj, property.GetValue(source, null), null);

            }

            return newObj;

        }

        /// <summary>
        /// Get the ObjectParameter for a MappedObject
        /// </summary>
        /// <param name="id">The Id of the Mapped Object</param>
        /// <returns>The Partial View with the ObjectViewModel</returns>
        public ActionResult ObjectParameterEdit(int id, double lat = 0, double lon = 0, bool inErrorView = false)
        {
            ViewBag.inErrorView = inErrorView;
            MappedObject o = db.MappedObjects.Include(m => m.Objectparameter).ThenInclude(m=>m.MotherOtbjectparameter).ThenInclude(m=>m.HasProperties)
                .Include(pstate => pstate.Project).ThenInclude(pstate=>pstate.ProjectState)
                .Include(m => m.Objectparameter).ThenInclude(m=>m.HasProperties)
                .Include(m => m.Objectparameter).ThenInclude(m=>m.ObjectparameterPerProcesses).ThenInclude(m=>m.NatHazard)
                .Include(m=>m.Objectparameter).ThenInclude(m=>m.ObjectClass)
                .Include(m => m.FreeFillParameter).Where(m => m.ID == id).FirstOrDefault();
            if (o == null) return NotFound();
            o.lat = lat;
            o.lon = lon;
            if (o.Project.ProjectState.ID < 3) ViewBag.ProjectWrite = true;
            else ViewBag.ProjectWrite = false;
            // Create new View Model with MergedObjectParameter
            ObjectparameterViewModel ovm = new ObjectparameterViewModel();
            ovm.MappedObject = o;
            // default take everything from the Original, no free fill values
            //ovm.MergedObjectparameter = (Objectparameter)o.Objectparameter.Clone();
            ovm.MergedObjectparameter = ShallowCopyEntity<Objectparameter>(o.Objectparameter);


            // look for free fill properties
            if (o.Objectparameter.MotherOtbjectparameter != null) ovm.HasProperties = o.Objectparameter.MotherOtbjectparameter.HasProperties;
            else ovm.HasProperties = o.Objectparameter.HasProperties;


            // copy all free fill properties to merged parameter, wenn free fill has a value
            foreach (ObjectparameterHasProperties ohp in ovm.HasProperties.Where(m => m.isOptional == true))
            {
                if (o.FreeFillParameter != null)
                {
                    // Get the Property from the Free Fill Object
                    var FreeFillProperty = ohp.Property.Split('.').Select(s => o.FreeFillParameter.GetType().GetProperty(s)).FirstOrDefault();
                    // Get the Property from the Free Fill Object
                    var MergedProperty = ohp.Property.Split('.').Select(s => ovm.MergedObjectparameter.GetType().GetProperty(s)).FirstOrDefault();

                    if (FreeFillProperty.GetValue(o.FreeFillParameter) != null) MergedProperty.SetValue(ovm.MergedObjectparameter, FreeFillProperty.GetValue(o.FreeFillParameter));
                }
            }

            return PartialView(ovm);
        }

        /// <summary>
        /// Get the ObjectParameter for a MappedObject
        /// </summary>
        /// <param name="id">The Id of the Mapped Object</param>
        /// <returns>The Partial View with the ObjectViewModel</returns>
        public ActionResult MultipleObjectsParameterEdit(int[] ids, bool inErrorView = false)
        {
            ViewBag.inErrorView = inErrorView;
            List<MappedObject> os = db.MappedObjects.Include(m => m.Objectparameter).ThenInclude(m => m.MotherOtbjectparameter).ThenInclude(m => m.HasProperties)
                .Include(pstate => pstate.Project).ThenInclude(pstate => pstate.ProjectState)
                .Include(m => m.Objectparameter).ThenInclude(m => m.HasProperties)
                .Include(m => m.Objectparameter).ThenInclude(m => m.ObjectparameterPerProcesses).ThenInclude(m => m.NatHazard)
                .Include(m => m.Objectparameter).ThenInclude(m => m.ObjectClass)
                .Include(m => m.FreeFillParameter).Where(m => ids.Contains(m.ID)).ToList();
            if (os == null) return NotFound();
            
            MultipleObjectparameterViewModel finalovm = new MultipleObjectparameterViewModel();
            finalovm.MappedObjects = new List<MappedObject>();

            List<ObjectparameterViewModel> opvms = new List<ObjectparameterViewModel>();

            foreach (MappedObject o in os)
            {

                if (o.Project.ProjectState.ID < 3) ViewBag.ProjectWrite = true;
                else ViewBag.ProjectWrite = false;
                // Create new View Model with MergedObjectParameter

                ObjectparameterViewModel ovm = new ObjectparameterViewModel();
                // default take everything from the Original, no free fill values
                //ovm.MergedObjectparameter = (Objectparameter)o.Objectparameter.Clone();
                ovm.MergedObjectparameter = ShallowCopyEntity<Objectparameter>(o.Objectparameter);
                ovm.MappedObject = o;

                // look for free fill properties
                if (o.Objectparameter.MotherOtbjectparameter != null) ovm.HasProperties = o.Objectparameter.MotherOtbjectparameter.HasProperties;
                else ovm.HasProperties = o.Objectparameter.HasProperties;


                // copy all free fill properties to merged parameter, wenn free fill has a value
                foreach (ObjectparameterHasProperties ohp in ovm.HasProperties.Where(m => m.isOptional == true))
                {
                    if (o.FreeFillParameter != null)
                    {
                        // Get the Property from the Free Fill Object
                        var FreeFillProperty = ohp.Property.Split('.').Select(s => o.FreeFillParameter.GetType().GetProperty(s)).FirstOrDefault();
                        // Get the Property from the Free Fill Object
                        var MergedProperty = ohp.Property.Split('.').Select(s => ovm.MergedObjectparameter.GetType().GetProperty(s)).FirstOrDefault();

                        if (FreeFillProperty.GetValue(o.FreeFillParameter) != null) MergedProperty.SetValue(ovm.MergedObjectparameter, FreeFillProperty.GetValue(o.FreeFillParameter));
                    }
                }
                opvms.Add(ovm);
                finalovm.MappedObjects.Add(o);

            }

            List<ObjectparameterHasProperties> commonProperties = opvms.SelectMany(m => m.HasProperties).Distinct().ToList();
            List<string> commonPropertiesString = opvms.SelectMany(m => m.HasProperties.Select(m => m.Property)).Distinct().ToList();

            foreach (ObjectparameterViewModel ohp in opvms)
            {
                foreach (string p in commonPropertiesString)
                {
                    if (!ohp.HasProperties.Select(m=>m.Property).Contains(p)) commonProperties.RemoveAll(m=>m.Property == p);
                }

            }

            finalovm.MergedObjectparameter = new Objectparameter();

            foreach (string prop in commonPropertiesString)
            {
                object? val = null;

                if (opvms.Count > 0)
                {
                    var MergedProperty = prop.Split('.').Select(s => opvms.First().MergedObjectparameter.GetType().GetProperty(s)).FirstOrDefault();
                    val = MergedProperty.GetValue(opvms.First().MergedObjectparameter);

                    foreach (var opvm in opvms)
                    {
                        object? nextSelectObjectValue = MergedProperty.GetValue(opvm.MergedObjectparameter);
                        // if values are not the same, make the value null
                        if ((val!=null) && (!val.Equals(nextSelectObjectValue))) val = null;
                    }
                }

                // set the new val
                var MergedPropertyToChange = prop.Split('.').Select(s => finalovm.MergedObjectparameter.GetType().GetProperty(s)).FirstOrDefault();
                MergedPropertyToChange.SetValue(finalovm.MergedObjectparameter, val);

            }

            finalovm.HasProperties = commonProperties;

            ViewBag.ProjectWrite = true;

            return PartialView(finalovm);
        }

        private Objectparameter CloneObject(Objectparameter o)
        {
            Objectparameter cloneme = new Objectparameter();
            if (o.Name != null) cloneme.Name = (string)o.Name.Clone();
            if (o.Description != null) cloneme.Description = (string)o.Description.Clone();
            cloneme.Value = o.Value;
            if (o.ChangeValue != null) cloneme.ChangeValue = (string)o.ChangeValue.Clone();
            if (o.Unity != null) cloneme.Unity = (string)o.Unity.Clone();
            cloneme.Floors = o.Floors;
            cloneme.Personcount = o.Personcount;
            if (o.ChangePersonCount != null) cloneme.ChangePersonCount = (string)o.ChangePersonCount.Clone();
            cloneme.Presence = o.Presence;
            cloneme.NumberOfVehicles = o.NumberOfVehicles;
            cloneme.Velocity = o.Velocity;
            cloneme.MotherOtbjectparameter = o;
            cloneme.IsStandard = false;
            cloneme.ObjectClass = o.ObjectClass;
            cloneme.Staff = o.Staff;
            return cloneme;

        }

        private void AddFreeValue(object newValue, MappedObject o, string parameter)
        {
            var property = parameter.Split('.').Select(s => o.Objectparameter.GetType().GetProperty(s)).FirstOrDefault();
            if (property == null) return;
            if ((newValue != null) && (o.FreeFillParameter == null))
            {
                o.FreeFillParameter = new Objectparameter();
                db.Entry(o.FreeFillParameter).State = EntityState.Added;
                db.SaveChanges();
            }

            if ((newValue != null) && (newValue != property.GetValue(o.FreeFillParameter, null)))
            {
                property.SetValue(o.FreeFillParameter, newValue);
                db.Entry(o.FreeFillParameter).State = EntityState.Modified;
                db.SaveChanges();
            }

        }

        private bool ChangeDefaultValue(object newValue, MappedObject o, string parameter, bool isAlreadyCloned)
        {
            var property = parameter.Split('.').Select(s => o.Objectparameter.GetType().GetProperty(s)).FirstOrDefault();
            if (property == null) throw new Exception("Property " + parameter + " not found");
            try
            {
                if (newValue != property.GetValue(o.Objectparameter))
                {
                    // If it was a standard Object and we changed something on the standard
                    if ((o.Objectparameter.IsStandard) && (!isAlreadyCloned))
                    {
                        o.Objectparameter = CloneObject(o.Objectparameter);
                        db.Entry(o.Objectparameter).State = EntityState.Added;
                        db.SaveChanges();
                        isAlreadyCloned = true;

                    }
                    property.SetValue(o.Objectparameter, newValue);
                    db.Entry(o.Objectparameter).State = EntityState.Modified;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not set value", e);
            }
            return isAlreadyCloned;

        }

        private void SaveChanges(int id, string name, string description, int? value, string ChangeValue, string unity, int? floors, int? Personcount, int? Staff, string ChangePersonCount, double? Presence, int? NumberOfVehicles, int? Velocity)
        {
            //MappedObject o = db.MappedObjects.Include(m => m.Objectparameter.MotherOtbjectparameter.HasProperties).Include(m => m.Objectparameter.HasProperties).Include(m => m.FreeFillParameter).Include(m => m.Objectparameter.ObjectClass).Where(m => m.ID == id).FirstOrDefault();
            MappedObject o = db.MappedObjects.Include(m => m.Objectparameter).ThenInclude(m => m.MotherOtbjectparameter).ThenInclude(m => m.HasProperties)
                .Include(pstate => pstate.Project).ThenInclude(pstate => pstate.ProjectState)
                .Include(m => m.Objectparameter).ThenInclude(m => m.HasProperties)
                .Include(m => m.Objectparameter).ThenInclude(m => m.ObjectparameterPerProcesses).ThenInclude(m => m.NatHazard)
                .Include(m => m.Objectparameter).ThenInclude(m => m.ObjectClass)
                .Include(m => m.FreeFillParameter).Where(m => m.ID == id).FirstOrDefault();

            bool isAlreadyCloned = false;

            List<ObjectparameterHasProperties> hasProps = new List<ObjectparameterHasProperties>();
            if (o.Objectparameter.MotherOtbjectparameter != null) hasProps = o.Objectparameter.MotherOtbjectparameter.HasProperties;
            else hasProps = o.Objectparameter.HasProperties;

            // if description is changed, add it to freefillparameters
            string parameter = "Description";
            ObjectparameterHasProperties ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(description, o, parameter);
                else ChangeDefaultValue(description, o, parameter, isAlreadyCloned);
            }

            parameter = "Name";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(name, o, parameter);
                else ChangeDefaultValue(name, o, parameter, isAlreadyCloned);
            }

            parameter = "Value";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(value, o, parameter);
                else ChangeDefaultValue(value, o, parameter, isAlreadyCloned);
            }

            parameter = "ChangeValue";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(ChangeValue, o, parameter);
                else ChangeDefaultValue(ChangeValue, o, parameter, isAlreadyCloned);
            }

            parameter = "Floors";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(floors, o, parameter);
                else ChangeDefaultValue(floors, o, parameter, isAlreadyCloned);
            }

            parameter = "Personcount";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(Personcount, o, parameter);
                else ChangeDefaultValue(Personcount, o, parameter, isAlreadyCloned);
            }

            parameter = "Staff";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(Staff, o, parameter);
                else ChangeDefaultValue(Staff, o, parameter, isAlreadyCloned);
            }

            parameter = "ChangePersonCount";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(ChangePersonCount, o, parameter);
                else ChangeDefaultValue(ChangePersonCount, o, parameter, isAlreadyCloned);
            }

            parameter = "Presence";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(Presence, o, parameter);
                else ChangeDefaultValue(Presence, o, parameter, isAlreadyCloned);
            }

            parameter = "NumberOfVehicles";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(NumberOfVehicles, o, parameter);
                else ChangeDefaultValue(NumberOfVehicles, o, parameter, isAlreadyCloned);
            }

            parameter = "Velocity";
            ophp = hasProps.Where(z => z.Property == parameter).FirstOrDefault();
            if (ophp != null)
            {
                if (ophp.isOptional) AddFreeValue(Velocity, o, parameter);
                else ChangeDefaultValue(Velocity, o, parameter, isAlreadyCloned);
            }

            db.SaveChanges();
        }


        public JsonResult EditInside(string id, string name, string description, int? value, string ChangeValue, string unity, int? floors, int? Personcount, int? Staff, string ChangePersonCount, double? Presence, int? NumberOfVehicles, int? Velocity)
        {
            int id_toUpdate = 0;
            if (Int32.TryParse(id, out id_toUpdate))
            {
                SaveChanges(id_toUpdate, name, description, value, ChangeValue, unity, floors, Personcount, Staff, ChangePersonCount, Presence, NumberOfVehicles, Velocity);
            }
            else
            {
                string[] ids = id.Split("_");
                foreach (string ids_one in ids)
                {
                    if (Int32.TryParse(ids_one, out id_toUpdate))
                    {
                        SaveChanges(id_toUpdate, name, description, value, ChangeValue, unity, floors, Personcount, Staff, ChangePersonCount, Presence, NumberOfVehicles, Velocity);
                    }
                }
            }

            return Json("OK");

        }




        public JsonResult GetObjects(int id)
        {
            List<Objectparameter> objekte = db.Objektparameter.Where(m => m.ObjectClass.ID == id).Where(m => m.IsStandard == true).OrderBy(m => m.Name).ToList();
            List<ObjectViewModel> ovm = new List<ObjectViewModel>();
            foreach (Objectparameter o in objekte)
            {
                ovm.Add(new ObjectViewModel() { ID = o.ID, Name = o.Name });
            }
            return Json(ovm);
        }

        public JsonResult GetObjectType(int id)
        {
            Objectparameter o = db.Objektparameter.Find(id);
            if (o == null) return Json("ID not found");
            return Json(o.FeatureType);
        }

        public ActionResult Details(string postgisid, string landuse)
        {


            postgisid = postgisid.Replace("postgislandus.", "");
            PostGISHatObjektparameter p = db.PostGISHatObjektparameter.Include("Objektparameter").Where(m => m.PostGISID.ToString() == postgisid).FirstOrDefault();

            if (p == null)
            {
                Objectparameter o = db.Objektparameter.Where(u => u.ID.ToString() == landuse).FirstOrDefault();
                if (o == null) return NotFound();
                return View(o);
            }

            return View(p.Objektparameter);
        }


        public IActionResult PMDetails(int id, bool projectWrite)
        {
            ViewBag.ProjectWrite = projectWrite;
            ProtectionMeasure p = db.ProtectionMeasurements.Find(id);
            db.Entry(p).Reload();
            if (p == null) return NotFound();
            return PartialView(p);
        }

        public IActionResult EditInsidePM(int id, int costs, int lifespan, int operatingcosts, int maintenancecosts, double rateofreturn, double valueaddedtax, string description)
        {
            ProtectionMeasure p = db.ProtectionMeasurements.Find(id);
            if (p == null) return NotFound();
            p.Costs = costs;
            p.LifeSpan = lifespan;
            p.OperatingCosts = operatingcosts;
            p.MaintenanceCosts = maintenancecosts;
            p.RateOfReturn = rateofreturn;
            p.ValueAddedTax = valueaddedtax;
            p.Description = description;

            db.Entry(p).State = EntityState.Modified;

            db.SaveChanges();


            return Json("OK");
        }



        // dram sub iframes
        public ActionResult DrawPerimeter(int id, bool projectWrite)
        {
            ViewBag.ProjectWrite = projectWrite;
            return PartialView(id);
        }

        public ActionResult DrawIKS(int id, bool projectWrite)
        {
            ViewBag.ProjectWrite = projectWrite;
            List<SelectListItem> hazardsSelect = new List<SelectListItem>();
            foreach (NatHazard n in db.NatHazards.ToList())
            {
                hazardsSelect.Add(new SelectListItem { Text = n.Name, Value = n.ID.ToString() });
            }
            ViewBag.Hazards = hazardsSelect;

            List<SelectListItem> ikclassesSelect = new List<SelectListItem>();
            foreach (IKClasses i in db.IntensitaetsKlassen.OrderBy(m => m.ID).ToList())
            {
                ikclassesSelect.Add(new SelectListItem { Text = i.Description, Value = i.ID.ToString() });
            }
            ViewBag.IKClasses = ikclassesSelect;

            ViewBag.IntensityDegree = new List<SelectListItem> {
                new SelectListItem { Text = Resources.Global.strong, Value = "0"},
                new SelectListItem { Text = Resources.Global.mittel, Value = "1" },
                new SelectListItem { Text = Resources.Global.weak, Value = "2" }
            };

            return PartialView(id);
        }

        public ActionResult DrawIKSAfter(int id, bool projectWrite)
        {
            ViewBag.ProjectWrite = projectWrite;
            List<SelectListItem> hazardsSelect = new List<SelectListItem>();
            foreach (NatHazard n in db.NatHazards.ToList())
            {
                hazardsSelect.Add(new SelectListItem { Text = n.Name, Value = n.ID.ToString() });
            }
            ViewBag.Hazards = hazardsSelect;

            List<SelectListItem> ikclassesSelect = new List<SelectListItem>();
            foreach (IKClasses i in db.IntensitaetsKlassen.OrderBy(m => m.ID).ToList())
            {
                ikclassesSelect.Add(new SelectListItem { Text = i.Description, Value = i.ID.ToString() });
            }
            ViewBag.IKClasses = ikclassesSelect;

            ViewBag.IntensityDegree = new List<SelectListItem> {
                new SelectListItem { Text = Resources.Global.strong, Value = "0"},
                new SelectListItem { Text = Resources.Global.mittel, Value = "1" },
                new SelectListItem { Text = Resources.Global.weak, Value = "2" }
            };

            return PartialView(id);
        }

        public ActionResult DrawPotential(int id, bool projectWrite)
        {

            ViewBag.ProjectWrite = projectWrite;
            List<SelectListItem> objclass = new List<SelectListItem>();
            objclass.Add(new SelectListItem { Text = "---", Value = "-1" });
            foreach (ObjectClass i in db.ObjektKlassen.ToList())
            {
                objclass.Add(new SelectListItem { Text = i.Name, Value = i.ID.ToString() });
            }
            ViewBag.ObjClasses = objclass;

            return PartialView(id);
        }

        public ActionResult DrawProtection(int id, bool projectWrite)
        {
            ViewBag.ProjectWrite = projectWrite;
            Project p = db.Projects.Include(m => m.ProtectionMeasure).Where(m => m.Id == id).FirstOrDefault();
            db.Entry(p.ProtectionMeasure).Reload();

            return PartialView(p);
        }

        public ActionResult DrawResilience(int id, bool projectWrite)
        {
            ViewBag.ProjectWrite = projectWrite;
            Project p = db.Projects.Include(m => m.ProtectionMeasure).Where(m => m.Id == id).FirstOrDefault();

            return PartialView(p);
        }

        public ActionResult EditResilience(int id, bool beforeAction = true)
        {
            MappedObject m = db.MappedObjects
                .Include(m => m.Project.Intesities).ThenInclude(m=>m.NatHazard)
                .Include(m=>m.ResilienceValues).ThenInclude(m=>m.ResilienceWeight).ThenInclude(m=>m.ResilienceFactor)
                .Include(p => p.Objectparameter).ThenInclude(m=>m.MotherOtbjectparameter).Where(z => z.ID == id).FirstOrDefault();

            List<ResilienceFactor> myPossibleFactors;
            if (m.Objectparameter.MotherOtbjectparameter != null) myPossibleFactors = db.ResilienceFactors
                    .Include(m => m.ObjectparameterHasResilienceFactor)
                    .Include(m => m.ResilienceWeights).ThenInclude(m => m.NatHazard)
                    .Where(q => q.ObjectparameterHasResilienceFactor.Any(zz => zz.Objectparameter_ID == m.Objectparameter.MotherOtbjectparameter.ID)).ToList();
            else myPossibleFactors = db.ResilienceFactors
                    .Include(m => m.ObjectparameterHasResilienceFactor).ThenInclude(m => m.Objectparameter)
                    .Include(m => m.ResilienceWeights).ThenInclude(m => m.NatHazard)
                    .Where(q => q.ObjectparameterHasResilienceFactor.Any(zz => zz.Objectparameter == m.Objectparameter)).ToList();
                   

            ViewBag.PossibleFactors = myPossibleFactors;


            List<NatHazard> projectNatHazards = m.Project.Intesities.Select(nat => nat.NatHazard).Distinct().ToList();

            // We have only resilience for Sequia and all others
            List<NatHazard> onlyTwoChoices = new List<NatHazard>();
            bool aluvionAdded = false;
            foreach (NatHazard n in projectNatHazards)
            {
                if ((n.Name == "Sequía") || (n.Name == "Drought")) onlyTwoChoices.Add(n);
                else
                {
                    if (!aluvionAdded)
                    {
                        NatHazard n2 = db.NatHazards.Find(2);

                        onlyTwoChoices.Add(n2);
                        aluvionAdded = true;
                    }
                }

            }

            ViewBag.NatHazards = onlyTwoChoices;
            ViewBag.ResilienceBefore = beforeAction;
            return PartialView(m);
        }

        public JsonResult CopyResilience(int from, int to, bool before = true)
        {
            if (from == to) return Json("OK (copy from and to the same)");

            List<ResilienceValues> fromValues = db.ResilienceValues.Include(m => m.ResilienceWeight.ResilienceFactor).Include(m => m.ResilienceWeight.NatHazard).Where(m => m.MappedObject.ID == from && m.ResilienceWeight.BeforeAction == before).ToList();
            //MappedObject mo = db.MappedObjects.Include(m => m.Objectparameter.ResilienceFactors).Include(m => m.Objectparameter.MotherOtbjectparameter.ResilienceFactors).Include(m => m.ResilienceValues).Where(m => m.ID == to).SingleOrDefault();
            MappedObject mo = db.MappedObjects
                .Include(m => m.Project.Intesities).ThenInclude(m => m.NatHazard)
                .Include(m => m.ResilienceValues).ThenInclude(m => m.ResilienceWeight).ThenInclude(m => m.ResilienceFactor)
                .Include(p => p.Objectparameter).ThenInclude(m => m.MotherOtbjectparameter).Where(z => z.ID == to).SingleOrDefault();

            if (mo != null)
            {
                // delete the old ones
                mo.ResilienceValues.RemoveAll(m => m.ResilienceWeight.BeforeAction == false);
                if (before) mo.ResilienceValues.RemoveAll(m => m.ResilienceWeight.BeforeAction == true);


                foreach (ResilienceValues r in fromValues)
                {
                    // TODO
                    //if (mo.Objectparameter.ResilienceFactors.Contains(r.ResilienceWeight.ResilienceFactor) || ((mo.Objectparameter.MotherOtbjectparameter.ResilienceFactors != null) && (mo.Objectparameter.MotherOtbjectparameter.ResilienceFactors.Contains(r.ResilienceWeight.ResilienceFactor))))
                    {
                        /*List<ResilienceValues> oldVs = mo.ResilienceValues.Where(m => m.ResilienceWeight.ID == r.ResilienceWeight.ID && m.ResilienceWeight.BeforeAction == before).ToList();

                        foreach (ResilienceValues oldV in oldVs)
                        {
                            mo.ResilienceValues.Remove(oldV);
                            db.Entry(oldV).State = EntityState.Deleted;
                        }*/

                        ResilienceValues v = new ResilienceValues();
                        v.MappedObject = mo;
                        v.OverwrittenWeight = r.OverwrittenWeight;
                        v.ResilienceWeight = r.ResilienceWeight;
                        v.Value = r.Value;
                        db.Entry(v).State = EntityState.Added;


                        // do it also for after mitigation
                        if (before == true)
                        {
                            ResilienceWeight rAfter = db.ResilienceWeights.Where(m => m.BeforeAction == false && m.NatHazard.ID == r.ResilienceWeight.NatHazard.ID && m.ResilienceFactor.ID == r.ResilienceWeight.ResilienceFactor.ID).FirstOrDefault();

                            /*List<ResilienceValues> oldVsAfter = mo.ResilienceValues.Where(m => m.ResilienceWeight.ID == rAfter.ID).ToList();

                            foreach (ResilienceValues oldV in oldVsAfter)
                            {
                                mo.ResilienceValues.Remove(oldV);
                                db.Entry(oldV).State = EntityState.Deleted;
                            }*/

                            ResilienceValues vAfter = new ResilienceValues();



                            vAfter.MappedObject = mo;
                            vAfter.OverwrittenWeight = r.OverwrittenWeight;
                            vAfter.ResilienceWeight = rAfter;
                            vAfter.Value = r.Value;
                            db.Entry(vAfter).State = EntityState.Added;
                        }

                    }
                }

                db.SaveChanges();
                return Json("OK");
            }

            return Json("ERROR: Wrong id");
        }

        public JsonResult ChangeResilience(int ResilienceValueID, string ResilienceFactorId, int NatHazardId, double value, int MappedObjectId, double Weight, bool BeforeAction)
        {
            if (ResilienceValueID > 0)
            {
                ResilienceValues r = db.ResilienceValues.Find(ResilienceValueID);
                r.Value = value;
                r.OverwrittenWeight = Weight;
                db.Entry(r).State = EntityState.Modified;


                db.SaveChanges();
            }
            // new resiliencevalue value
            else
            {
                ResilienceWeight w = db.ResilienceWeights.Where(m => m.ResilienceFactor.ID == ResilienceFactorId && m.NatHazard.ID == NatHazardId && m.BeforeAction == BeforeAction).FirstOrDefault();
                MappedObject mo = db.MappedObjects.Find(MappedObjectId);

                ResilienceValues r = db.ResilienceValues.Where(m => m.MappedObject.ID == mo.ID && m.ResilienceWeight.ID == w.ID).FirstOrDefault();
                if (r == null)
                {
                    r = new ResilienceValues();
                    r.MappedObject = mo;
                    r.ResilienceWeight = w;
                    r.Value = value;
                    r.OverwrittenWeight = Weight;

                    db.Entry(r).State = EntityState.Added;

                    // do the same for after mitigation
                    if (BeforeAction == true)
                    {
                        ResilienceWeight wAfter = db.ResilienceWeights.Where(m => m.ResilienceFactor.ID == ResilienceFactorId && m.NatHazard.ID == NatHazardId && m.BeforeAction == false).FirstOrDefault();
                        ResilienceValues rAfter = new ResilienceValues();
                        rAfter.MappedObject = mo;
                        rAfter.ResilienceWeight = wAfter;
                        rAfter.Value = value;
                        rAfter.OverwrittenWeight = Weight;
                        db.Entry(rAfter).State = EntityState.Added;
                    }


                    db.SaveChanges();
                }
                else
                {
                    r.Value = value;
                    r.OverwrittenWeight = Weight;
                    db.Entry(r).State = EntityState.Modified;
                    db.SaveChanges();
                }

            }

            return Json("OK");
        }


        /// <summary>
        /// Get the id of the intensity Table or false, if it doesn't exists
        /// </summary>
        /// <param name="ProjectId"></param>
        /// <param name="NatHazardId"></param>
        /// <param name="IKClassID"></param>
        /// <param name="isBefore"></param>
        /// <returns>id of Intensity Class or false</returns>
        [HttpGet]
        public JsonResult GetIntensityID(int ProjectId, int NatHazardId, int IKClassID, bool isBefore, IntensityDegree IntensityDegree)
        {
            Intensity i = db.Intensities.Where(m => m.Project.Id == ProjectId && m.NatHazard.ID == NatHazardId && m.IKClasses.ID == IKClassID && m.BeforeAction == isBefore && m.IntensityDegree == IntensityDegree).FirstOrDefault();
            if (i == null) return Json("false");
            return Json(i.ID);
        }


    }

    public class ObjectViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
