
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Report.Server.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Report.Server.Services.TelerikReportService;

namespace Report.Server.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly RedisService _redisService;
        private readonly TelerikReportService _telerikReportService;

        public ReportsController(RedisService redisService, TelerikReportService telerikReportService)
        {
            _redisService = redisService;
            _telerikReportService = telerikReportService;
        }
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

                var categoriesJson = await _redisService.GetMerchantCategoriesAsync(merchantGuid);


                return Ok(categoriesJson);
            }
            catch (Exception ex)
            {
           
                return StatusCode(500, new { message = ex.Message, details = ex.StackTrace });
            }
     
        }
        [HttpGet("merchant/{categoryId}/reports")]
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
