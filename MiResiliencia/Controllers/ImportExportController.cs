using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Data.SqlClient.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MiResiliencia.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NuGet.ProjectModel;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace MiResiliencia.Controllers
{
    public class ImportExportController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private MiResilienciaContext _context;
        private IConfiguration _configuration { get; }

        public ImportExportController(UserManager<ApplicationUser> userManager, MiResilienciaContext context, IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            string exportf = Path.GetRandomFileName();
            string[] fname = exportf.Split(".");


            string? logFile = HttpContext.Session.GetString("logFile");
            if (logFile == null)
            {
                string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
                if (!Directory.Exists(dataDir + "//Logs")) Directory.CreateDirectory(dataDir + "//Logs");
                logFile = dataDir + "//Logs//" + fname[0] + ".txt";
                HttpContext.Session.SetString("logFile", logFile);
            }
            return View();
        }

        public IActionResult Import()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            string logFile = HttpContext.Session.GetString("logFile");
            System.IO.File.WriteAllText(logFile, "");
            System.IO.File.AppendAllText(logFile, $"Iniciar la importación  {file.FileName}" + System.Environment.NewLine);

            int? projectId = HttpContext.Session.GetInt32("Project");
            if (projectId == null) { return NotFound(); }
            

            System.IO.File.AppendAllText(logFile, $"Importar a ID {projectId} de proyecto" + System.Environment.NewLine);

            Project p = _context.Projects.Include(m => m.Intesities).ThenInclude(m=>m.IKClasses)
                .Include(m => m.Intesities).ThenInclude(m => m.NatHazard)
                .Include(m => m.ProtectionMeasure).Include(m => m.ProjectState).Include(m => m.PrAs).ThenInclude(m => m.IKClasses).Include(m => m.PrAs).ThenInclude(m => m.NatHazard).Where(m => m.Id == projectId).FirstOrDefault();

            string prefix = "import_" + RandomString(4);
            string changes = "";
            string filePath = dataDir + "\\Import\\" + file.FileName;
            if (file.Length > 0)
            {
                // full path to file in temp location

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            ProcessStartInfo psi = new ProcessStartInfo();

            string db = _configuration["Environment:DB"];
            string host = _configuration["Environment:DBHost"];
            string dbuser = _configuration["Environment:DBUSer"];
            string dbpassword = _configuration["Environment:DBPassword"];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = @"C:\Program Files\GDAL\ogr2ogr.exe";
                psi.WorkingDirectory = @"C:\Program Files\GDAL";
                psi.EnvironmentVariables["GDAL_DATA"] = @"C:\Program Files\GDAL\gdal-data";
                psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = @"C:\Program Files\GDAL\gdal-plugins";
                psi.EnvironmentVariables["PATH"] = "C:\\Program Files\\GDAL;" + psi.EnvironmentVariables["PATH"];
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                psi.FileName = "ogr2ogr";
            }

            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;

            string pgstring = " PG:\"dbname = '" + db + "' user = '" + dbuser + "' password = '" + dbpassword + "' host = '" + host + "'\"";


            psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_perimeter\" \"" + filePath + "\" -sql \"select * from perimeter\"";
            try
            {
                System.IO.File.AppendAllText(logFile, $"Importar perímetro" + System.Environment.NewLine);
                ExecuteProzess(psi);
                changes = updateProject(prefix + "_perimeter", p);
                System.IO.File.AppendAllText(logFile, changes + System.Environment.NewLine);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                changes = "Error: " + ex.ToString();
                System.IO.File.AppendAllText(logFile, $"ERROR: {ex.ToString()}" + System.Environment.NewLine);
            }

            try
            {
                System.IO.File.AppendAllText(logFile, $"Importar intensidades" + System.Environment.NewLine);
                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_intensities\" \"" + filePath + "\" -sql \"select * from intensities\"";
                ExecuteProzess(psi);
                changes = await updateIntensities(prefix + "_intensities", p);
                System.IO.File.AppendAllText(logFile, changes + System.Environment.NewLine);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                changes = "Error: " + ex.ToString();
                System.IO.File.AppendAllText(logFile, $"ERROR: {ex.ToString()}" + System.Environment.NewLine);
            }

            try
            {
                System.IO.File.AppendAllText(logFile, $"Importar PrA" + System.Environment.NewLine);
                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_pra\" \"" + filePath + "\" -sql \"select * from pra\"";
                ExecuteProzess(psi);
                changes = await updatePras(prefix + "_pra", p);
                System.IO.File.AppendAllText(logFile, changes + System.Environment.NewLine);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                changes = "Error: " + ex.ToString();
                System.IO.File.AppendAllText(logFile, $"ERROR: {ex.ToString()}" + System.Environment.NewLine);
            }

            try
            {
                System.IO.File.AppendAllText(logFile, $"Importar Medida de Mitigación" + System.Environment.NewLine);
                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_protect\" \"" + filePath + "\" -sql \"select * from protect\"";
                ExecuteProzess(psi);
                changes = await updateProtectionMeasure(prefix + "_protect", p);
                System.IO.File.AppendAllText(logFile, changes + System.Environment.NewLine);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                changes = "Error: " + ex.ToString();
                System.IO.File.AppendAllText(logFile, $"ERROR: {ex.ToString()}" + System.Environment.NewLine);
            }

            try
            {
                System.IO.File.AppendAllText(logFile, $"Importar potenciales" + System.Environment.NewLine);
                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_potentials\" \"" + filePath + "\" -sql \"select * from potentials\"";
                ExecuteProzess(psi);
                psi.Arguments = "-F \"PostgreSQL\" " + pgstring + " -nln \"" + prefix + "_resiliencevalues\" \"" + filePath + "\" -sql \"select * from resiliencevalues\"";
                ExecuteProzess(psi);
                changes = await updatePotentials(prefix + "_potentials", p, logFile);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                changes = "Error: " + ex.ToString();
                System.IO.File.AppendAllText(logFile, $"ERROR: {ex.ToString()}" + System.Environment.NewLine);
            }

            // TODO: Delete importet tables

            System.IO.File.AppendAllText(logFile, $"FIN" + System.Environment.NewLine);

            return Content(changes);


        }

        private string updateProject(string tablename, Project p)
        {
            string changes = "";
            try
            {
                using (var context = new MiResilienciaContext())
                {
                    using (var command = context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "select *, st_astext(ST_Force2D(geometry)) as wkt_geometrie from " + tablename;
                        command.CommandType = CommandType.Text;

                        context.Database.OpenConnection();

                        try
                        {

                            using (var result = command.ExecuteReader())
                            {
                                var names = Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToList();
                                
                                while (result.Read())
                                {
                                    string geom = result.GetString("wkt_geometrie");
                                    WKTReader reader = new WKTReader();
                                    reader.IsOldNtsCoordinateSyntaxAllowed = false;
                                    Geometry geometrie = reader.Read(geom);
                                    geometrie.SRID = 3857;

                                    if ((p.geometry == null) || (p.geometry != geometrie))
                                    {
                                        changes += "<li>Perímetro adaptado</li>";
                                        p.geometry = (Polygon)geometrie;
                                        _context.Entry(p).State  = EntityState.Modified;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        { }

                    }
                }
            }
            catch (Exception e2)
            {

            }

            return changes;
            
        }

        private async Task<string> updateIntensities(string tablename, Project p)
        {
            string changes = "";
            try
            {
                using (var context = new MiResilienciaContext())
                {
                    using (var command = context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "select *, st_astext(ST_Force2D(geometry)) as wkt_geometrie from " + tablename;
                        command.CommandType = CommandType.Text;

                        context.Database.OpenConnection();

                        try
                        {

                            using (var result = command.ExecuteReader())
                            {
                                var names = Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToList();

                                while (result.Read())
                                {
                                    string geom = result.GetString("wkt_geometrie");

                                    int nathaz = result.GetInt32("NatHazardID");
                                    int ikclass = result.GetInt32("IKClassesID");
                                    bool before = result.GetBoolean("BeforeAction");
                                    int intensity = result.GetInt32("IntensityDegree");

                                    Intensity existingIntensity = p.Intesities.Where(m => m.IKClassesID == ikclass && m.IntensityDegree == (IntensityDegree)intensity && m.NatHazardID == nathaz && m.BeforeAction == before).FirstOrDefault();

                                    WKTReader reader = new WKTReader();
                                    reader.IsOldNtsCoordinateSyntaxAllowed = false;
                                    Geometry geometrie = reader.Read(geom);
                                    geometrie.SRID = 3857;
                                    bool changed = false;
                                    if (existingIntensity!=null)
                                    {
                                        if (existingIntensity.geometry != geometrie)
                                        {
                                            existingIntensity.geometry = (MultiPolygon)geometrie;
                                            changed = true;
                                        }
                                        if (existingIntensity.NatHazardID != nathaz)
                                        {
                                                existingIntensity.NatHazardID = nathaz; changed = true;
                                            
                                        }
                                        if (existingIntensity.IntensityDegree != (IntensityDegree)intensity)
                                        {
                                            existingIntensity.IntensityDegree = (IntensityDegree)intensity;
                                            changed = true;
                                        }
                                        if (existingIntensity.IKClassesID != ikclass)
                                        {
                                            existingIntensity.IKClassesID = ikclass; changed = true;
                                        }

                                        if (changed)
                                        {
                                            changes += "<li>Intensidades adaptado</li>";
                                            _context.Entry(existingIntensity).State = EntityState.Modified;
                                        }
                                    }
                                    else
                                    {
                                        Intensity newIntensity = new Intensity() { geometry = (MultiPolygon)geometrie, IntensityDegree = (IntensityDegree)intensity, BeforeAction = before, IKClassesID = ikclass, NatHazardID = nathaz };
     
                                        p.Intesities.Add(newIntensity);
                                        _context.Entry(p).State = EntityState.Modified;
                                        changes += "<li>Intensidades agregadas</li>";
                                    }

                                    
                                }
                            }
                        }
                        catch (Exception ex)
                        { }

                    }
                }
            }
            catch (Exception e2)
            {

            }

            return changes;

        }

        private async Task<string> updatePras(string tablename, Project p)
        {
            string changes = "";
            try
            {
                using (var context = new MiResilienciaContext())
                {
                    using (var command = context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "select * from " + tablename;
                        command.CommandType = CommandType.Text;

                        context.Database.OpenConnection();

                        try
                        {

                            using (var result = command.ExecuteReader())
                            {
                                var names = Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToList();

                                while (result.Read())
                                {
                                    int nathaz = result.GetInt32("NatHazardID");
                                    int ikclass = result.GetInt32("IKClassesId");
                                    double value = result.GetDouble("Value");

                                    PrA existingPra = p.PrAs.Where(m=>m.NatHazardId == nathaz && m.IKClassesId == ikclass).FirstOrDefault();

                                    bool changed = false;
                                    if (existingPra != null)
                                    {
                                        if (existingPra.Value != value)
                                        {
                                            existingPra.Value = value;
                                            changed = true;
                                        }

                                        if (changed)
                                        {
                                            changes += "<li>Pra adaptado</li>";
                                            _context.Entry(existingPra).State = EntityState.Modified;
                                        }
                                    }
                                    else
                                    {
                                        PrA newpra = new PrA() { IKClassesId = ikclass, Value = value, NatHazardId = nathaz };

                                        p.PrAs.Add(newpra);
                                        _context.Entry(p).State = EntityState.Modified;
                                        changes += "<li>PrA agregadas</li>";
                                    }


                                }
                            }
                        }
                        catch (Exception ex)
                        { }

                    }
                }
            }
            catch (Exception e2)
            {

            }

            return changes;

        }

        private async Task<string> updateProtectionMeasure(string tablename, Project p)
        {
            string changes = "";
            try
            {
                using (var context = new MiResilienciaContext())
                {
                    using (var command = context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = "select *, st_astext(ST_Force2D(geometry)) as wkt_geometrie from " + tablename;
                        command.CommandType = CommandType.Text;

                        context.Database.OpenConnection();

                        try
                        {

                            using (var result = command.ExecuteReader())
                            {
                                var names = Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToList();

                                while (result.Read())
                                {
                                    string geom = result.GetString("wkt_geometrie");

                                    int Costs = result.GetInt32("Costs");
                                    int LifeSpan = result.GetInt32("LifeSpan");
                                    int OperatingCosts = result.GetInt32("OperatingCosts");
                                    int MaintenanceCosts = result.GetInt32("MaintenanceCosts");
                                    double RateOfReturn = result.GetDouble("RateOfReturn");
                                    string Description = result.GetString("Description");
                                    double ValueAddedTax = result.GetDouble("ValueAddedTax");
                                    double DiscountRate = result.GetDouble("DiscountRate");

                                    ProtectionMeasure existingPM = p.ProtectionMeasure;
                                    if (existingPM == null)
                                    {
                                        existingPM = new ProtectionMeasure();
                                        existingPM.Project = p;
                                    }

                                    WKTReader reader = new WKTReader();
                                    reader.IsOldNtsCoordinateSyntaxAllowed = false;
                                    Geometry geometrie = reader.Read(geom);
                                    geometrie.SRID = 3857;
                                    
                                    existingPM.geometry = (MultiPolygon)geometrie;
                                    existingPM.Costs = Costs;
                                    existingPM.LifeSpan = LifeSpan;
                                    existingPM.OperatingCosts = OperatingCosts;
                                    existingPM.MaintenanceCosts = MaintenanceCosts;
                                    existingPM.RateOfReturn= RateOfReturn;
                                    existingPM.Description = Description;
                                    existingPM.ValueAddedTax= ValueAddedTax;
                                    existingPM.DiscountRate= DiscountRate;

                                    changes += "<li>Medida adaptado</li>";
                                    _context.Entry(existingPM).State = EntityState.Modified;
                                   
                                }
                            }
                        }
                        catch (Exception ex)
                        { }

                    }
                }
            }
            catch (Exception e2)
            {

            }

            return changes;

        }

        private async Task<string> updatePotentials(string tablename, Project p, string logFile)
        {
            string changes = "";
            try
            {
                using (var ct = new MiResilienciaContext())
                {
                    using (var context = new MiResilienciaContext())
                    {
                        using (var command = context.Database.GetDbConnection().CreateCommand())
                        {
                            context.Database.OpenConnection();

                            command.CommandText = "select count(*) from " + tablename;
                            int totalToImport = 0;

                            using (DbDataReader dataReader = command.ExecuteReader())
                                if (dataReader.HasRows)
                                    while (dataReader.Read())
                                        totalToImport = dataReader.GetInt32(0);

                            System.IO.File.AppendAllText(logFile, $"Importar ${totalToImport} potentiales" + System.Environment.NewLine);

                            command.CommandText = "select *, st_astext(ST_Force2D(geometry)) as wkt_geometrie from " + tablename; // + " iop left outer join "+ tablename.Replace("potentials", "resiliencevalues") + " rv on (iop.id = rv.mappedobjectid) " + tablename;
                            command.CommandType = CommandType.Text;

                            int counter = 0;

                            try
                            {

                                using (var result = command.ExecuteReader())
                                {
                                    var names = Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToList();

                                    while (result.Read())
                                    {
                                        counter++;

                                        string geom = result.GetString("wkt_geometrie");
                                        WKTReader reader = new WKTReader();
                                        reader.IsOldNtsCoordinateSyntaxAllowed = false;
                                        Geometry geometrie = reader.Read(geom);
                                        geometrie.SRID = 3857;
                                        string name = result.GetString("name");
                                        int id = result.GetInt32("id");

                                        Objectparameter? o = await ct.Objektparameter.Where(m => m.Name == name && m.MotherOtbjectparameter == null).SingleOrDefaultAsync();
                                        if (o != null)
                                        {
                                            MappedObject mo = new MappedObject() { geometry = geometrie, Project = p, Objectparameter = o };
                                            ct.Entry(mo).State = EntityState.Added;
                                            ct.MappedObjects.Add(mo);
                                            await ct.SaveChangesAsync();
                                            string description = "";
                                            if (!result.IsDBNull("description"))
                                            {
                                                description = result.GetString("description");
                                            }
                                            int value = result.GetInt32("value");
                                            string changevalue = "";
                                            if (!result.IsDBNull("changevalue"))
                                            {
                                                changevalue = result.GetString("changevalue");
                                            }
                                            int floors = result.GetInt32("floors");
                                            int personcount = result.GetInt32("personcount");
                                            string changepersoncount = "";
                                            if (!result.IsDBNull("changepersoncount"))
                                            {
                                                changepersoncount = result.GetString("changepersoncount");
                                            }
                                            double presence = result.GetDouble("presence");
                                            int numberofvehicles = result.GetInt32("numberofvehicles");
                                            int velocity = result.GetInt32("velocity");
                                            int staff = result.GetInt32("staff");

                                            

                                            ToolsController tc = new ToolsController(_userManager, ct);
                                            tc.SaveChanges(mo.ID, name, description, value, changevalue, "", floors, personcount, staff, changepersoncount, presence, numberofvehicles, velocity);

                                            changes += "<li>Potentiales agregadas</li>";
                                            // Resilience values
                                            using (var context2 = new MiResilienciaContext())
                                            {
                                                using (var command2 = context2.Database.GetDbConnection().CreateCommand())
                                                {
                                                    context2.Database.OpenConnection();
                                                    command2.CommandText = "select * from " + tablename.Replace("potentials", "resiliencevalues") + " where mappedobjectid = " + id;

                                                    using (DbDataReader dataReader2 = command2.ExecuteReader())
                                                        if (dataReader2.HasRows)
                                                            while (dataReader2.Read())
                                                            {
                                                                int resilienceweightid = dataReader2.GetInt32("resilienceweightid");
                                                                double overwrittenweight = dataReader2.GetDouble("overwrittenweight");
                                                                double rvalue = dataReader2.GetDouble("value");

                                                                ResilienceWeight rw = await ct.ResilienceWeights.Where(m => m.ID == resilienceweightid).FirstOrDefaultAsync();

                                                                ResilienceValues rv = new ResilienceValues() { MappedObject = mo, OverwrittenWeight = overwrittenweight, Value = rvalue, ResilienceWeight = rw };
                                                                if (mo.ResilienceValues == null) mo.ResilienceValues = new List<ResilienceValues>();
                                                                mo.ResilienceValues.Add(rv);
                                                            }
                                                }
                                            }


                                            System.IO.File.AppendAllText(logFile, $"Potential ({counter} / {totalToImport}) {mo.Objectparameter.Name} agregada" + System.Environment.NewLine);


                                        }


                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.IO.File.AppendAllText(logFile, $"ERROR in potentials: {ex.ToString()}");
                            }

                        }
                    }
                }
            }
            catch (Exception e2)
            {
                System.IO.File.AppendAllText(logFile, $"ERROR in potentials: {e2.ToString()}" + System.Environment.NewLine);
            }

            return changes;

        }

        public IActionResult LogFileViewer()
        {
            return View();
        }

        public async Task<IActionResult> CurrentLogFileContent()
        {
            string? logFile = HttpContext.Session.GetString("logFile");
            if (logFile != null)
            {
                return Content(System.IO.File.ReadAllText(logFile).Replace(System.Environment.NewLine, "<br>"));
            }
            else return Content("No logFile defined");
        }

        /// <summary>
        /// Exports the export view to geopackage with ogr2ogr
        /// </summary>
        /// <returns></returns>
        public IActionResult Export()
        {
            string exportf = Path.GetRandomFileName();
            string[] fname = exportf.Split(".");

            int? projectId = HttpContext.Session.GetInt32("Project");
            if (projectId == null) { return NotFound(); }

            string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            if (!Directory.Exists(dataDir + "//Export")) Directory.CreateDirectory(dataDir + "//Export");
            string exportfilename = dataDir + "//Export//" + fname[0] + ".gpkg";


            ProcessStartInfo psi = new ProcessStartInfo();

            string db = _configuration["Environment:DB"];
            string host = _configuration["Environment:DBHost"];
            string dbuser = _configuration["Environment:DBUSer"];
            string dbpassword = _configuration["Environment:DBPassword"];

            if ((db==null) || (db==""))
            {
                db = _configuration["DB"];
                host = _configuration["DBHost"];
                dbuser = _configuration["DBUSer"];
                dbpassword = _configuration["DBPassword"];
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = @"C:\Program Files\GDAL\ogr2ogr.exe";
                psi.WorkingDirectory = @"C:\Program Files\GDAL";
                psi.EnvironmentVariables["GDAL_DATA"] = @"C:\Program Files\GDAL\gdal-data";
                psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = @"C:\Program Files\GDAL\gdal-plugins";
                psi.EnvironmentVariables["PATH"] = "C:\\Program Files\\GDAL;" + psi.EnvironmentVariables["PATH"];
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                psi.FileName = "ogr2ogr";
            }

                


            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;

            string pgstring = " PG:\"dbname = '" + db + "' user = '" + dbuser + "' password = '" + dbpassword + "' host = '" + host + "'\"";

            // Export Perimeter
            try
            {
                psi.Arguments = "-f GPKG " + exportfilename + pgstring + " \"Export_Perimeter\" -where \"\\\"Id\\\" = " + projectId.Value + "\" -nln \"perimeter\"";
                Console.WriteLine("GPKG Export Parameter list:");
                Console.WriteLine(psi.Arguments);
                
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Pra
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"PrA\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"pra\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Protection Measure
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"ProtectionMeasure\" -where \"\\\"ProjectID\\\" = " + projectId.Value + "\" -nln \"protect\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Intensities
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"Export_Intensity\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"intensities\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Potential
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"Export_Potential\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"potentials\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Resilience Values
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"Export_Resiliencevalues\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"resiliencevalues\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            return Json(new ExportProcess() { Filename = fname[0] + ".gpkg"  });
        }

        /// <summary>
        /// Exports the project from v1 db to gpkg
        /// </summary>
        /// <returns></returns>
        public IActionResult ExportFromV1(int? projectId)
        {
            string exportf = Path.GetRandomFileName();
            string[] fname = exportf.Split(".");

            if (projectId == null) { return NotFound(); }

            string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            if (!Directory.Exists(dataDir + "//Export")) Directory.CreateDirectory(dataDir + "//Export");
            string exportfilename = dataDir + "//Export//" + fname[0] + ".gpkg";


            ProcessStartInfo psi = new ProcessStartInfo();

            string db = _configuration["Environment:DBV1"];
            string host = _configuration["Environment:DBHostV1"];
            string dbuser = _configuration["Environment:DBUserV1"];
            string dbpassword = _configuration["Environment:DBPasswordV1"];

            if ((db == null) || (db == ""))
            {
                db = _configuration["DBV1"];
                host = _configuration["DBHostV1"];
                dbuser = _configuration["DBUserV1"];
                dbpassword = _configuration["DBPasswordV1"];
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = @"C:\Program Files\GDAL\ogr2ogr.exe";
                psi.WorkingDirectory = @"C:\Program Files\GDAL";
                psi.EnvironmentVariables["GDAL_DATA"] = @"C:\Program Files\GDAL\gdal-data";
                psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = @"C:\Program Files\GDAL\gdal-plugins";
                psi.EnvironmentVariables["PATH"] = "C:\\Program Files\\GDAL;" + psi.EnvironmentVariables["PATH"];
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                psi.FileName = "ogr2ogr";
            }




            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;

            string pgstring = " PG:\"dbname = '" + db + "' user = '" + dbuser + "' password = '" + dbpassword + "' host = '" + host + "'\"";

            // Export Perimeter
            try
            {
                psi.Arguments = "-f GPKG " + exportfilename + pgstring + " \"Export_Perimeter\" -where \"\\\"Id\\\" = " + projectId.Value + "\" -nln \"perimeter\"";
                Console.WriteLine("GPKG Export Parameter list:");
                Console.WriteLine(psi.Arguments);

                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Pra
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"PrA\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"pra\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Protection Measure
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"ProtectionMeasure\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"protectionmeasure\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Intensities
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"Export_Intensity\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"intensities\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Potential
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"Export_Potential\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"potentials\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            // Export Resilience Values
            try
            {
                psi.Arguments = "-f GPKG -append " + exportfilename + pgstring + " \"Export_Resiliencevalues\" -where \"\\\"ProjectId\\\" = " + projectId.Value + "\" -nln \"resiliencevalues\"";
                ExecuteProzess(psi);
            }
            catch (Exception ex)
            {
                return Json(new ExportProcess() { Error = ex.ToString() });
            }

            return Json(new ExportProcess() { Filename = fname[0] + ".gpkg" });
        }

        public IActionResult Download(string filename)
        {
            string dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            if (!Directory.Exists(dataDir + "//Export")) Directory.CreateDirectory(dataDir + "//Export");
            string exportfilename = dataDir + "//Export//" + filename;
            Stream stream = System.IO.File.OpenRead(exportfilename);
            if (stream == null)
                return NotFound(); // returns a NotFoundResult with Status404NotFound response.

            string[] fname = filename.Split(".");
            return File(stream, "application/octet-stream", "miresiliencia_export." + fname[1]);
        }

        private string ExecuteProzess(ProcessStartInfo psi)
        {
            var process = new Process()
            {
                StartInfo = psi,

            };

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            string output = "";
            string error = "";

            process.OutputDataReceived += (sender, data) => {
                output += data;
            };

            process.ErrorDataReceived += (sender, data) => {
                error += data;
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw new InvalidOperationException(output + " " + error);
            }
        }

        private static Random random = new Random();
        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
    public class ExportProcess
    {
        public string Error { get; set; }
        public string Output { get; set; }
        public string Filename { get; set; }

    }
}
