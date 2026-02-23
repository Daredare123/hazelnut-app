using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HazelnutVeb.Data;
using Microsoft.Extensions.Logging;
using HazelnutVeb.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;

var builder = WebApplication.CreateBuilder(args);

// Add connection string and services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<HazelnutVeb.Data.AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add controllers with views
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddScoped<HazelnutVeb.Services.NotificationService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var supportedCultures = new[] { "mk", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Enable Production Exception handling to seamlessly route HTTP 500 errors to /Home/Error
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Ensure database migrations run automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<HazelnutVeb.Data.AppDbContext>();
        
        // Use Migrate instead of EnsureCreated to execute all EF snapshots correctly
        context.Database.Migrate();

        // Seed Inventory if empty ensuring at least one record
        if (!context.Inventory.Any())
        {
            context.Inventory.Add(new Inventory { TotalKg = 0 });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating/initializing the DB.");
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();