using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.Redis;
using ServiceStack;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using static Report.Server.Services.TelerikReportService;


namespace Report.Server.Services
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    public class RedisService
    {
        private readonly IRedisClientsManager _redisClientsManager;
        private readonly string _merchantsKey;
        private readonly string _redisKeyPrefix;

        public RedisService(IRedisClientsManager redisClientsManager, IOptions<RedisKeysOptions> redisOptions)
        {
            _redisClientsManager = redisClientsManager;
            _redisKeyPrefix = redisOptions.Value.RedisKeyPrefix;
            _merchantsKey = redisOptions.Value.MerchantsKey;
        }

        public async Task<MerchantData> GetMerchantInfoAsync(string merchantGuid)
        {
            var client = await _redisClientsManager.GetClientAsync();
            var merchantsJson = await client.GetValueAsync(_merchantsKey);
            var merchants = JsonConvert.DeserializeObject<List<MerchantData>>(merchantsJson);
            return merchants.FirstOrDefault(x => x.MerchantGuid == merchantGuid) ?? new MerchantData();
        }

        public async Task<List<object>> GetMerchantCategoriesAsync(string merchantGuid)
        {
            var client = await _redisClientsManager.GetClientAsync();
            var merchant = await GetMerchantInfoAsync(merchantGuid);
            var categories = await Task.WhenAll(
                merchant.ReportCategories.Select(async category =>
                {
                    return JSON.parse(await client.GetValueAsync($"{_redisKeyPrefix}{category}"));
                })
            );
            return categories.ToList();
        }

        public async Task<List<object>> GetAllDataAsync()
        {
            await using var redisClient = await _redisClientsManager.GetClientAsync();
            var keys = await redisClient.GetAllKeysAsync();

            var allData = (await Task.WhenAll(keys.Select(async key =>
                JSON.parse(await redisClient.GetValueAsync(key)))));
            return allData.ToList();
        }



    }
}




public class MerchantData
{
    [JsonProperty("guid")]
    public string MerchantGuid { get; set; }

    [JsonProperty("report_categories")]
    public List<string> ReportCategories { get; set; }

    [JsonProperty("license_id")]
    public int licenseId { get; set; }

}