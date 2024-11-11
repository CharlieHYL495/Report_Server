using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.Redis;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ServiceStack;
using static Report.Server.Services.TelerikReportService;


namespace Report.Server.Services
{
    public class RedisService
    {
        private readonly IRedisClientsManager _redisClientsManager;


        public RedisService(IRedisClientsManager redisClientsManager)
        {
            _redisClientsManager = redisClientsManager;
        }

        public async Task<Dictionary<string, string>> GetAllDataAsync()
        {
            var data = new Dictionary<string, string>();
            var client = await _redisClientsManager.GetClientAsync();

            var keys = await client.SearchKeysAsync("*");

            foreach (var key in keys)
            {
                var value = await client.GetValueAsync(key);  
                data[key] = value;
            }

            return data;
        }


        public async Task<List<object>> GetMerchantCategoriesAsync(string MerchantGuid)
        {
            var MerchantsRedisKey = $"wyo:report_server:merchants";
            var client = await _redisClientsManager.GetClientAsync();

            var MerchantsJson = await client.GetValueAsync(MerchantsRedisKey);
            var merchantList = JsonConvert.DeserializeObject<List<MerchantData>>(MerchantsJson);

            var categories = new List<string>();
            foreach (var merchant in merchantList)
            {
                if (merchant.MerchantGuid == MerchantGuid)
                {
                    categories.AddRange(merchant.ReportCategories);
                }
            }

            var Categories = new List<object>();
            foreach (var category in categories)
            {
                var catagoriesJson = await client.GetValueAsync(category);
                Categories.Add(JSON.parse(catagoriesJson));
            }
            
            return Categories;
        }
    


    public async Task<string> GetCategoryReportsAsync(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId))
        {
            throw new ArgumentNullException(nameof(categoryId), "Report category ID cannot be null or empty.");
        }


        var client = await _redisClientsManager.GetClientAsync();


        var reportsRedisKey = $"wyo:report_server:reports:{categoryId}";

        var reportCategoriesJson = await client.GetValueAsync(reportsRedisKey);

        if (string.IsNullOrEmpty(reportCategoriesJson))
        {
            return "No reports found for the given category.";
        }
        
        return reportCategoriesJson;
    }

}
}


public class MerchantData
{
    [JsonProperty("merchant_guid")]
    public string MerchantGuid { get; set; }

    [JsonProperty("report_categories")]
    public List<string> ReportCategories { get; set; }
}