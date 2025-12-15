using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GrapheneTrace.Models;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace GrapheneTrace.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admins can access these actions
    public class AdminController : Controller
    {
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            // Prevent browser from caching admin pages to block back-button access after logout
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, private";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            base.OnActionExecuting(context);
        }
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly GrapheneTrace.Data.ApplicationDbContext _db;
        private readonly IHostEnvironment _env;
        private readonly ILogger<AdminController> _logger;

        public AdminController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, GrapheneTrace.Data.ApplicationDbContext db, IHostEnvironment env, ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _db = db;
            _env = env;
            _logger = logger;
        }

        // MAIN DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var clinicianCount = (await _userManager.GetUsersInRoleAsync("Clinician")).Count;
            var patientCount = (await _userManager.GetUsersInRoleAsync("Patient")).Count;

            var vm = new DashboardStatsViewModel
            {
                TotalUsers = totalUsers,
                ClinicianCount = clinicianCount,
                PatientCount = patientCount
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> KpiDetails(string type)
        {
            type = (type ?? string.Empty).ToLowerInvariant();

            var users = await _userManager.Users.ToListAsync();
            var assignments = await _db.ClinicianPatientAssignments.ToListAsync();

            var clinicianPatientCounts = assignments
                .GroupBy(a => a.ClinicianUserId)
                .ToDictionary(g => g.Key, g => g.Count());

            var patientClinicianMap = assignments
                .GroupBy(a => a.PatientUserId)
                .ToDictionary(g => g.Key, g => g.First().ClinicianUserId);

            IEnumerable<object> data;

            if (type == "clinicians")
            {
                var clinicians = await _userManager.GetUsersInRoleAsync("Clinician");
                data = clinicians.Select(c => new
                {
                    Email = c.Email,
                    UserName = c.UserName,
                    Role = "Clinician",
                    PatientCount = clinicianPatientCounts.TryGetValue(c.Id, out var cnt) ? cnt : 0
                });
            }
            else if (type == "patients")
            {
                var patients = await _userManager.GetUsersInRoleAsync("Patient");
                var clinicianEmails = users.ToDictionary(u => u.Id, u => u.Email);
                data = patients.Select(p => new
                {
                    Email = p.Email,
                    UserName = p.UserName,
                    Role = "Patient",
                    AssignedClinicianEmail = patientClinicianMap.TryGetValue(p.Id, out var cid) && clinicianEmails.TryGetValue(cid, out var cem) ? cem : null
                });
            }
            else
            // total
            {
                data = await Task.WhenAll(users.Select(async u => new
                {
                    Email = u.Email,
                    UserName = u.UserName,
                    Role = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? string.Empty
                }));
            }

            return Json(new { type, count = data.Count(), items = data });
        }

        // USER MANAGEMENT PAGE
        public async Task<IActionResult> UserManagement()
        {
            var users = await _userManager.Users.ToListAsync();
            var viewModel = new List<AdminUserViewModel>();

            // Preload assignments for mapping Patient -> Clinician
            var assignments = await _db.ClinicianPatientAssignments.ToListAsync();
            var patientToClinician = assignments
                .GroupBy(a => a.PatientUserId)
                .ToDictionary(g => g.Key, g => g.First().ClinicianUserId);

            var emailById = users.ToDictionary(u => u.Id, u => u.Email);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "";
                var count = 0;
                if (role == "Clinician")
                {
                    count = await _db.ClinicianPatientAssignments
                        .Where(a => a.ClinicianUserId == user.Id)
                        .CountAsync();
                }

                string assignedClinicianEmail = null;
                if (role == "Patient" && patientToClinician.TryGetValue(user.Id, out var clinicianId))
                {
                    if (emailById.TryGetValue(clinicianId, out var email))
                        assignedClinicianEmail = email;
                }

                viewModel.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = role,
                    PhoneNumber = user.PhoneNumber,
                    PatientCount = count,
                    AssignedClinicianEmail = assignedClinicianEmail
                });
            }

            // Populate clinician and patient lists for assignment UI
            var clinicians = await _userManager.GetUsersInRoleAsync("Clinician");
            var patients = await _userManager.GetUsersInRoleAsync("Patient");

            ViewBag.Clinicians = clinicians
                .Select(c => new { c.Id, c.Email })
                .OrderBy(c => c.Email)
                .ToList();
            ViewBag.Patients = patients
                .Select(p => new { p.Id, p.Email })
                .OrderBy(p => p.Email)
                .ToList();

            return View(viewModel);
        }

        // SETTINGS PAGE
        public IActionResult Settings()
        {
            var settings = _db.SystemSettings.FirstOrDefault() ?? new SystemSettings
            {
                Id = 1,
                Timezone = "UTC",
                Language = "English",
                AutoLogout = "30 Minutes",
                DataRetention = "30 Days",
                BackupFrequency = "Weekly",
                EmailNotifications = "Enabled",
                SmsAlerts = "Off",
                WeeklySummary = "Enabled",
                MaintenanceMode = "Off",
                MaxLoginAttempts = "3 Attempts"
            };

            if (settings.Id == 0)
            {
                settings.Id = 1;
                _db.SystemSettings.Add(settings);
                _db.SaveChanges();
            }

            var vm = new SettingsViewModel
            {
                DisplayName = settings.DisplayName,
                PhoneNumber = settings.PhoneNumber,
                Timezone = settings.Timezone,
                Language = settings.Language,
                AutoLogout = settings.AutoLogout,
                DataRetention = settings.DataRetention,
                BackupFrequency = settings.BackupFrequency,
                EmailNotifications = settings.EmailNotifications,
                SmsAlerts = settings.SmsAlerts,
                WeeklySummary = settings.WeeklySummary,
                MaintenanceMode = settings.MaintenanceMode,
                MaxLoginAttempts = settings.MaxLoginAttempts
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(SettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid settings input.";
                return RedirectToAction("Settings");
            }

            var settings = _db.SystemSettings.FirstOrDefault() ?? new SystemSettings { Id = 1 };

            settings.DisplayName = model.DisplayName;
            settings.PhoneNumber = model.PhoneNumber;
            settings.Timezone = model.Timezone;
            settings.Language = model.Language;
            settings.AutoLogout = model.AutoLogout;
            settings.DataRetention = model.DataRetention;
            settings.BackupFrequency = model.BackupFrequency;
            settings.EmailNotifications = model.EmailNotifications;
            settings.SmsAlerts = model.SmsAlerts;
            settings.WeeklySummary = model.WeeklySummary;
            settings.MaintenanceMode = model.MaintenanceMode;
            settings.MaxLoginAttempts = model.MaxLoginAttempts;

            if (_db.SystemSettings.Any())
                _db.SystemSettings.Update(settings);
            else
                _db.SystemSettings.Add(settings);

            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin updated system settings");
            TempData["Success"] = "System settings saved.";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(SettingsViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword) || string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                TempData["Error"] = "All password fields are required.";
                return RedirectToAction("Settings");
            }
            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["Error"] = "New password and confirmation do not match.";
                return RedirectToAction("Settings");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Settings");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Settings");
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Admin password changed for {Email}", user.Email);
            TempData["Success"] = "Password updated.";
            return RedirectToAction("Settings");
        }

        // LOGS PAGE
        public IActionResult Logs()
        {
            var logDir = Path.Combine(_env.ContentRootPath, "Logs");
            if (!Directory.Exists(logDir))
            {
                return View(new List<LogEntryViewModel>());
            }

            var latest = Directory.GetFiles(logDir, "app-*.log")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (latest == null)
                return View(new List<LogEntryViewModel>());

            var lines = System.IO.File.ReadLines(latest).TakeLast(500).ToList();
            var entries = new List<LogEntryViewModel>();

            foreach (var line in lines)
            {
                var entry = ParseSerilogLine(line);
                if (entry != null && IsLoginRelated(entry.Message))
                    entries.Add(entry);
            }

            return View(entries.OrderByDescending(e => e.Timestamp));
        }

        private static bool IsLoginRelated(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;
            
            // Show only successful login and logout sessions
            var sessionKeywords = new[] { 
                "Login succeeded",
                "Logout",
                "successfully"
            };
            return sessionKeywords.Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private static LogEntryViewModel ParseSerilogLine(string line)
        {
            // Serilog default format: 2025-12-13 07:00:00.000 +00:00 [INF] Message
            // We parse timestamp, level between brackets, and remainder as message.
            try
            {
                var firstBracket = line.IndexOf('[');
                var secondBracket = line.IndexOf(']');
                if (firstBracket < 0 || secondBracket < firstBracket)
                    return null;

                var tsPart = line.Substring(0, firstBracket).Trim();
                if (!DateTime.TryParse(tsPart, out var ts))
                    ts = DateTime.MinValue;

                var level = line.Substring(firstBracket + 1, secondBracket - firstBracket - 1).Trim();
                var message = line.Substring(secondBracket + 1).Trim();

                return new LogEntryViewModel
                {
                    Timestamp = ts,
                    Level = level,
                    Message = message,
                    Raw = line
                };
            }
            catch
            {
                return null;
            }
        }

        // CREATE USER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string firstName, string lastName, string email, string phoneNumber, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Email and password are required.";
                return RedirectToAction("UserManagement");
            }

            // Validate email format
            if (!email.Contains("@") || !email.Contains("."))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("UserManagement");
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                TempData["Error"] = "User with this email already exists.";
                return RedirectToAction("UserManagement");
            }

            // Validate password strength
            if (password.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                return RedirectToAction("UserManagement");
            }

            var user = new ApplicationUser 
            { 
                UserName = email, 
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber
            };
            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                TempData["Error"] = "Failed to create user: " + string.Join("; ", create.Errors.Select(e => e.Description));
                return RedirectToAction("UserManagement");
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));
                await _userManager.AddToRoleAsync(user, role);
            }

            _logger.LogInformation("Admin created user {Email} with role {Role}", email, role);

            TempData["Success"] = $"User '{email}' created successfully with role '{role ?? "(none)"}'.";
            return RedirectToAction("UserManagement");
        }

        // EDIT USER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string firstName, string lastName, string email, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "User ID and email are required.";
                return RedirectToAction("UserManagement");
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("UserManagement");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("UserManagement");
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null && existing.Id != id)
            {
                TempData["Error"] = "This email is already in use by another user.";
                return RedirectToAction("UserManagement");
            }

            user.Email = email;
            user.UserName = email;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;
            var update = await _userManager.UpdateAsync(user);
            if (update.Succeeded)
            {
                TempData["Success"] = $"User updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update user: " + string.Join("; ", update.Errors.Select(e => e.Description));
            }
            return RedirectToAction("UserManagement");
        }

        // DELETE/DEACTIVATE USER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("UserManagement");
            }
            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "User deleted." : string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction("UserManagement");
        }

        // ASSIGN ROLE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("UserManagement");
            }
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            // Track user domain assignment
            var ud = await _db.UserDomains.FindAsync(user.Id);
            if (ud == null)
            {
                ud = new UserDomain { UserId = user.Id, Email = user.Email, Domain = role };
                _db.UserDomains.Add(ud);
            }
            else
            {
                ud.Email = user.Email;
                ud.Domain = role;
                _db.UserDomains.Update(ud);
            }
            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin set role {Role} for user {Email}", role, user.Email);
            TempData["Success"] = "Role updated successfully.";
            return RedirectToAction("UserManagement");
        }

        // ASSIGN PATIENT TO CLINICIAN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPatientToClinician(string patientId, string clinicianId)
        {
            if (string.IsNullOrWhiteSpace(patientId) || string.IsNullOrWhiteSpace(clinicianId))
            {
                TempData["Error"] = "Both clinician and patient are required.";
                return RedirectToAction("UserManagement");
            }

            // Validate roles
            var clinician = await _userManager.FindByIdAsync(clinicianId);
            var patient = await _userManager.FindByIdAsync(patientId);
            if (clinician == null || patient == null)
            {
                TempData["Error"] = "Invalid clinician or patient.";
                return RedirectToAction("UserManagement");
            }
            if (!await _userManager.IsInRoleAsync(clinician, "Clinician") || !await _userManager.IsInRoleAsync(patient, "Patient"))
            {
                TempData["Error"] = "Role mismatch: ensure correct roles for assignment.";
                return RedirectToAction("UserManagement");
            }

            // Enforce single clinician per patient: remove other assignments for this patient
            var allForPatient = await _db.ClinicianPatientAssignments
                .Where(a => a.PatientUserId == patientId)
                .ToListAsync();

            var alreadyLinked = allForPatient.Any(a => a.ClinicianUserId == clinicianId);
            var toRemove = allForPatient.Where(a => a.ClinicianUserId != clinicianId).ToList();
            if (toRemove.Any())
            {
                _db.ClinicianPatientAssignments.RemoveRange(toRemove);
            }

            if (!alreadyLinked)
            {
                _db.ClinicianPatientAssignments.Add(new ClinicianPatientAssignment
                {
                    ClinicianUserId = clinicianId,
                    PatientUserId = patientId
                });
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin assigned patient {PatientEmail} to clinician {ClinicianEmail}", patient.Email, clinician.Email);
            TempData["Success"] = "Patient assignment updated.";
            return RedirectToAction("UserManagement");
        }
    }
}