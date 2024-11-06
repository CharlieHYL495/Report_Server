
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Report.Server.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Reporting.Server.Services.TelerikReportServerClient;

namespace Report.Server.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly RedisService _redisService;

        public ReportsController(RedisService redisService)
        {
            _redisService = redisService;
        }
        [HttpGet("merchants/{merchantGuid}/reports")]
        //[Authorize]
        public async Task<IActionResult> GetMerchantReports([FromRoute] string merchantGuid)
        {

            if (string.IsNullOrEmpty(merchantGuid))
            {
                return BadRequest(new { message = "Merchant GUID cannot be null or empty." });
            }

            try
            {
                // 获取商家的报表类别
                var categoriesJson = await _redisService.GetMerchantCategories(merchantGuid);

                // 假设 GetMerchantReportsAsync 返回的是一个 JSON 格式的字符串，需要反序列化
                var categories = JsonConvert.DeserializeObject<List<string>>(categoriesJson)?.ToList() ?? new List<string>();

                // 检查是否有报表类别
                if (categories.Count == 0)
                {
                    return NotFound(new { message = "No reports found for the merchant." });
                }

                return Ok(new { categories });
            }
            catch (Exception ex)
            {
                // 返回 500 错误及异常信息
                return StatusCode(500, new { message = ex.Message, details = ex.StackTrace });
            }

        }
    }

}
