

namespace Reporting.Server.Services
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

    public class TelerikReportServerClient
    {
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly RedisClient _redisClient;

        public TelerikReportServerClient(IOptions<TelerikReportOptions> options)
        {
            _baseUrl = options.Value.BaseUrl;
            _username = options.Value.Username;
            _password = options.Value.Password;
            var redisOptions = options.Value.RedisOptions;
            _redisClient = new RedisClient(redisOptions.Host, redisOptions.Port, redisOptions.Password);
        }

        public async Task<string> GetTokenAsync()
        {
            var token = new TelerikUserToken();
            try
            {
                token = await _baseUrl.AppendPathSegment("token")
                    .PostUrlEncodedAsync(new Dictionary<string, string>()
                    {
                        {"grant_type", "password"},
                        {"username", _username},
                        {"password", _password}
                    }).Result.GetJsonAsync<TelerikUserToken>();

                string Token = token.access_token;
                return Token;
            }
            catch (FlurlHttpException ex)
            {
                string Token = token.access_token;
                SentrySdk.CaptureException(ex);
                return Token;
            }
        }

        public async Task<IEnumerable<TelerikReportCategory>> GetReportCategoriesAsync(string token)
        {
            var result = await _baseUrl
                .AppendPathSegment("api/reportserver/v2/categories")
                .WithOAuthBearerToken(token)
                .GetJsonAsync<IEnumerable<TelerikReportCategory>>();
            await SaveCategoriesAsync(result, token);
            return result;
        }

        public async Task<IEnumerable<TelerikReportInfo>> GetReportInfosByCategoryIdAsync(string token, string categoryId)
        {
            var result = await _baseUrl
                .AppendPathSegment($"api/reportserver/v2/categories/{categoryId}/reports")
                .WithOAuthBearerToken(token)
                .GetJsonAsync<IEnumerable<TelerikReportInfo>>();
            await SaveReportsAsync(result, token);
            return result;
        }

        public async Task<IEnumerable<TelerikReportInfo>> ReportInfosByCategoryIdAsync(string token, string categoryId)
        {
            return await _baseUrl
                .WithOAuthBearerToken(token)
                .AppendPathSegment($"api/reportserver/v2/categories/{categoryId}/reports")
                .GetJsonAsync<IEnumerable<TelerikReportInfo>>();
        }

        public async Task<TelerikReportDefinition> GetReportLatestRevisionAsync(string token, string reportId)
        {
            return await _baseUrl
                .AppendPathSegment($"api/reportserver/v2/reports/{reportId}/revisions/latest")
                .WithOAuthBearerToken(token)
                .GetJsonAsync<TelerikReportDefinition>();
        }

        public async Task<IEnumerable<TelerikReportParameter>> GetReportParameters(string token, string reportId)
        {
            var result = await _baseUrl
                .AppendPathSegment($"api/reportserver/v2/reports/{reportId}/parameters")
                .WithOAuthBearerToken(token)
                .GetJsonAsync<IEnumerable<TelerikReportParameter>>();
            await SaveParametersAsync(result);
            return result;
        }
        public async Task<IEnumerable<TelerikReportParameter>> ReportParameters(string token, string reportId)
        {
            var result = await _baseUrl
                .AppendPathSegment($"api/reportserver/v2/reports/{reportId}/parameters")
                .WithOAuthBearerToken(token)
                .GetJsonAsync<IEnumerable<TelerikReportParameter>>();
            return result;
        }

        public async Task SaveParametersAsync(IEnumerable<TelerikReportParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                var filePath = Path.Combine("C://Reports", parameter.Name);
                var parameterJson = JsonConvert.SerializeObject(parameter);
                await File.WriteAllTextAsync(filePath, parameterJson);

                _redisClient.SetValue($"parameter:{parameter.Name}", parameterJson);
            }
        }

        public async Task SaveCategoriesAsync(IEnumerable<TelerikReportCategory> categories, string token)
        {

            //await SaveToLocalFile("C://Reports/categories.json", categoriesJson);


            //await db.StringSetAsync("categories", categoriesJson);

            foreach (var category in categories)
            {

                var reportInfos = await ReportInfosByCategoryIdAsync(token, category.Id);
                var reportsJson = JsonConvert.SerializeObject(reportInfos);

                var reports = JsonConvert.DeserializeObject<List<TelerikReportInfo>>(reportsJson);

                List<TelerikReportInfo> reportList = new List<TelerikReportInfo>();
                foreach (TelerikReportInfo report in reports)
                {
                    var parameters = await ReportParameters(token, report.Id);
                    var parameterListsJson = JsonConvert.SerializeObject(parameters);
                    var parameterList = JsonConvert.DeserializeObject<List<TelerikReportParameter>>(parameterListsJson);

                    var reportsWithParameters = new TelerikReportInfo
                    {
                        Id = report.Id,
                        Name = report.Name,
                        CategoryId = report.CategoryId,
                        Description = report.Description,
                        CreatedBy = report.CreatedBy,
                        LockedBy = report.LockedBy,
                        Extension = report.Extension,
                        IsDraft = report.IsDraft,
                        IsFavorite = report.IsFavorite,
                        LastRevisionId = report.LastRevisionId,
                        CreatedByName = report.CreatedByName,
                        LockedByName = report.CreatedByName,
                        LastModifiedDate = report.LastModifiedDate,
                        LastModifiedDateUtc = report.LastModifiedDateUtc,
                        CanEdit = report.CanEdit,
                        CanView = report.CanView,
                        Parameters = parameterList
                    };
                    reportList.Add(reportsWithParameters);
                }

                var categoryWithReports = new
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Reports = reportList
                };


                var categoryJson = JsonConvert.SerializeObject(categoryWithReports, Formatting.Indented);


                _redisClient.SetValue($"{category.Id}", categoryJson); // 保存到 Redis
                var reportFileName = $"{category.Name}.json";

                var reportFilePath = Path.Combine("C://Reports", reportFileName);

                await File.WriteAllTextAsync(reportFilePath, categoryJson);
            }

        }

        public async Task SaveReportsAsync(IEnumerable<TelerikReportInfo> reports, string token)
        {

            List<TelerikReportInfo> reportList = new List<TelerikReportInfo>();

            foreach (var report in reports)
            {
                var parameters = await ReportParameters(token, report.Id);
                var parameterListsJson = JsonConvert.SerializeObject(parameters);
                var parameterList = JsonConvert.DeserializeObject<List<TelerikReportParameter>>(parameterListsJson);

                var reportsWithParameters = new TelerikReportInfo
                {
                    Id = report.Id,
                    Name = report.Name,
                    CategoryId = report.CategoryId,
                    Description = report.Description,
                    CreatedBy = report.CreatedBy,
                    LockedBy = report.LockedBy,
                    Extension = report.Extension,
                    IsDraft = report.IsDraft,
                    IsFavorite = report.IsFavorite,
                    LastRevisionId = report.LastRevisionId,
                    CreatedByName = report.CreatedByName,
                    LockedByName = report.CreatedByName,
                    LastModifiedDate = report.LastModifiedDate,
                    LastModifiedDateUtc = report.LastModifiedDateUtc,
                    CanEdit = report.CanEdit,
                    CanView = report.CanView,
                    Parameters = parameterList
                };
                reportList.Add(reportsWithParameters);


            }


            foreach (var report in reportList)
            {
                var reportJson = JsonConvert.SerializeObject(report, Formatting.Indented);
                _redisClient.SetValue($"{report.Id}", reportJson); // 保存到 Redis
                var reportFileName = $"{report.Name}.json";

                var reportFilePath = Path.Combine("C://Reports", reportFileName);

                await File.WriteAllTextAsync(reportFilePath, reportJson);
            }
        }

        //public async Task SaveToLocalFile(string filePath, string content)
        //{
        //    using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
        //    {
        //        await writer.WriteAsync(content);
        //    }
        //}

        //public void SaveReportToLocal(TelerikReportDefinition reportDefinition, string reportId)
        //{
        //    var filePath = Path.Combine("C://Reports", reportId + reportDefinition.Extension);
        //    File.WriteAllBytes(filePath, reportDefinition.Content);
        //}

        //    public async Task SaveReportToRedis(TelerikReportDefinition reportDefinition, string reportId)
        //    {
        //        var reportJson = JsonConvert.SerializeObject(reportDefinition);

        //        _redisClient.SetValue($"{reportId.Id}", reportJson); // 保存到 Redis
        //    }

        //}


        public class TelerikReportsResponse
        {
            public List<TelerikReportInfo> ReportInfos { get; set; } = new List<TelerikReportInfo>();
            public List<string> Errors { get; set; } = new List<string>();
        }

        public class TelerikCategoriesResponse
        {
            public List<TelerikReportCategory> Categories { get; set; } = new List<TelerikReportCategory>();
            public List<string> Errors { get; set; } = new List<string>();
        }


        public class TelerikUserToken
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
            public string userName { get; set; }

            [JsonProperty(".issued")]
            public string Issued { get; set; }

            [JsonProperty(".expires")]
            public string Expires { get; set; }
        }
        public class Token
        {
            public string token { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class TelerikReportCategory
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public class TelerikReportDefinition
        {
            public string Id { get; set; }
            public byte[] Content { get; set; }
            public string Extension { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
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




    }
}