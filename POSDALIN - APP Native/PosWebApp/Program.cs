using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PosWebApp.Hubs;
using PosWebApp.Infrastructure;
using PosWebApp.Infrastructure.Options;
using PosWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Secret Manager (development only) ───────────────────────────────────────
// Sensitive config values (connection strings, API keys) MUST NOT be committed
// to source control.  Use: dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
// Requirements: 25.10
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// ── Razor Pages + response compression ─────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ── Authentication (cookie) ─────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath  = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan    = TimeSpan.FromHours(
            builder.Configuration.GetValue<int>("Auth:SessionTimeoutHours", 8));
        options.Cookie.HttpOnly     = true;
        options.Cookie.SecurePolicy = builder.Environment.IsEnvironment("Testing")
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite     = builder.Environment.IsEnvironment("Testing")
            ? SameSiteMode.Lax
            : SameSiteMode.Strict;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// ── Rate limiting (brute-force protection on auth endpoints) ──────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("authLogin", httpContext =>
    {
        if (builder.Environment.IsEnvironment("Testing"))
            return RateLimitPartition.GetNoLimiter("test");

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
    });
});

// ── Session ─────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(
        builder.Configuration.GetValue<int>("Auth:SessionTimeoutHours", 8));
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

// ── Memory cache ────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── Application services ────────────────────────────────────────────────────
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IImageOptimizerService, ImageOptimizerService>();
builder.Services.AddScoped<ISaleManagerService, SaleManagerService>();
builder.Services.AddScoped<IReceiptPrinterService, ReceiptPrinterService>();

// ── Options — existing ───────────────────────────────────────────────────────
builder.Services.Configure<CatalogOptions>(
    builder.Configuration.GetSection(CatalogOptions.SectionName));
builder.Services.Configure<PaymentOptions>(
    builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.Configure<ReceiptOptions>(
    builder.Configuration.GetSection(ReceiptOptions.SectionName));

// ── Options — new (Requirements 25.1-25.9) ──────────────────────────────────
// Each section is bound AND validated at startup via IValidateOptions<T>.
// ValidateOnStart() forces validation before the first request is served.

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<DatabaseOptions>,
    DatabaseOptionsValidator>();
builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateOnStart();

builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<CacheOptions>, CacheOptionsValidator>();
builder.Services.AddOptions<CacheOptions>()
    .Bind(builder.Configuration.GetSection(CacheOptions.SectionName))
    .ValidateOnStart();

builder.Services.Configure<AuthOptions>(
    builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();
builder.Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration.GetSection(AuthOptions.SectionName))
    .ValidateOnStart();

builder.Services.Configure<ImageOptions>(
    builder.Configuration.GetSection(ImageOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<ImageOptions>, ImageOptionsValidator>();
builder.Services.AddOptions<ImageOptions>()
    .Bind(builder.Configuration.GetSection(ImageOptions.SectionName))
    .ValidateOnStart();

// ── Localization ─────────────────────────────────────────────────────────────
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supported = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("km-KH")
    };
    options.DefaultRequestCulture    = new RequestCulture("en-US");
    options.SupportedCultures        = supported;
    options.SupportedUICultures      = supported;
    options.RequestCultureProviders  = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// ── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Service Client (SqlServiceClient - ADO.NET stored procedures) ────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<SqlServiceClient>();
builder.Services.AddScoped<IServiceClient>(sp =>
    sp.GetRequiredService<SqlServiceClient>());

var app = builder.Build();

// ── Log non-sensitive config at startup (Requirement 25.7) ───────────────────
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
var cfg = app.Configuration;

startupLogger.LogInformation(
    "[Config] Database.CommandTimeoutSeconds={Timeout} RetryCount={Retry}",
    cfg["Database:CommandTimeoutSeconds"],
    cfg["Database:RetryCount"]);

startupLogger.LogInformation(
    "[Config] Cache.UseRedis={UseRedis} CatalogExpiry={CatalogMin}min ImageExpiry={ImageHrs}h",
    cfg["Cache:UseRedis"],
    cfg["Cache:CatalogSlidingExpirationMinutes"],
    cfg["Cache:ImageAbsoluteExpirationHours"]);

startupLogger.LogInformation(
    "[Config] Auth.SessionTimeoutHours={SessionHours} MaxFailedAttempts={MaxFail} LockoutMinutes={Lockout}",
    cfg["Auth:SessionTimeoutHours"],
    cfg["Auth:MaxFailedAttempts"],
    cfg["Auth:LockoutMinutes"]);

startupLogger.LogInformation(
    "[Config] Image.MaxConcurrentProcessing={MaxConc}",
    cfg["Image:MaxConcurrentProcessing"]);

// ══════════════════════════════════════════════════════════════════════════════
//  Middleware pipeline — ORDER IS STRICT (per design spec)
// ══════════════════════════════════════════════════════════════════════════════

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseResponseCompression();

app.MapRazorPages();
app.MapHub<CartHub>("/cartHub");

// ── Image optimisation route (/Images/{*path}) ───────────────────────────────
app.MapGet("/Images/{*imagePath}", async (
    string                  imagePath,
    int                     w,
    int                     h,
    string?                 format,
    IImageOptimizerService  imageOptimizer,
    HttpContext             ctx,
    ILogger<Program>        logger) =>
{
    if (string.IsNullOrWhiteSpace(imagePath))
        return Results.BadRequest("Image path is required.");

    var targetWidth  = w  > 0 ? w  : 300;
    var targetHeight = h  > 0 ? h  : 200;
    var imgFormat    = string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase)
                       ? ImageFormat.Jpeg
                       : ImageFormat.WebP;

    var decodedPath = Uri.UnescapeDataString(imagePath);

    try
    {
        var optimized = await imageOptimizer.GetOptimizedAsync(
            decodedPath, targetWidth, targetHeight, imgFormat);

        var incomingETag = ctx.Request.Headers.IfNoneMatch.FirstOrDefault();
        if (!string.IsNullOrEmpty(incomingETag) &&
            string.Equals(incomingETag, optimized.ETag, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        ctx.Response.Headers.CacheControl = $"public, max-age={604800}";
        ctx.Response.Headers.ETag         = optimized.ETag;
        ctx.Response.Headers.Expires      = optimized.Expires.ToString("R");

        return Results.File(optimized.Data, optimized.ContentType);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Image route failed for path: {ImagePath}", decodedPath);
        return Results.Redirect("/images/placeholder.png");
    }
});

// ── Cart badge count endpoint for SignalR polling fallback (Req 7.6) ─────────
app.MapGet("/Cart/Count", (HttpContext ctx, ICartService cartSvc) =>
{
    var count = cartSvc.GetItemCount(ctx.Session);
    return Results.Json(new { count });
}).RequireAuthorization();

app.Run();

// ── Test accessibility ────────────────────────────────────────────────────────
public partial class Program { }