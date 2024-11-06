using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using ServiceStack.Redis;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Report.Server.Workers;
using Reporting.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Report.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// 从配置文件加载 JWT、Redis 和 Telerik 配置
var jwtSettings = builder.Configuration.GetSection("Jwt");
var redisConfig = builder.Configuration.GetSection("Redis");
var reportsStorageSettings = builder.Configuration.GetSection("ReportsStorage").Get<ReportsStorageSettings>();
var telerikReportOptions = builder.Configuration.GetSection("TelerikReportOptions").Get<TelerikReportOptions>();
var timerInterval = builder.Configuration.GetValue<int>("TimerInterval");
var maximumOrderWorkers = builder.Configuration.GetValue<int>("MaximumOrderWorkers");

 //配置 JWT 认证
var key = jwtSettings["Key"];
var issuer = jwtSettings["Issuer"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

// 注入配置
builder.Services.Configure<TelerikReportOptions>(builder.Configuration.GetSection("TelerikReportOptions"));
builder.Services.AddSingleton(reportsStorageSettings);

// 配置 Redis 客户端
var redisConnectionString = builder.Configuration["RedisConnString"];
builder.Services.AddSingleton<IRedisClientsManager>(c => new RedisManagerPool(redisConnectionString));
builder.Services.AddScoped<RedisService>();

// 注入其他服务
builder.Services.AddScoped<TelerikReportService>();
builder.Services.AddHostedService<ReportsHostedService>();

// 如果 TimerInterval 和 MaximumOrderWorkers 被用作后台任务配置，注入它们
builder.Services.AddSingleton(new TimerIntervalSettings { Interval = timerInterval });
builder.Services.AddSingleton(new WorkerSettings { MaximumOrderWorkers = maximumOrderWorkers });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// 构建应用
var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();

// 启用认证和授权
app.UseAuthentication();
app.UseAuthorization();


// 映射控制器
app.MapControllers();

// 启动应用
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
}

public class TimerIntervalSettings
{
    public int Interval { get; set; }
}

public class WorkerSettings
{
    public int MaximumOrderWorkers { get; set; }
}



