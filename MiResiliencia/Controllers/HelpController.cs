using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiResiliencia.Models;

namespace MiResiliencia.Controllers
{
    [Authorize]
    public class HelpController : Controller
    {
        private UserManager<ApplicationUser> _userManager;
        private MiResilienciaContext _context;

        public HelpController(UserManager<ApplicationUser> userManager, MiResilienciaContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public PartialViewResult ShowHelp(string wikiname)
        {
            List<Help> help = _context.Helps.Where(m => m.Wikiname == wikiname).ToList();
            if (help.Count == 0) return PartialView(new Help() { Content = "Not found" });
            Help spainHelp = help.Where(m => m.Language == "es").FirstOrDefault();
            if (spainHelp != null) return PartialView(spainHelp);
            else return PartialView(help.First());
        }
    }
}
