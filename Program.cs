using MACSAPI.Middlewares;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Serilog;
using MACSAPI.Filters;
using Microsoft.OpenApi.Models;

var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Logs");

// T?o thý m?c n?u chýa t?n t?i
if (!Directory.Exists(logsPath))
{
    Directory.CreateDirectory(logsPath);
}
// T?o logger Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        path: Path.Combine(logsPath, "application-log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7, // Ghi log 7 ngày
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning 
    )
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("UploadContext")) // Ch? log upload
        .WriteTo.File(
            path: Path.Combine(logsPath, "upload-log-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "\n---\n{Timestamp:yyyy-MM-dd HH:mm:ss}\n[{Level:u3}] {Message:lj}\n{NewLine}{Exception}"
        )
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// C?u h?nh JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = jwtSettings["Key"];
if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
{
    throw new Exception("JWT Key must be at least 32 characters long.");
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    // C?u h?nh ð? h? tr? file upload
    c.OperationFilter<FileUploadOperationFilter>();
});


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"]
    };
});

// Thêm d?ch v? CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50 MB
});

// Tích h?p Serilog
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<JwtMiddleware>(key);

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
