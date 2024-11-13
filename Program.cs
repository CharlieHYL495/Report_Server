﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Redis;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Report.Server.Services;
using Report.Server.Workers;

var builder = WebApplication.CreateBuilder(args);

// 加载配置
builder.Services.Configure<TelerikReportOptions>(builder.Configuration.GetSection("TelerikReportOptions"));
builder.Services.Configure<RedisKeysOptions>(builder.Configuration.GetSection("RedisKeys"));
builder.Services.Configure<ReportsStorageSettings>(builder.Configuration.GetSection("ReportsStorage"));
builder.Services.Configure<TimerIntervalSettings>(builder.Configuration.GetSection("TimerInterval"));
builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("MaximumOrderWorkers"));

// 配置JWT认证
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings["Key"];
var issuer = jwtSettings["Issuer"];
var rsa = RSA.Create();
rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(key), out _);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidIssuer = issuer
        };
    });

builder.Services.AddAuthorization();

// 配置Redis客户端
var redisConnectionString = builder.Configuration["RedisConnString"];
builder.Services.AddSingleton<IRedisClientsManager>(_ => new RedisManagerPool(redisConnectionString));

// 注册服务
builder.Services.AddScoped<RedisService>();
builder.Services.AddScoped<TelerikReportService>();
builder.Services.AddHostedService<ReportsHostedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
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

public class TelerikReportOptions
{
    public string BaseUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string CategoriesPath { get; set; }
    public string TokenPath { get; set; }
    public string ReportsByCategoryPath { get; set; }
    public string ParametersPath { get; set; }
    public string SavePath { get; set; }
}
public class RedisKeysOptions
{
    public string MerchantsKey { get; set; }
    public string RedisKeyPrefix { get; set; }
}


public class TimerIntervalSettings
{
    public int Interval { get; set; }
}

public class WorkerSettings
{
    public int MaximumOrderWorkers { get; set; }
}


//app.UseFileServer(new FileServerOptions
//{
//    FileProvider = new PhysicalFileProvider(
//        Path.Combine(AppDb.path_root, "TelerikReport", "report")),
//    RequestPath = "/report",
//    EnableDirectoryBrowsing = true,
//    StaticFileOptions =
//    {
//        ContentTypeProvider = provider,
//        OnPrepareResponse = (c) =>
//        {
//            c.Context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
//        }
//    }
//});
