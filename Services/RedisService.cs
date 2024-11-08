using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.Redis;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
            Console.WriteLine($"Connecting to Redis at {_redisClientsManager.GetClientAsync()}");


            await using (var redis = await _redisClientsManager.GetClientAsync())
            {

                var keys = await redis.SearchKeysAsync("*");


                foreach (var key in keys)
                {
                    var value = await redis.GetValueAsync(key); // 异步获取键的值
                    data[key] = value;
                }
            }

            return data;
        }


        public async Task<string> GetCategoryDataAsync(string key)

        {
            if (key == null)
            { throw new ArgumentNullException(nameof(key)); }
            else
            {
                var value = await Task.Run(() => _redisClientsManager.GetClient().GetValue(key));

                return string.IsNullOrEmpty(value) ? "Key not found." : value;
            }
        }


        public async Task<List<string>> GetMerchantCategoriesAsync(string MerchantGuid)
        {

                // 拉取商家的报表类别
                var MerchantsRedisKey = $"wyo:report_server:merchants";

                // 异步获取 Redis 中存储的值
                var MerchantsJson = await Task.Run(() => _redisClientsManager.GetClient().GetValue(MerchantsRedisKey));




            // 反序列化 JSON 数据为 List<MerchantData> 对象
            var merchantList = JsonConvert.DeserializeObject<List<MerchantData>>(MerchantsJson);

            var categories = new List<string>();

            var Categories = new List<string>();
            foreach (var merchant in merchantList)
            {
                
                if (merchant.MerchantGuid == MerchantGuid)
                {
                    foreach (var category in merchant.ReportCategories)
                    {
                        categories.Add(category);
                    }
                }
            }

             
            foreach (var category in categories)
            {
                
                var catagoriesJson = await Task.Run(() => _redisClientsManager.GetClient().GetValue(category));
                Categories.Add(catagoriesJson);
            }

            return Categories;
        }

        public async Task<string> GetCategoryReports(string reportcategoryid)
        {
            // 拉取商家的报表类别
            if (string.IsNullOrEmpty(reportcategoryid))
            {
                throw new ArgumentNullException(nameof(reportcategoryid), "Merchant GUID cannot be null or empty.");
            }


            var ReportsRedisKey = $"wyo:report_server: reports: {reportcategoryid}";


            var reportCategoriesJson = _redisClientsManager.GetClient().GetValue(ReportsRedisKey);



            //var categories = string.IsNullOrEmpty(reportCategoriesJson)
            //    ? new { categories = new string[] { } }
            //    : JsonConvert.DeserializeObject(reportCategoriesJson);
            return reportCategoriesJson;

        }
        //public async Task<string> GetApiDataAsync(string token)
        //{
        //    var client = _httpClientFactory.CreateClient();

        //    // 将 Bearer Token 添加到请求头
        //    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        //    var response = await client.GetAsync("https://your-api-url.com/data");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        return content;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

    }
}

//public class reportCategoriesJson
//{
//    public List<TelerikReportCategory> Categories { get; set; } = new List<TelerikReportCategory>();

//}
public class MerchantData
{
    [JsonProperty("merchant_guid")]
    public string MerchantGuid { get; set; }

    [JsonProperty("report_categories")]
    public List<string> ReportCategories { get; set; }
}