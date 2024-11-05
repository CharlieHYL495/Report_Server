using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.Redis;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static Reporting.Server.Services.TelerikReportServerClient;

namespace Reporting.Server.Services
{
    public class RedisService
    {
        private readonly IRedisClientsManager _redisClientsManager;
        //private readonly TelerikReportServerClient _TelerikReportServerClient;

        public RedisService(IRedisClientsManager redisClientsManager)
        {
            _redisClientsManager = redisClientsManager;
            //_TelerikReportServerClient = TelerikReportServerClient;
        }

        public async Task<Dictionary<string, string>> GetAllDataAsync()
        {
            var allData = new Dictionary<string, string>();
            var keys = _redisClientsManager.GetClient().GetAllKeys();

            foreach (var key in keys)
            {
                var value = await Task.Run(() => _redisClientsManager.GetClient().GetValue(key));
                allData[key] = value;
            }

            return allData;
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
