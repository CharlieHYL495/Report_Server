using Report.Server.Workers;
using Reporting.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Reporting.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// 读取配置
var reportingSettings = builder.Configuration.GetSection("ReportServer").Get<ReportingSettings>();
var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();
var credentialsSettings = builder.Configuration.GetSection("Credentials").Get<CredentialsSettings>();
var reportsStorageSettings = builder.Configuration.GetSection("ReportsStorage").Get<ReportsStorageSettings>();

// 注入配置
builder.Services.AddSingleton(reportingSettings);
builder.Services.AddSingleton(redisSettings);
builder.Services.AddSingleton(credentialsSettings);
builder.Services.AddSingleton(reportsStorageSettings);
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");

// 注册 RedisService，并传递连接字符串
builder.Services.AddSingleton<RedisService>(provider => new RedisService(redisConnectionString));

//builder.Services.AddSingleton<ReportBackgroundService>();
//builder.Services.AddSingleton<TelerikBackgroundService>();System.ArgumentNullException: 'Value cannot be null. Arg_ParamName_Name'

//builder.Services.AddSingleton<TokenService>(provider =>
//    new TokenService(
//        reportingSettings.BaseUrl, 
//        credentialsSettings.Username,
//        credentialsSettings.Password));
//builder.Services.Configure<DataFetchOptions>(builder.Configuration.GetSection("DataFetch"));
builder.Services.AddHostedService<ReportsHostedService>();



// 注入 TelerikService中TelerikReportServerClient类
builder.Services.AddSingleton<TelerikReportServerClient>(provider =>
    new TelerikReportServerClient(
        reportingSettings.BaseUrl,
        credentialsSettings.Username,
        credentialsSettings.Password,
        $"{redisSettings.Host}:{redisSettings.Port},password={redisSettings.Password}"
    ));

builder.Services.AddControllers();






var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
// 中间件配置
public class ReportingSettings
{
    public string BaseUrl { get; set; }
    public string TokenEndpoint { get; set; }
}

public class RedisSettings
{
    public string Host { get; set; }
    public string Port { get; set; }
    public string Password { get; set; }
    public string Period { get; set; }
}

public class CredentialsSettings
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class ReportsStorageSettings
{
    public string FolderPath { get; set; }
}

