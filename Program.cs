using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OtobusBiletRezervasyon;
using OtobusBiletRezervasyon.Services;
using OtobusBiletRezervasyon.Services.Interfaces;
using OtobusBiletRezervasyon.Repositories;
using OtobusBiletRezervasyon.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Veritabani ──────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

// ── JWT Ayarlari ────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? "BuCokGizliBirAnahtarEnAz32Karakter!";
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
    opt.Cookie.SameSite = SameSiteMode.Lax;
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
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "admin"));
});

// ── Repository Kayitlari ─────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IDepartureRepository, DepartureRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();

// ── Service Kayitlari ────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ── Diger Servisler ──────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(opt =>
{
    opt.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// ── Anti-forgery ─────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery(opt =>
{
    opt.HeaderName = "X-CSRF-TOKEN";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Security Headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// ── Routing ────────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Dashboard}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Sefer}/{action=Index}/{id?}");

// ── DB Migration & Seed ──────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
