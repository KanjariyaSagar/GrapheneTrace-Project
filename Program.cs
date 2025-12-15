using System;
using GrapheneTrace.Models;
using GrapheneTrace.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging: console + rolling file
var logPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Logs", "app-.log");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure SQLite DB (use ApplicationDbContext to match migrations)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity with basic password policy and cookie path
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
});

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ------------------- DATABASE SEEDING (ADMIN USER) -------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Ensure the SQLite database and schema exist before seeding
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Create roles if missing
        string[] roles = { "Admin", "Clinician", "Patient" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Create default admin user if not exists
        string adminEmail = "admin@graphenetrace.com";
        string adminPassword = "Admin@123";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail
                // ❗ No FullName, No Role fields here
            };

            var result = await userManager.CreateAsync(admin, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                Console.WriteLine("✔ Admin account created successfully!");
            }
            else
            {
                Console.WriteLine("❌ Failed to create admin:");
                foreach (var error in result.Errors)
                    Console.WriteLine(error.Description);
            }
        }
        else
        {
            Console.WriteLine("✔ Admin already exists.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ ERROR during DB seed: " + ex.Message);
    }
}
// ------------------------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware to set no-cache headers for authenticated users (prevents back-button access to cached admin pages)
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, private";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    await next();
});

// Default route → login page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
