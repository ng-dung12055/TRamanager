using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using TRManager.Api.Data;
using TRManager.Api.Data.Entities;
using TRManager.Api.Features.Auth;
using TRManager.Api.Features.Auth.Validation;
using TRManager.Api.Features.User;

// Alias entity để tránh trùng namespace
using AppUser = TRManager.Api.Data.Entities.User;

// Alias type để chắc chắn đúng kiểu/namespace
using IJwtGen = TRManager.Api.Features.Auth.IJwtTokenGenerator;
using JwtGen = TRManager.Api.Features.Auth.JwtTokenGenerator;

var builder = WebApplication.CreateBuilder(args);

// ================== CẤU HÌNH DỊCH VỤ ==================
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordRequestValidator>();

// Swagger + JWT Security
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TRManager API",
        Version = "v1",
        Description = "API quản lý nhà trọ (.NET 9 + EF Core + JWT)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token theo dạng: Bearer {your JWT token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Kết nối DB
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Config
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["SecretKey"]!);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ========== ĐĂNG KÝ SERVICE CHO DEPENDENCY INJECTION ==========
// Dùng alias kiểu đầy đủ để loại trừ mọi xung đột namespace
builder.Services.AddScoped<IJwtGen, JwtGen>(); // ✅ ĐĂNG KÝ IJwtTokenGenerator
builder.Services.AddScoped<TRManager.Api.Features.Auth.IAuthService,
                           TRManager.Api.Features.Auth.AuthService>();
builder.Services.AddScoped<TRManager.Api.Features.User.IUserService,
                           TRManager.Api.Features.User.UserService>();

builder.Services.AddScoped<
    Microsoft.AspNetCore.Identity.IPasswordHasher<AppUser>,
    Microsoft.AspNetCore.Identity.PasswordHasher<AppUser>>();

// (DEBUG) In ra để tự kiểm tra đã đăng ký JwtTokenGenerator chưa
foreach (var s in builder.Services)
{
    if (s.ServiceType.FullName?.Contains("IJwtTokenGenerator") == true)
        Console.WriteLine($"DI: {s.ServiceType.FullName} -> {s.ImplementationType?.FullName} ({s.Lifetime})");
}

// Cấu hình Kestrel port
builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenLocalhost(5161);                      // HTTP
    o.ListenLocalhost(7183, lo => lo.UseHttps()); // HTTPS
});

// ================== XÂY DỰNG APP ==================
var app = builder.Build();

// Middlewares
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ================== SEED ADMIN ==================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
    if (adminRole == null)
    {
        adminRole = new Role { Name = "Admin" };
        db.Roles.Add(adminRole);
        await db.SaveChangesAsync();
    }

    var adminEmail = "admin@trmanager.com";
    var admin = await db.Users.Include(u => u.Roles)
                              .FirstOrDefaultAsync(u => u.Email == adminEmail);
    if (admin == null)
    {
        var hasher = scope.ServiceProvider.GetRequiredService<
            Microsoft.AspNetCore.Identity.IPasswordHasher<AppUser>>();

        var newAdmin = new AppUser
        {
            Email = adminEmail,
            UserName = "admin",
            FullName = "Administrator",
            Phone = "0900000000",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = new List<Role> { adminRole }
        };
        newAdmin.PasswordHash = hasher.HashPassword(newAdmin, "Admin@123");

        db.Users.Add(newAdmin);
        await db.SaveChangesAsync();

        Console.WriteLine("✅ Seeded admin: admin@trmanager.com / Admin@123");
    }
}

// ================== CHẠY APP ==================
app.Run();
