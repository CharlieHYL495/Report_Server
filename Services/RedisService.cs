using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.Redis;
using System.Collections.Generic;
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

        {   if (key == null)
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
        


            if (string.IsNullOrEmpty(reportCategoriesJson))
            {
                return JsonConvert.SerializeObject(new { categories = new string[] { } });
            }


            //var categories = JsonConvert.DeserializeObject<List<string>>(reportCategoriesJson);

            return reportCategoriesJson;
        
        }
    }
}
