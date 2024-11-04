using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Server.Services;

namespace Report.Server.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class TelerikController : ControllerBase
    {
        private readonly RedisService _redisService;

        public TelerikController(RedisService redisService)
        {

            _redisService = redisService;
        }
        [Authorize]
        [HttpGet("Telerik")]
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
