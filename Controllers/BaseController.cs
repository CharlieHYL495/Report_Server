using Microsoft.AspNetCore.Mvc;

namespace Report.Server.Controllers
{
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected bool HasMerchantAccess(int licenseId)
        {
           
            var licenseIds = HttpContext.User.Claims
                .Where(c => c.Type is "owner" or "admin" or "user")
                .SelectMany(c => c.Value.Split(','))
                .Where(v => int.TryParse(v.Trim(), out int _))
                .Select(int.Parse)
                .ToHashSet();

            return licenseIds.Contains(licenseId);
        }
    }
}