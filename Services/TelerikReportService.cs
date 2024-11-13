namespace Report.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Flurl;
    using Flurl.Http;
    using Newtonsoft.Json;
    using Sentry;
    using Telerik.Reporting;
    using StackExchange.Redis;
    using ServiceStack.Redis;
    using System.Text;
    using System.Reflection.Metadata;
    using Telerik.Reporting.Processing;
    using System.Configuration;
    using DocumentFormat.OpenXml.Bibliography;
    using Microsoft.Extensions.Options;
    using Sentry.Protocol;

    public class TelerikReportService
    {
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly string _categoriesPath;
        private readonly string _tokenPath;
        private readonly string _reportByCategoryPath;
        private readonly string _parametersPath;
        private readonly string _savePath;
        private readonly string _redisKeyPrefix;
        private readonly IRedisClientsManager _redisClientsManager;

        public TelerikReportService(IOptions<TelerikReportOptions> telerikOptions,
            IOptions<RedisKeysOptions> redisOptions,
            IRedisClientsManager redisClientsManager)
        {
            _baseUrl = telerikOptions.Value.BaseUrl;
            _username = telerikOptions.Value.Username;
            _password = telerikOptions.Value.Password;
            _tokenPath = telerikOptions.Value.TokenPath;
            _categoriesPath = telerikOptions.Value.CategoriesPath;
            _reportByCategoryPath = telerikOptions.Value.ReportsByCategoryPath;
            _parametersPath = telerikOptions.Value.ParametersPath;
            _savePath = telerikOptions.Value.SavePath;
            _redisKeyPrefix = redisOptions.Value.RedisKeyPrefix;
            _redisClientsManager = redisClientsManager;
        }
        public async Task<string> GetTokenAsync()
        {
            var token = new TelerikUserToken();
            try
            {
                token = await _baseUrl.AppendPathSegment(_tokenPath)
                    .PostUrlEncodedAsync(new Dictionary<string, string>()
                    {
                        { "grant_type", "password" },
                        { "username", _username },
                        { "password", _password }
                    }).Result.GetJsonAsync<TelerikUserToken>();
                Console.WriteLine(token?.access_token);
                return token?.access_token;

            }
            catch (FlurlHttpException ex)
            {

                SentrySdk.CaptureException(ex);
                return null;
            }
        }
        // get report categories and save them in redis and local file
        public async Task<IEnumerable<TelerikReportCategory>> GetReportCategoriesAsync(string token)
        {
            var result = await _baseUrl
                .AppendPathSegment(_categoriesPath)
                .WithOAuthBearerToken(token)
                .GetJsonAsync<IEnumerable<TelerikReportCategory>>();
            await SaveCategoriesAsync(result, token);
            return result;
        }
        // get reportInfos by category
        private async Task<IEnumerable<TelerikReportInfo>> ReportInfosByCategoryIdAsync(string token, string categoryId)
        {
            return await _baseUrl
                .WithOAuthBearerToken(token)
                .AppendPathSegment(string.Format(_reportByCategoryPath, categoryId))
                .GetJsonAsync<IEnumerable<TelerikReportInfo>>();
        }
        // get parameterInfos for each report
        private async Task<IEnumerable<TelerikReportParameter>> ReportParametersAsync(string token, string reportId)
        {
            var result = await _baseUrl
                .AppendPathSegment(string.Format(_parametersPath, reportId))
                .WithOAuthBearerToken(token)
                .GetJsonAsync<IEnumerable<TelerikReportParameter>>();
            return result;
        }
        // main logic to save categories in redis and local file

        private async Task SaveCategoriesAsync(IEnumerable<TelerikReportCategory> categories, string token)
        {
            var tasks = categories.Select(async category =>
            {
                var reports = await ReportInfosByCategoryIdAsync(token, category.Id);

                // Retrieve parameters for each report and embed them in the report object
                var reportList = await Task.WhenAll(reports.Select(async report =>
                {
                    var parameters = await ReportParametersAsync(token, report.Id);
                    report.Parameters = parameters.ToList();
                    return report;
                }));

                // Create a new object with category and reports
                var categoryWithReports = new
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Reports = reportList
                };

                // Save to Redis
                var categoryJson = JsonConvert.SerializeObject(categoryWithReports, Formatting.Indented);
                _redisClientsManager.GetClient().SetValue($"{_redisKeyPrefix}{category.Id}", categoryJson);

                // Save to local file system
                await SaveCategoryToFileAsync(token);
            });

            await Task.WhenAll(tasks);
        }


        private async Task SaveCategoryToFileAsync(string token)
        {
            var response = _baseUrl
                .AppendPathSegment(_categoriesPath)
                .WithOAuthBearerToken(token);
            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            var filePath = Path.Combine(_savePath);
            await File.WriteAllBytesAsync(filePath, fileBytes);
        }




        public class TelerikUserToken
        {
            public string access_token { get; set; }

            [JsonProperty(".issued")] public string Issued { get; set; }

            [JsonProperty(".expires")] public string Expires { get; set; }
        }

        public class TelerikReportCategory
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public class TelerikReportInfo
        {
            public string Id { get; set; }
            public string CategoryId { get; set; }
            public object Description { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
            public string LockedBy { get; set; }
            public string Extension { get; set; }
            public bool IsDraft { get; set; }
            public bool IsFavorite { get; set; }
            public string LastRevisionId { get; set; }
            public string CreatedByName { get; set; }
            public string LockedByName { get; set; }
            public string LastModifiedDate { get; set; }
            public DateTime LastModifiedDateUtc { get; set; }
            public bool CanEdit { get; set; }
            public bool CanView { get; set; }
            public List<TelerikReportParameter> Parameters { get; set; }
        }

        public class TelerikReportParameter
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public object Value { get; set; }
            public bool Mergeable { get; set; }
            public string Text { get; set; }
            public bool Visible { get; set; }
            public bool MultiValue { get; set; }
            public bool AllowNull { get; set; }
            public bool AllowBlank { get; set; }
            public bool AutoRefresh { get; set; }
        }

        public class Token
        {
            public string token { get; set; }
        }

    }
}
