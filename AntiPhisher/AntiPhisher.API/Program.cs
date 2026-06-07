using AntiPhisher.API.Middleware;
using AntiPhisher.Application;
using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.MyMapper;
using AntiPhisher.Application.Services;
using AntiPhisher.Application.Validation; // Import để nhận diện SubPlanValidator
using AntiPhisher.Domain;
using AntiPhisher.Infrastructure;
using FluentValidation; // Import thư viện FluentValidation
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
if (configuration != null)
{
    builder.Services.AddSingleton(configuration);
}

// ======================================================
// CONTROLLERS & VALIDATION
// ======================================================
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Kích hoạt tính năng tự động validate đầu vào của FluentValidation trên Controller
builder.Services.AddFluentValidationAutoValidation();

// Nạp toàn bộ các class Validator (bao gồm cả SubPlanValidator) trong tầng Application vào DI Container
builder.Services.AddValidatorsFromAssemblyContaining<SubPlanValidator>();

// ======================================================
// DATABASE (Đã fix lỗi Design-time Migration)
// ======================================================
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);

    options.ConfigureWarnings(warnings =>
        warnings.Ignore(CoreEventId.NavigationBaseIncludeIgnored));
});

// ======================================================
// JWT AUTHENTICATION (Đã fix cách đọc dữ liệu từ JSON)
// ======================================================
var secretValue = builder.Configuration["SecretToken:Value"];
if (string.IsNullOrWhiteSpace(secretValue))
{
    throw new Exception("SecretToken:Value is missing or invalid in appsettings.json");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretValue)),
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
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AntiPhisher API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ======================================================
// DEPENDENCY INJECTION (DI CONTAINER)
// ======================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services Registration
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();

// HttpClient cho ScenarioService (OpenAI)
builder.Services.AddHttpClient<IScenarioService, ScenarioService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<ITeamService, TeamService>();

// Đăng ký dịch vụ Company Service
builder.Services.AddScoped<ICompanyService, CompanyService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MapperConfigurationsProfile).Assembly);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ======================================================
// MIDDLEWARE PIPELINE (Thứ tự chuẩn hóa toàn hệ thống)
// ======================================================
// Luôn đặt ExceptionMiddleware lên đầu tiên để bắt mọi lỗi ngoại lệ
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// ⚡ ĐÃ FIX THỨ TỰ: Authenticate (Xác thực) -> Authorize (Phân quyền) -> Rồi mới đến Validation nội bộ
app.UseAuthentication();
app.UseAuthorization();

// Khớp dữ liệu Request đầu vào và validate sau khi danh tính User đã được xác định qua Token
app.UseMiddleware<ValidationMiddleware>();

// ======================================================
// DATABASE SEEDING (Tự động chạy khi khởi động ứng dụng)
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Khởi tạo và nạp toàn bộ cấu trúc dữ liệu mặc định + 7 file JSON kịch bản mô phỏng
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        // Log lại chi tiết lỗi hệ thống nếu quá trình Seeding thất bại
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Một lỗi nghiêm trọng đã xảy ra khi đang chạy Seeding dữ liệu kịch bản Phishing.");
    }
}

app.MapControllers();
app.Run();