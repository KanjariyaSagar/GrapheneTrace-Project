using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GrapheneTrace.Models;
using System.Threading.Tasks;

namespace GrapheneTrace.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // LOGIN PAGE
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string domain)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                _logger.LogWarning("Login failed (user not found) for email {Email}", email);
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, password, false, false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                _logger.LogWarning("Login failed (bad credentials) for email {Email}", email);
                return View();
            }

            // Optional domain/role selector enforcement (ignored if not provided)
            if (!string.IsNullOrWhiteSpace(domain))
            {
                var selected = domain.Trim().ToLowerInvariant();
                var roleMatches = selected switch
                {
                    "admin" => await _userManager.IsInRoleAsync(user, "Admin"),
                    "clinician" => await _userManager.IsInRoleAsync(user, "Clinician"),
                    "patient" => await _userManager.IsInRoleAsync(user, "Patient"),
                    _ => false
                };

                if (!roleMatches)
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "Selected account type does not match this account.");
                    _logger.LogWarning("Login failed (domain mismatch) for email {Email} with domain {Domain}", email, domain);
                    return View();
                }
            }

            // Redirect based on actual role
            _logger.LogInformation("Login succeeded for email {Email}", email);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Dashboard", "Admin");
            if (await _userManager.IsInRoleAsync(user, "Clinician"))
                return RedirectToAction("Dashboard", "Clinician");
            if (await _userManager.IsInRoleAsync(user, "Patient"))
                return RedirectToAction("Dashboard", "Patient");

            await _signInManager.SignOutAsync();
            ModelState.AddModelError(string.Empty, "Access denied: role not permitted.");
            return View();
        }

        // LOGOUT
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // FORGOT PASSWORD
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // FORGOT ID
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotID()
        {
            return View();
        }
    }
}