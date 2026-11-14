using System.Security.Claims;
using HRMS.Web.Security;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace HRMS.Web.Controllers;
[ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public class AccountController(DemoAccountService account, LoginGuard guard, DemoSessions sessions) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return Destination(returnUrl);
        ViewData["SetupRequired"] = !account.IsConfigured;
        return View(new LoginViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (User.Identity?.IsAuthenticated == true) return Destination(vm.ReturnUrl);
        if (ModelState.IsValid)
        {
            if (!account.IsConfigured)
                ModelState.AddModelError("", "Chưa thiết lập tài khoản demo. Chạy tiện ích --setup-demo-account theo hướng dẫn bàn giao.");
            else if (guard.Attempt(() => account.Verify(vm.Username, vm.Password), out var locked))
            {
                var identity = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, account.Username), new Claim("demo-session", sessions.Create())
                }, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
                return Destination(vm.ReturnUrl);
            }
            else ModelState.AddModelError("", locked ? "Đã có quá nhiều lần đăng nhập sai. Vui lòng thử lại sau 5 phút." : "Tên tài khoản hoặc mật khẩu không đúng.");
        }
        vm.Password = "";
        // Tag Helpers prefer ModelState attempted values: clear password there as well.
        var errors = ModelState[nameof(vm.Password)]?.Errors.Select(x => x.ErrorMessage).ToArray() ?? Array.Empty<string>();
        ModelState.Remove(nameof(vm.Password));
        foreach (var error in errors) ModelState.AddModelError(nameof(vm.Password), error);
        vm.ReturnUrl = Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : null;
        ModelState.Remove(nameof(vm.ReturnUrl));
        ViewData["SetupRequired"] = !account.IsConfigured;
        return View(vm);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        sessions.Revoke(User.FindFirstValue("demo-session"));
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
    private IActionResult Destination(string? returnUrl) => Url.IsLocalUrl(returnUrl)
        ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Employees");
}
