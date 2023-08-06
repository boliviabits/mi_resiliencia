using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiResiliencia.Models;
using System.Net;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MiResiliencia.Controllers
{
    public class GeoserverProxyController : Controller
    {
        public IConfiguration Configuration { get; }
        public MiResilienciaContext _context = new MiResilienciaContext();
        private UserManager<ApplicationUser> _userManager;

        public GeoserverProxyController(UserManager<ApplicationUser> userManager, IConfiguration configuration, MiResilienciaContext context)
        {
            Configuration = configuration;
            _context = context;
            _userManager = userManager;
        }

        private async Task<int> getMyCompanyID()
        {
            var id = _userManager.GetUserId((ClaimsPrincipal)User);
            var applicationUser = await _userManager.GetUserAsync((ClaimsPrincipal)User);
            await _context.Entry(applicationUser).Reference(m => m.MyWorkingProjekt).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.MainCompany).LoadAsync();
            await _context.Entry(applicationUser).Reference(m => m.UserSettings).LoadAsync();

            if (applicationUser.MainCompanyID != null) return (int)applicationUser.MainCompanyID;
            Company myCompany = await _context.Companies.Where(m => m.AdminUsers.Any(x => x.AdminRefId == applicationUser.Id)).FirstOrDefaultAsync();
            if (myCompany != null) return myCompany.ID;
            myCompany = await _context.Companies.Where(m => m.CompanyUsers.Any(x => x.CompanyUserRefId == applicationUser.Id)).FirstOrDefaultAsync();
            if (myCompany != null) return myCompany.ID;
            else return 0;

        }

        // GET: GeoserverProxy
        public async Task<ActionResult> Http()
        {


            string geoserver = Configuration["Environment:Geoserver"];
            if ((geoserver==null) || (geoserver == "")) geoserver = Configuration["Geoserver"];
            //return Content()
            string content;
            string url = HttpContext.Request.Path.Value.Replace("/proxy", "");
            string query = HttpContext.Request.QueryString.Value;


            url = geoserver + url + query;
            url = url.Replace("//geoserver/", "/geoserver/");

            // replace Company
            if (url.Contains("{CompanyID}"))
            {
                url = url.Replace("{CompanyID}", (await getMyCompanyID()).ToString());
            }
            if (url.Contains("{ProjectID}"))
            {
                url = url.Replace("{ProjectID}", HttpContext.Session.GetInt32("Project").ToString());
            }

            // Create a request for the URL. 		
            var req = HttpWebRequest.Create(url);
            req.Method = HttpContext.Request.Method;

            //-- No need to copy input stream for GET (actually it would throw an exception)
            if (req.Method != "GET")
            {
                //req.ContentType = "application/json";
                req.ContentType = HttpContext.Request.ContentType;

                Request.Body.Position = 0;  //***** THIS IS REALLY IMPORTANT GOTCHA

                var requestStream = HttpContext.Request.Body;
                Stream webStream = null;
                try
                {
                    //copy incoming request body to outgoing request
                    if (requestStream != null && requestStream.Length > 0)
                    {
                        req.ContentLength = requestStream.Length;
                        webStream = req.GetRequestStream();
                        requestStream.CopyTo(webStream);
                    }
                }
                finally
                {
                    if (null != webStream)
                    {
                        webStream.Flush();
                        webStream.Close();
                    }
                }
            }

            // If required by the server, set the credentials.
            req.Credentials = CredentialCache.DefaultCredentials;
            try
            {
                // No more ProtocolViolationException!
                HttpWebResponse response = (HttpWebResponse)req.GetResponse();

                // Display the status.
                //Console.WriteLine(response.StatusDescription);

                //Response.ContentType = response.ContentType;
                // Get the stream containing content returned by the server.

                string contentType = response.ContentType;
                Stream content2 = response.GetResponseStream();
                StreamReader contentReader = new StreamReader(content2);

                return base.File(content2, contentType);
            }
            catch (Exception e)
            {
                return Content("Error");
                //throw new HttpException(404, "The Proxy returned an error: " + e.ToString(), e.InnerException);
            }

        }
    }
}
