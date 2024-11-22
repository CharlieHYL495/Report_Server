using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Redis;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Report.Server.Services;
using Report.Server.Workers;
using Microsoft.Extensions.FileProviders;
using Report.Server;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.SignalR;
using Telerik.Reporting.Cache.File;
using Telerik.Reporting.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 加载配置
builder.Services.Configure<TelerikReportOptions>(builder.Configuration.GetSection("TelerikReportOptions"));
builder.Services.Configure<RedisKeysOptions>(builder.Configuration.GetSection("RedisKeys"));
builder.Services.Configure<TimerIntervalSettings>(builder.Configuration.GetSection("TimerInterval"));
builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("MaximumOrderWorkers"));
builder.Services.Configure<SentryOptions>(builder.Configuration.GetSection("Sentry"));

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
builder.Services.AddSingleton<IReportServiceConfiguration>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    return new ReportServiceConfiguration
    {
        Storage = new FileStorage(),
        ReportSourceResolver = new UriReportSourceResolver(
            Path.Combine(env.ContentRootPath, "Reports"))
    };
});


builder.Services.AddCors(o => o.AddDefaultPolicy(b =>
{
    b
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithOrigins(
            "http://localhost:8080",
            "http://localhost:8100",
            "http://localhost:5173",
            "https://cloudpos.posx.ai",
            "https://xpos-app.revopos.io",
            "https://connect.revopos.io",
            "http://localhost:5050",
            "http://localhost:5005",
            "https://m.wyo.is",
            "https://uniapp.revopos.io",
            "https://wyo-crm.revopos.io",
            "https://dashboard.wyocrm.com",
            "http://localhost:3000",
            "capacitor://localhost"
        );
}));
//Sentry
builder.WebHost.UseSentry(options =>
{
    var sentryConfig = builder.Configuration.GetSection("Sentry").Get<SentryOptions>();
    options.Dsn = sentryConfig.Dsn;
    options.TracesSampleRate = sentryConfig.TracesSampleRate;
#if DEBUG
    options.Debug = sentryConfig.Debug;
#endif
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return !apiDesc.ActionDescriptor.DisplayName.Contains("GetResource");
    });
});
builder.Services.AddControllers().AddNewtonsoftJson();



var app = builder.Build();
var rootPath = AppContext.BaseDirectory;
Location.RootPath = rootPath;
var reportsPath = Path.Combine(rootPath, "reports");
if (!Directory.Exists(reportsPath)) Directory.CreateDirectory(reportsPath);
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".html"] = "text/html";
provider.Mappings[".pdf"] = "application/pdf";
provider.Mappings[".xlsx"] = "application/vnd.ms-excel";
app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(reportsPath),
    RequestPath = "/reports",
    EnableDirectoryBrowsing = true,
    StaticFileOptions =
    {
        ContentTypeProvider = provider,
        OnPrepareResponse = (c) =>
        {
            c.Context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        }
    }
});

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("Report Server Is Running...");
    });
    endpoints.MapControllers();
});
app.Run("http://*:80");



public class TelerikReportOptions
{
    public string BaseUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string CategoriesPath { get; set; }
    public string TokenPath { get; set; }
    public string ReportsByCategoryPath { get; set; }
    public string ParametersPath { get; set; }
    public string ReportLatestPath { get; set; }
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
public class SentryOptions
{
    public string Dsn { get; set; }
    public double TracesSampleRate { get; set; }
    public bool Debug { get; set; }
}

