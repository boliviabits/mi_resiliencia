using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiResiliencia.Models;

namespace MiResiliencia.Components
{
    public class LayerStyleViewComponent : ViewComponent
    {
        private UserManager<ApplicationUser> _userManager;
        private MiResilienciaContext _context;

        public LayerStyleViewComponent(UserManager<ApplicationUser> userManager, MiResilienciaContext context)
        {
            _userManager = userManager;
            _context = context;
        }
    
        public async Task<IViewComponentResult> InvokeAsync(string idlist)
        {
            List<LayerStyle> styles = await _context.LayerStyles.ToListAsync();
            return View(styles);
        }
    }
}
