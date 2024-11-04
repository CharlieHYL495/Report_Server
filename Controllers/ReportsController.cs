using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Server.Services;
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

        [Authorize]
        [HttpGet("merchants/{merchantGuid}/reports")]
        public async Task<IActionResult> GetMerchantReports([FromRoute] string merchantGuid)
        {
            // 从 Redis 获取商家的所有报表类别
            var reportCategoriesData = await _redisService.GetCategoryDataAsync(merchantGuid);

            if (reportCategoriesData == null)
            {
                return NotFound("No reports found for this merchant.");
            }

            // 假设从 Redis 获取的数据是某种格式，解析为 ReportCategory 列表
            var reportCategories = new List<ReportCategory>();

            // 解析从 Redis 获取的数据
            // 假设 reportCategoriesData 是一个 JSON 字符串，你可以根据实际情况调整
            foreach (var category in reportCategoriesData)
            {
                reportCategories.Add(new ReportCategory
                {
                    CategoryName = category.CategoryName, // 根据实际字段名调整
                    Reports = category.Reports // 这里获取报表列表
                });
            }

            var response = new MerchantReportResponse
            {
                MerchantGuid = merchantGuid,
                TelerikReportCategory = reportCategories
            };

            return Ok(response);
        }
    }
}
public class MerchantReportResponse
{
    public string MerchantGuid { get; set; }
    public List<TelerikReportCategory> TelerikReportCategory { get; set; }
}

public class TelerikReportsResponse
{
    public string CategoryName { get; set; }
    public List<string> Reports { get; set; } // 你可以根据具体报表对象类型调整
}
