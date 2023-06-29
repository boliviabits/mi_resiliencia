using Microsoft.AspNetCore.Mvc;

namespace MiResiliencia.Controllers
{
	public class LayersController : Controller
	{
		public ActionResult ProjectLayers()
		{
			return PartialView();
		}
	}
}
