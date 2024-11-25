using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
using Microsoft.Extensions.DependencyInjection;

using Telerik.Reporting.Services;
using Telerik.Reporting.Services.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ServiceStack.Redis;
using Telerik.Reporting.Cache.Interfaces;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;
using Telerik.Reporting.Cache.StackExchangeRedis;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(o =>
{
    o.Dsn = "https://c2c0c0e1939d1af158ac0b9ae7b9da68@o102090.ingest.us.sentry.io/4508333770014720";
    o.Debug = false;
    o.TracesSampleRate = 1.0;
    o.ProfilesSampleRate = 1.0;
    o.AddIntegration(new ProfilingIntegration(TimeSpan.FromMilliseconds(500)));
});

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
builder.Services.AddSingleton<RedisService>();
builder.Services.AddSingleton<TelerikReportService>();
builder.Services.AddHostedService<ReportsHostedService>();


builder.Services.AddSingleton<IReportServiceConfiguration>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    var redisOptions = new ConfigurationOptions
    {
        EndPoints = { "101.100.172.172:6379" },
        Password = "Bi35K0yqV9XmQtvL08S434QMCHthnt9e",
        DefaultDatabase = 0,            
        AbortOnConnectFail = false       
    };

    var redisConnection = ConnectionMultiplexer.Connect(redisOptions);

    return new ReportServiceConfiguration
    {
        Storage = new RedisStorage(redisConnection, "report-cache:"),
        ReportSourceResolver = new UriReportSourceResolver(
            Path.Combine(env.ContentRootPath, "reports"))

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

builder.Services.AddLogging(); // 如果有日志依赖
builder.Logging.AddDebug();
builder.Logging.AddConsole();




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

