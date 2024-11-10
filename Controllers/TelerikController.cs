using Report.Server.Services;

namespace Reporting.Server.Services
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    [ApiController]
    [Route("api/v1")]
    public class TelerikController : ControllerBase
    {

        private readonly RedisService _redisService;
        private readonly TelerikReportService _telerikReportService;

        public TelerikController(RedisService redisService, TelerikReportService telerikReportService)
        {

            _redisService = redisService;
            _telerikReportService = telerikReportService;
        }

        [HttpGet("Telerik")]
        [Authorize]

        public async Task<IActionResult> GetAllRedisData()
        {



            var allData = await _redisService.GetAllDataAsync(); 

            if (allData.Count == 0)
            {
                return NotFound("No data found.");
            }

            return Ok(allData);
        }
    }

}
