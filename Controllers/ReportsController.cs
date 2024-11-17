
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Report.Server.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static Report.Server.Services.TelerikReportService;
using ServiceStack.Redis;
using System.IdentityModel.Tokens.Jwt;

namespace Report.Server.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class ReportsController : BaseController
    {
        private readonly RedisService _redisService;

        public ReportsController(RedisService redisService)
        {
            _redisService = redisService;
        }

        [HttpGet("merchants/{merchantGuid}/reports")]
        [Authorize]
        public async Task<IActionResult> GetMerchantReports([FromRoute] string merchantGuid)
        {
            if (string.IsNullOrEmpty(merchantGuid)) return BadRequest(new { message = "Invalid Merchant Id" });
            var merchantData = await _redisService.GetMerchantInfoAsync(merchantGuid);
            if (string.IsNullOrEmpty(merchantData.MerchantGuid))
                return BadRequest(new { message = "Invalid Merchant Id" });
            if (!HasMerchantAccess(merchantData.licenseId))
                return BadRequest(new { message = "No access to the resources" });
            var categoriesJson = await _redisService.GetMerchantCategoriesAsync(merchantGuid);
            return Ok(categoriesJson);

        }
        [HttpGet("Telerik")]
        [Authorize]
        public async Task<IActionResult> GetTelerikReports()
        {
            var TelerikData = await _redisService.GetAllDataAsync();
            return TelerikData == null || !TelerikData.Any()
                ? NotFound(new { Message = "No Telerik data found in Redis." })
                : Ok(TelerikData);
        }
    }


}
