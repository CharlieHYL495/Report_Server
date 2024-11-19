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
builder.Services.AddControllers().AddNewtonsoftJson();

// 加载配置
builder.Services.Configure<TelerikReportOptions>(builder.Configuration.GetSection("TelerikReportOptions"));
builder.Services.Configure<RedisKeysOptions>(builder.Configuration.GetSection("RedisKeys"));
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return !apiDesc.ActionDescriptor.DisplayName.Contains("GetResource");
    });
});
builder.Services.AddControllers();

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

