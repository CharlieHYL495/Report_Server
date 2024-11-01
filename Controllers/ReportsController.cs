using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reporting.Server.Services;

namespace Report.Server.Controllers
{
    [Route("api/v1/merchants/{merchantGuid}/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly RedisService _redisService;

        public ReportsController(RedisService redisService)
        {

            _redisService = redisService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMerchantReports([FromRoute] string merchantGuid)
        {

            var data = await _redisService.GetCategoryDataAsync(merchantGuid);

            if (data == null)
            {
                return NotFound("No reports found for this merchant.");
            }

            return Ok(data);
        }
    }
}
