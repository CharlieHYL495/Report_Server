   namespace Reporting.Server.Services
    {

        using StackExchange.Redis;
        using System.Threading.Tasks;
        using Microsoft.Extensions.Hosting;
        public class RedisService
        {
            private readonly ConnectionMultiplexer _redis;

            public RedisService(string redisConnectionString)
            {
                _redis = ConnectionMultiplexer.Connect(redisConnectionString);
            }

            public async Task<Dictionary<string, string>> GetAllDataAsync()
            {
                var db = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints()[0]);

                var allData = new Dictionary<string, string>();


                var keys = server.Keys();


                foreach (var key in keys)
                {
                    var value = await db.StringGetAsync(key);
                    allData[key] = value;
                }

                return allData;
            }

            public async Task<string> GetCategoryDataAsync(string key)
            {
                var db = _redis.GetDatabase();


                var value = await db.StringGetAsync(key);


                if (value.IsNullOrEmpty)
                {
                    return "Key not found.";
                }

                return value;
            }



        }

    }

