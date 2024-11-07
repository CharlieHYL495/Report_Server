using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.Redis;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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


        public async Task<string> GetMerchantCategories(string merchantGuid)
        {
            // 拉取商家的报表类别
            if (string.IsNullOrEmpty(merchantGuid))
            {
                throw new ArgumentNullException(nameof(merchantGuid), "Merchant GUID cannot be null or empty.");
            }


            var categoryKey = $"report_server:merchants:{merchantGuid}:report_categories";
            var reportCategoriesJson = _redisClientsManager.GetClient().GetValue(categoryKey);



            var categories = string.IsNullOrEmpty(reportCategoriesJson)
                ? new { categories = new string[] { } }
                : JsonConvert.DeserializeObject(reportCategoriesJson);
            return JsonConvert.SerializeObject(categories);

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

