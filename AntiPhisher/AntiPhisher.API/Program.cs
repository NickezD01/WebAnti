using AntiPhisher.API.Middleware;
using AntiPhisher.Application;
using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.MyMapper;
using AntiPhisher.Application.Services;
using AntiPhisher.Domain;
using AntiPhisher.Infrastructure;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// CONFIGURATION
// ======================================================
var configuration = builder.Configuration.Get<AppSetting>();
builder.Services.AddSingleton(configuration!);

// ======================================================
// CONTROLLERS
// ======================================================
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddFluentValidationAutoValidation();

// ======================================================
// DATABASE
// ======================================================
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        configuration!.ConnectionStrings.DefaultConnection
    );

    options.ConfigureWarnings(warnings =>
        warnings.Ignore(
            CoreEventId.NavigationBaseIncludeIgnored
        ));
});

// ======================================================
// JWT
// ======================================================
var secretValue = configuration?.SecretToken?.Value;
if (string.IsNullOrWhiteSpace(secretValue))
{
    throw new Exception(
        "SecretToken:Value missing in appsettings.json"
    );
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secretValue)
                    ),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
    });

// ======================================================
// SWAGGER
// ======================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "AntiPhisher API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT Token"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// ======================================================
// HTTP CONTEXT
// ======================================================
builder.Services.AddHttpContextAccessor();

// ======================================================
// UNIT OF WORK
// ======================================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ======================================================
// SERVICES
// ======================================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();

// Tích hợp HttpClient chuyên dụng cho ScenarioService để gọi OpenAI API an toàn
builder.Services.AddHttpClient<IScenarioService, ScenarioService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();

// ĐĂNG KÝ MỚI: Kích hoạt LessonService vào DI Container để sửa lỗi sập Controller
builder.Services.AddScoped<ILessonService, LessonService>();

// ======================================================
// AUTOMAPPER
// ======================================================
builder.Services.AddAutoMapper(
    typeof(MapperConfigurationsProfile).Assembly
);

// ======================================================
// CORS
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// ======================================================
// APP
// ======================================================
var app = builder.Build();

// ======================================================
// MIDDLEWARE
// ======================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<ValidationMiddleware>();

app.MapControllers();
app.Run();