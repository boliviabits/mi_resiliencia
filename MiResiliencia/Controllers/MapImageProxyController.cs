using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiResiliencia.Models;
using static System.Net.WebRequestMethods;
using System.Net;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Identity;

namespace MiResiliencia.Controllers
{
    public class MapImageProxyController : Controller
    {
        public IConfiguration Configuration { get; }
        public MiResilienciaContext _context = new MiResilienciaContext();
        private UserManager<ApplicationUser> _userManager;

        public MapImageProxyController(UserManager<ApplicationUser> userManager, IConfiguration configuration, MiResilienciaContext context)
        {
            Configuration = configuration;
            _context = context;
            _userManager = userManager;
        }


        [AcceptVerbs(Http.Get, Http.Head, Http.MkCol, Http.Post, Http.Put)]
        [ReadableBodyStream]
        public JsonResult GetGeoServer(string param, string workbench)
        {
            HttpRequest original = this.HttpContext.Request;

            original.EnableBuffering();
            string geoserver = Configuration["Environment:Geoserver"];
            if ((geoserver == null) || (geoserver == "") || (geoserver == "https://geoserver.yourserver.domain/geoserver/")) geoserver = Configuration["Geoserver"];

            HttpWebRequest newRequest = (HttpWebRequest)WebRequest.Create(geoserver + workbench + "/ows?");
           
            newRequest.ContentType = original.ContentType;
            newRequest.Method = original.Method;

            HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            string body;
            using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
            {
                Task<string> task = stream.ReadToEndAsync();
                task.Wait();
                body = task.Result;
            }

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] originalStream = encoding.GetBytes(body);

            Stream reqStream = newRequest.GetRequestStream();
            reqStream.Write(originalStream, 0, originalStream.Length);
            reqStream.Close();


            //newRequest.GetResponse();
            var httpResponse = (HttpWebResponse)newRequest.GetResponse();



            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                // get the postgis-id from the new objects (WFS-T Answer in XML)

                string myxml = streamReader.ReadToEnd();
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(myxml); // suppose that myXmlString contains "<Names>...</Names>"

                List<string> newObjects = new List<string>();
                foreach (XmlNode x in xml.ChildNodes[1].ChildNodes)
                {
                    if (x.Name == "wfs:InsertResults")
                    {
                        foreach (XmlNode feature in x.ChildNodes)
                        {
                            XmlNode featureId = feature.FirstChild;
                            string newId = featureId.Attributes[0].Value.ToString();
                            newObjects.Add(newId);
                        }
                    }
                }

                return Json(newObjects);
            }
        }
    }
}
