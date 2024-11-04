using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Report.Server.Workers;
using Reporting.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ServiceStack.Redis;

var builder = WebApplication.CreateBuilder(args);


// 添加服务到容器
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// 读取配置
var reportsStorageSettings = builder.Configuration.GetSection("ReportsStorage").Get<ReportsStorageSettings>();
var redisOptions = builder.Configuration.GetSection("Redis").Get<RedisOptions>();
var telerikReportOptions = builder.Configuration.GetSection("TelerikReportOptions").Get<TelerikReportOptions>();

// 注入配置
builder.Services.AddSingleton(reportsStorageSettings);
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<TelerikReportOptions>(builder.Configuration.GetSection("TelerikReportOptions"));

// 注入服务

builder.Services.AddSingleton<TelerikReportServerClient>();

// 添加后台服务
builder.Services.AddHostedService<ReportsHostedService>();

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); 
app.UseAuthorization();
app.MapControllers();

app.Run();

// 配置类定义
public class ReportsStorageSettings
{
    public string FolderPath { get; set; }
}

public class RedisOptions
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Password { get; set; }

    public string ConnectionString => $"{Host}:{Port},password={Password}";
}

public class TelerikReportOptions
{
    public string BaseUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public RedisOptions RedisOptions { get; set; } // 属性名称改为大写
}
public class UserCredentials
{
    public string Username { get; set; }
    public string Password { get; set; }
}
