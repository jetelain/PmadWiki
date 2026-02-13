using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Pmad.Wiki.Demo.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login(string? returnUrl = null)
        {
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/", IsPersistent = true });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
