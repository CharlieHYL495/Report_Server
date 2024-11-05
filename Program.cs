//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;
//using Report.Server.Workers;
//using Reporting.Server.Services;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Configuration;
//using ServiceStack.Redis;
//using System.Security.Claims;
//using System.IdentityModel.Tokens.Jwt;

//var builder = WebApplication.CreateBuilder(args);


//// 添加服务到容器
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//builder.Services.AddControllers();

//// 读取配置
//var reportsStorageSettings = builder.Configuration.GetSection("ReportsStorage").Get<ReportsStorageSettings>();
//var redisOptions = builder.Configuration.GetSection("Redis").Get<RedisOptions>();
//var telerikReportOptions = builder.Configuration.GetSection("TelerikReportOptions").Get<TelerikReportOptions>();
//var redisConfig = builder.Configuration.GetSection("Redis");

//var jwtSettings = builder.Configuration.GetSection("Jwt");
//var key = jwtSettings["Key"];
//var issuer = jwtSettings["Issuer"];

//// 添加认证服务
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = false,
//        ValidateAudience = false,
//        ValidateLifetime = false,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = issuer,
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
//    };
//});

//// 注入配置
//builder.Services.AddSingleton(reportsStorageSettings);
//builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
//builder.Services.Configure<TelerikReportOptions>(builder.Configuration.GetSection("TelerikReportOptions"));
//// 从配置文件获取连接字符串
//string redisConnectionString = redisConfig["ConnectionString"];

//builder.Services.AddSingleton<IRedisClientsManager>(c =>
//    new RedisManagerPool(redisConnectionString));

//builder.Services.AddSingleton<IRedisClient>(c =>
//    c.GetRequiredService<IRedisClientsManager>().GetClient());
////builder.Services.Configure<LoginModel>(builder.Configuration.GetSection("LoginModel"));
//// 注入服务

//builder.Services.AddSingleton<TelerikReportServerClient>();

//// 添加后台服务
//builder.Services.AddHostedService<ReportsHostedService>();

//var app = builder.Build();

//// 配置 HTTP 请求管道
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//    app.UseRouting();
//    app.UseHttpsRedirection();
//    app.UseAuthentication();
//    app.UseAuthorization();
//    app.MapControllers();
//}
//// 添加控制器
////app.MapPost("api/auth/login", async (LoginModel login) =>
////{
////    // 这里应添加用户验证逻辑
////    if (login.Username == "test" && login.Password == "password") // 示例验证
////    {
////        var claims = new[]
////        {
////            new Claim(ClaimTypes.Name, login.Username)
////        };

////        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

////        var token = new JwtSecurityToken(
////            claims: claims,
////            expires: DateTime.Now.AddMinutes(30),
////            signingCredentials: creds);

////        return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
////    }

////    return Results.Unauthorized();
////});
////app.UseRouting();
////app.UseHttpsRedirection();
////app.UseAuthentication();
////app.UseAuthorization();
////app.MapControllers();

//app.Run();

//// 配置类定义
//public class ReportsStorageSettings
//{
//    public string FolderPath { get; set; }
//}

//public class RedisOptions
//{
//    public string Host { get; set; }
//    public int Port { get; set; }
//    public string Password { get; set; }

//    public string ConnectionString => $"{Host}:{Port},password={Password}";
//}

//public class TelerikReportOptions
//{
//    public string BaseUrl { get; set; }
//    public string Username { get; set; }
//    public string Password { get; set; }
//    public RedisOptions RedisOptions { get; set; } // 属性名称改为大写
//}
//public class UserCredentials
//{
//    public string Username { get; set; }
//    public string Password { get; set; }
//}
////public class LoginModel
////{
////    public string Username { get; set; }
////    public string Password { get; set; }
////}
///using Microsoft.AspNetCore.Authentication.JwtBearer;
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

var builder = WebApplication.CreateBuilder(args);

// 从配置文件加载 JWT、Redis 和 Telerik 配置
var jwtSettings = builder.Configuration.GetSection("Jwt");
var redisConfig = builder.Configuration.GetSection("Redis");
var reportsStorageSettings = builder.Configuration.GetSection("ReportsStorage").Get<ReportsStorageSettings>();
var telerikReportOptions = builder.Configuration.GetSection("TelerikReportOptions").Get<TelerikReportOptions>();
var timerInterval = builder.Configuration.GetValue<int>("TimerInterval");
var maximumOrderWorkers = builder.Configuration.GetValue<int>("MaximumOrderWorkers");

// 配置 JWT 认证
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
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<TelerikReportOptions>(builder.Configuration.GetSection("TelerikReportOptions"));
builder.Services.AddSingleton(reportsStorageSettings);

// 配置 Redis 客户端
var redisConnectionString = builder.Configuration["RedisConnString"];
builder.Services.AddSingleton<IRedisClientsManager>(c => new RedisManagerPool(redisConnectionString));
builder.Services.AddSingleton<IRedisClient>(c => c.GetRequiredService<IRedisClientsManager>().GetClient());

// 注入其他服务
builder.Services.AddScoped<TelerikReportServerClient>();
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
    public RedisOptions RedisOptions { get; set; }
}

public class TimerIntervalSettings
{
    public int Interval { get; set; }
}

public class WorkerSettings
{
    public int MaximumOrderWorkers { get; set; }
}

public class UserCredentials
{
    public string Username { get; set; }
    public string Password { get; set; }
}



