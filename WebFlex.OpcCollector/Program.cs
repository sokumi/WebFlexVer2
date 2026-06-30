using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebFlex.OpcCollector;
using WebFlex.OpcCollector.Data;
using WebFlex.OpcCollector.Logging;
using WebFlex.OpcCollector.Options;
using WebFlex.OpcCollector.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options => {
    options.ServiceName = "WebFlex OPC Collector";
});

builder.Logging.AddProvider(new MemoryLoggerProvider());

builder.Services.Configure<OpcCollectorOptions>(
    builder.Configuration.GetSection("OpcCollector"));

builder.Services.AddSingleton<OpcCollectorOptionState>();

builder.Services.AddDbContextFactory<WebFlexConfigDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebFlexDb"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<OpcCollectTargetProvider>();
builder.Services.AddSingleton<OpcClientOptionState>();
builder.Services.AddSingleton<TimescaleDbWriter>();
builder.Services.AddSingleton<OpcExpressionEvaluator>();
builder.Services.AddSingleton<OpcUaSessionFactory>();
builder.Services.AddSingleton<OpcUaRuntimeService>();
builder.Services.AddSingleton<OpcRuntimeStatusService>();
builder.Services.AddSingleton<OpcRuntimeManager>();
builder.Services.AddSingleton<OpcHistoryReadService>();

builder.Services.AddHostedService<Worker>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtSecretKey)) {
    throw new InvalidOperationException("Jwt 설정이 없습니다.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.RequireHttpsMetadata = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

await app.Services.GetRequiredService<OpcCollectorOptionState>().LoadAsync();
await app.Services.GetRequiredService<OpcClientOptionState>().LoadAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();