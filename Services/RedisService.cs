using Microsoft.Extensions.Options;
using ServiceStack.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Reporting.Server.Services
{
    public class RedisService
    {
        private readonly RedisClient _redisClient;

        public RedisService(IOptions<RedisOptions> options)
        {
            _redisClient = new RedisClient(options.Value.Host, options.Value.Port, options.Value.Password);
        }

        public async Task<Dictionary<string, string>> GetAllDataAsync()
        {
            var allData = new Dictionary<string, string>();
            var keys = _redisClient.GetAllKeys();

            foreach (var key in keys)
            {
                var value = await Task.Run(() => _redisClient.GetValue(key));
                allData[key] = value;
            }

            return allData;
        }

        public async Task<string> GetCategoryDataAsync(string key)
        {
            var value = await Task.Run(() => _redisClient.GetValue(key));

            return string.IsNullOrEmpty(value) ? "Key not found." : value;
        }
    }
}
