using Microsoft.AspNetCore.RateLimiting;
using HRMS.Web.Data;
using HRMS.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

if (args.Contains("--setup-demo-account"))
{
    Environment.ExitCode = HRMS.Web.Security.DemoAccountSetup.Run();
    return;
}
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<HRMS.Web.Security.DemoAccountService>();
builder.Services.AddSingleton<HRMS.Web.Security.LoginGuard>();
builder.Services.AddSingleton<HRMS.Web.Security.DemoSessions>();
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.Cookie.Name = "HRMS.Demo.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = HRMS.Web.Security.DemoSessions.IdleTimeout;
        o.SlidingExpiration = true;
        o.Events.OnValidatePrincipal = async context =>
        {
            var sessions = context.HttpContext.RequestServices.GetRequiredService<HRMS.Web.Security.DemoSessions>();
            if (!sessions.Touch(context.Principal?.FindFirst("demo-session")?.Value))
            {
                context.RejectPrincipal();
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context.HttpContext);
            }
        };
        o.Events.OnRedirectToLogin = context => AuthRedirect(context, 401);
        o.Events.OnRedirectToAccessDenied = context => AuthRedirect(context, 403);
    });
builder.Services.AddAuthorization(o => o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy("login", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit=10, Window=TimeSpan.FromMinutes(1), QueueLimit=0 }));
    o.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Quá nhiều yêu cầu đăng nhập. Vui lòng chờ 1 phút rồi thử lại.", token);
    };
});
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(n => $"{n} phải là số.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((v,n) => $"Giá trị nhập cho {n} không hợp lệ.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(v => "Giá trị nhập không hợp lệ.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(v => "Vui lòng nhập giá trị.");
    options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(v => "Giá trị nhập không hợp lệ.");
});
builder.Services.AddDbContext<HrmsContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("Hrms"), sql => sql.UseCompatibilityLevel(120)));
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<RewardDecisionService>();
builder.Services.AddScoped<PersonnelService>();
builder.Services.AddScoped<PersonnelQueryService>();
builder.Services.AddScoped<HistoryQueryService>();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 6 * 1024 * 1024);
var app = builder.Build();
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
// Sensitive MVC responses, including redirects and employee photos, must not be cached.
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
    context.Response.Headers.Pragma = "no-cache";
    context.Response.Headers.Expires = "0";
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllerRoute("default", "{controller=Employees}/{action=Index}/{id?}");
// Cố ý không gọi Migrate, EnsureCreated, EnsureDeleted hoặc seed dữ liệu.
app.Run();
static Task AuthRedirect(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context, int status)
{
    var request = context.Request;
    if (request.Headers["X-Requested-With"] == "XMLHttpRequest" || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { message = status == 401 ? "Phiên đăng nhập đã hết. Vui lòng đăng nhập lại." : "Không có quyền truy cập." });
    }
    context.Response.Redirect(context.RedirectUri);
    return Task.CompletedTask;
}
public partial class Program { }
