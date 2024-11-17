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
        private readonly string _redisKeyPrefix;
        private readonly string _reportDefinitionPath;
        private readonly ILogger<TelerikReportService> _logger;
        private readonly IRedisClientsManager _redisClientsManager;

        public TelerikReportService(IOptions<TelerikReportOptions> telerikOptions,
            IOptions<RedisKeysOptions> redisOptions,
            IRedisClientsManager redisClientsManager,
            ILogger<TelerikReportService> logger)
        {
            _baseUrl = telerikOptions.Value.BaseUrl;
            _username = telerikOptions.Value.Username;
            _password = telerikOptions.Value.Password;
            _tokenPath = telerikOptions.Value.TokenPath;
            _categoriesPath = telerikOptions.Value.CategoriesPath;
            _reportByCategoryPath = telerikOptions.Value.ReportsByCategoryPath;
            _parametersPath = telerikOptions.Value.ParametersPath;
            _reportDefinitionPath = telerikOptions.Value.ReportLatestPath;
            _redisKeyPrefix = redisOptions.Value.RedisKeyPrefix;
            _redisClientsManager = redisClientsManager; 
            _logger = logger;
        }

        // Save reports to local files
        //public async Task SaveReportsToLocalFilesAsync(string token)
        //{
        //    var categories = await GetCategoriesAsync(token);

        //    var tasks = categories.Select(async category =>
        //    {
        //        var reportList = await GetReportListWithParametersAsync(token, category.Id);
        //        await SaveReportsToLocalAsync(token, reportList);
        //    });

        //    await Task.WhenAll(tasks);
        //}
        public async Task SaveReportsToLocalFilesAsync(string token)
        {
            try
            {
                var categories = await GetCategoriesAsync(token);

                var tasks = categories.Select(async category =>
                {
                    try
                    {
                        var reportList = await GetReportListWithParametersAsync(token, category.Id);
                        await SaveReportsToLocalAsync(token, reportList);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error occurred while processing category {category.Id}: {category.Name}");
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving reports to local files.");
            }
        }

        // Method Logic to save reports to local files
        //private async Task SaveReportsToLocalAsync(string token, List<TelerikReportInfo> reportList)
        //{
        //    var saveTasks = reportList.Select(async report =>
        //    {
        //        var reportDefinition = await GetReportLatestRevisionAsync(token, report.Id);
        //        var filePath = Server.Location.ReportPath($"{report.Name}.{reportDefinition.Extension}");
        //        await File.WriteAllBytesAsync(filePath, reportDefinition.Content);
        //    });

        //    await Task.WhenAll(saveTasks);
        //}
        private async Task SaveReportsToLocalAsync(string token, List<TelerikReportInfo> reportList)
        {
            try
            {
                var saveTasks = reportList.Select(async report =>
                {
                    try
                    {
                        var reportDefinition = await GetReportLatestRevisionAsync(token, report.Id);
                        var filePath = Server.Location.ReportPath($"{report.Name}{reportDefinition.Extension}");

                        await File.WriteAllBytesAsync(filePath, reportDefinition.Content);
                        _logger.LogInformation($"Report saved successfully: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error occurred while saving report {report.Id}: {report.Name}");
                    }
                });

                await Task.WhenAll(saveTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving reports to local.");
            }
        }


        // Save category data to Redis
        public async Task SaveCategoryToRedisAsync(string token)
        {
            var categories = await GetCategoriesAsync(token);
            using var redisClient = _redisClientsManager.GetClient();

            var tasks = categories.Select(async category =>
            {
                var reportList = await GetReportListWithParametersAsync(token, category.Id);
                var categoryWithReports = new
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Reports = reportList
                };

                SaveToRedis(redisClient, category.Id, categoryWithReports);
            });

            await Task.WhenAll(tasks);
        }
        // Method logic to save category data to Redis
        private void SaveToRedis(IRedisClient redisClient, string categoryId, object categoryWithReports)
        {
            var categoryJson = JsonConvert.SerializeObject(categoryWithReports, Formatting.Indented);
            redisClient.SetValue($"{_redisKeyPrefix}{categoryId}", categoryJson);
        }
        //Get token
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
        // Get report categories 
        private async Task<IEnumerable<TelerikReportCategory>> GetCategoriesAsync(string token)
        {
            var result = await _baseUrl
                .AppendPathSegment(_categoriesPath)
                .WithOAuthBearerToken(token)
                .GetJsonAsync<IEnumerable<TelerikReportCategory>>();
            return result;
        }
        // get report list with parameters
        private async Task<List<TelerikReportInfo>> GetReportListWithParametersAsync(string token, string categoryId)
        {
            var reports = await GetReportInfosByCategoryIdAsync(token, categoryId);
            var reportTasks = reports.Select(async report =>
            {
                var parameters = await GetReportParametersAsync(token, report.Id);
                report.Parameters = parameters.ToList();
                return report;
            });

            return (await Task.WhenAll(reportTasks)).ToList();
        }

        //Get latest report definition
        private async Task<TelerikReportDefinition> GetReportLatestRevisionAsync(string token, string reportId)
        {
            return await _baseUrl
                .AppendPathSegment(string.Format(_reportDefinitionPath,reportId))
                .WithOAuthBearerToken(token)
                .GetJsonAsync<TelerikReportDefinition>();
        }
        // Get reportInfos by category
        private async Task<IEnumerable<TelerikReportInfo>> GetReportInfosByCategoryIdAsync(string token, string categoryId)
        {
            return await _baseUrl
                .WithOAuthBearerToken(token)
                .AppendPathSegment(string.Format(_reportByCategoryPath, categoryId))
                .GetJsonAsync<IEnumerable<TelerikReportInfo>>();
        }
        // Get parameterInfos for each report
        private async Task<IEnumerable<TelerikReportParameter>> GetReportParametersAsync(string token, string reportId)
        {
            return await _baseUrl
                .AppendPathSegment(string.Format(_parametersPath, reportId))
                .WithOAuthBearerToken(token)
                .GetJsonAsync<IEnumerable<TelerikReportParameter>>();

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

        private class TelerikReportInfo
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

        private class TelerikReportParameter
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
        private class TelerikReportDefinition
        {
            public string Id { get; set; }
            public byte[] Content { get; set; }
            public string Extension { get; set; }
        }

    }
}
