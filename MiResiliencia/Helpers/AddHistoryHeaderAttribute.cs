using Microsoft.AspNetCore.Mvc.Filters;

namespace MiResiliencia.Helpers
{
    public class AddHistoryHeaderAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string app = filterContext.HttpContext.Request.Host.Value;
            string tool = filterContext.HttpContext.Request.Path;
            Console.WriteLine(filterContext.HttpContext.Request.QueryString.Value);
            if ((filterContext.HttpContext.Request.QueryString.Value.Contains("ic-request=true")) && (!tool.Contains("Admin")))
                filterContext.HttpContext.Response.Headers.Append("X-IC-PushURL", "?inside=" + tool + "#home");

            base.OnActionExecuting(filterContext);
        }
    }
}
