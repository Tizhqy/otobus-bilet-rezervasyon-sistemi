using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using OtobusBiletRezervasyon;
using OtobusBiletRezervasyon.Middleware;
using OtobusBiletRezervasyon.Services;
using OtobusBiletRezervasyon.Services.Interfaces;
using OtobusBiletRezervasyon.Repositories;
using OtobusBiletRezervasyon.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

static int? ResolveHttpsPort(IConfiguration configuration)
{
    var explicitPort =
        configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT") ??
        configuration.GetValue<int?>("HTTPS_PORT");

    if (explicitPort.HasValue)
    {
        return explicitPort.Value;
    }

    var configuredUrls = configuration["ASPNETCORE_URLS"] ?? configuration["urls"];
    if (!string.IsNullOrWhiteSpace(configuredUrls))
    {
        foreach (var rawUrl in configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) &&
                uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return uri.Port;
            }
        }
    }

    var kestrelHttpsUrl = configuration["Kestrel:Endpoints:Https:Url"];
    if (!string.IsNullOrWhiteSpace(kestrelHttpsUrl) &&
        Uri.TryCreate(kestrelHttpsUrl, UriKind.Absolute, out var kestrelUri) &&
        kestrelUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return kestrelUri.Port;
    }

    return null;
}

var httpsPort = ResolveHttpsPort(builder.Configuration);

// ── Veritabani ──────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");

var connStrBuilder = new MySqlConnectionStringBuilder(connStr);
if (string.IsNullOrWhiteSpace(connStrBuilder.Password))
{
    var configuredDbPassword = builder.Configuration["Database:Password"]
        ?? builder.Configuration["MYSQL_PASSWORD"];

    if (!string.IsNullOrWhiteSpace(configuredDbPassword))
    {
        connStrBuilder.Password = configuredDbPassword;
        connStr = connStrBuilder.ConnectionString;
    }
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

// ── JWT Ayarlari ────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured in appsettings.json");
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "OtobusBiletRezervasyon";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "OtobusBiletRezervasyon";

// ── Auth ────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(opt =>
{
    opt.LoginPath = "/Auth/Giris";
    opt.LogoutPath = "/Auth/Cikis";
    opt.AccessDeniedPath = "/Auth/Giris";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opt.Cookie.SameSite = SameSiteMode.Strict;
    opt.SlidingExpiration = true;
    opt.ExpireTimeSpan = TimeSpan.FromHours(8);
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.Claims.Any(c =>
                c.Type == ClaimTypes.Role &&
                c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase))));
});

// ── Repository Kayitlari ─────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IDepartureRepository, DepartureRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<ITemporaryReservationRepository, TemporaryReservationRepository>();

// ── Service Kayitlari ────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IBiletFlowService, BiletFlowService>();
builder.Services.AddScoped<IOdemeFlowService, OdemeFlowService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<ISeferFlowService, SeferFlowService>();
builder.Services.AddScoped<IAdminFlowService, AdminFlowService>();
builder.Services.AddScoped<IReservationFlowService, ReservationFlowService>();
builder.Services.AddScoped<IPdfTicketService, PdfTicketService>();

// ── Background Services ──────────────────────────────────────────────────────
builder.Services.AddHostedService<OtobusBiletRezervasyon.Services.BackgroundJobs.ReservationCleanupService>();

// ── Diger Servisler ──────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddControllersWithViews(opt =>
{
    opt.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.AddHttpsRedirection(options =>
{
    if (httpsPort.HasValue)
    {
        options.HttpsPort = httpsPort.Value;
    }
});
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("StationSearchCache", policy =>
    {
        policy.Expire(TimeSpan.FromMinutes(2));
        policy.SetVaryByQuery("query");
        policy.With(context => context.HttpContext.User.Identity?.IsAuthenticated != true);
    });

    options.AddPolicy("StationListCache", policy =>
    {
        policy.Expire(TimeSpan.FromMinutes(10));
        policy.With(context => context.HttpContext.User.Identity?.IsAuthenticated != true);
    });
});

// ── Anti-forgery ─────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery(opt =>
{
    opt.HeaderName = "X-CSRF-TOKEN";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    var authLoginPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:AuthLogin:PermitLimit") ?? 10;
    var authLoginWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:AuthLogin:WindowSeconds") ?? 60;
    var passwordResetPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:PasswordReset:PermitLimit") ?? 3;
    var passwordResetWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:PasswordReset:WindowSeconds") ?? 600;
    var passwordResetConfirmPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:PasswordResetConfirm:PermitLimit") ?? 8;
    var passwordResetConfirmWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:PasswordResetConfirm:WindowSeconds") ?? 300;
    var passwordChangePermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:PasswordChange:PermitLimit") ?? 5;
    var passwordChangeWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:PasswordChange:WindowSeconds") ?? 300;
    var adminLogCleanupPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:AdminLogCleanup:PermitLimit") ?? 2;
    var adminLogCleanupWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:AdminLogCleanup:WindowSeconds") ?? 300;

    options.AddPolicy("AuthLoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authLoginPermitLimit,
                Window = TimeSpan.FromSeconds(authLoginWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("PasswordResetPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = passwordResetPermitLimit,
                Window = TimeSpan.FromSeconds(passwordResetWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("PasswordResetConfirmPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = passwordResetConfirmPermitLimit,
                Window = TimeSpan.FromSeconds(passwordResetConfirmWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("PasswordChangePolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = passwordChangePermitLimit,
                Window = TimeSpan.FromSeconds(passwordChangeWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("AdminLogCleanupPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown"}:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = adminLogCleanupPermitLimit,
                Window = TimeSpan.FromSeconds(adminLogCleanupWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Cok fazla istek gonderdiniz. Lutfen biraz sonra tekrar deneyin.", token);
    };
});

// ── QuestPDF License ──────────────────────────────────────────────────────────
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment() && httpsPort.HasValue)
{
    app.UseHttpsRedirection();
}
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();

// Security Headers
app.Use(async (ctx, next) =>
{
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    ctx.Items["CspNonce"] = nonce;

    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    var imgSrc = app.Environment.IsDevelopment()
        ? "img-src 'self' data: https: http:; "
        : "img-src 'self' data: https:; ";
    var connectSrc = app.Environment.IsDevelopment()
        ? "connect-src 'self' https: http: ws: wss:; "
        : "connect-src 'self' https: wss:; ";

    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        $"script-src 'self' 'nonce-{nonce}' cdn.jsdelivr.net unpkg.com; " +
        "style-src 'self' 'unsafe-inline' fonts.googleapis.com cdn.jsdelivr.net unpkg.com; " +
        "font-src 'self' fonts.gstatic.com; " +
        imgSrc +
        connectSrc +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'none';";
    await next();
});

app.UseRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// ── Routing ────────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Dashboard}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Sefer}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

// ── DB Migration & Seed ──────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db);
}

app.Run();
