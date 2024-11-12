
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
    public class ReportsController : ControllerBase
    {
        private readonly RedisService _redisService;
        private readonly IRedisClientsManager _redisClientsManager;

        public ReportsController(RedisService redisService, IRedisClientsManager redisClientsManager)
        {
            _redisService = redisService;
            _redisClientsManager = redisClientsManager;

        }
        //[HttpGet("merchants/{merchantGuid}/reports")]
        //[Authorize]
        //public async Task<IActionResult> GetMerchantReports([FromRoute] string merchantGuid)
        //{

        //    if (string.IsNullOrEmpty(merchantGuid))
        //    {
        //        return BadRequest(new { message = "Merchant GUID cannot be null or empty." });
        //    }


        //    try
        //    {

        //        var categoriesJson = await _redisService.GetMerchantCategoriesAsync(merchantGuid);


        //        return Ok(categoriesJson);
        //    }
        //    catch (Exception ex)
        //    {

        //        return StatusCode(500, new { message = ex.Message, details = ex.StackTrace });
        //    }
        //}
        [HttpGet("merchants/{merchantGuid}/reports")]
        [Authorize]
        public async Task<IActionResult> GetMerchantReports([FromRoute] string merchantGuid)
        {
            if (string.IsNullOrEmpty(merchantGuid))
            {
                return BadRequest(new { message = "Merchant GUID cannot be null or empty." });
            }

            try
            {
                var client = await _redisClientsManager.GetClientAsync();
                var merchantsJson = await client.GetValueAsync("wyo:report_server:merchants");

                if (string.IsNullOrEmpty(merchantsJson))
                {
                    return StatusCode(500, new { message = "Merchant data not found in Redis." });
                }

                var targetMerchant = JsonConvert.DeserializeObject<List<MerchantData>>(merchantsJson).FirstOrDefault(m => m.MerchantGuid == merchantGuid);

                if (targetMerchant == null)
                {
                    return NotFound(new { message = $"Merchant with GUID {merchantGuid} not found." });
                }

                bool hasAccess = HttpContext.User.Claims.Any(claim =>
                {
                    var values = claim.Value.Replace("\"", "").Split(',')
                        .Select(v => int.TryParse(v, out int intValue) ? (int?)intValue : null)
                        .Where(v => v.HasValue)
                        .Select(v => v.Value);
                    return values.Contains(targetMerchant.licenseId);
                });

                if (!hasAccess)
                {
                    return Forbid("You do not have access to this resource.");
                }

                return Ok(await _redisService.GetMerchantCategoriesAsync(merchantGuid));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, details = ex.StackTrace });
            }

        }

        [HttpGet("merchant/{categoryId}/reports")]
        [Authorize]
        public async Task<IActionResult> GetCategoryReports([FromRoute] string categoryId)
        {

            if (string.IsNullOrEmpty(categoryId))
            {
                return BadRequest(new { message = "Merchant GUID cannot be null or empty." });
            }

            try
            {

                var categoriesJson = await _redisService.GetCategoryReportsAsync(categoryId);


                return Ok(categoriesJson);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { message = ex.Message, details = ex.StackTrace });
            }
        }

    }

}
