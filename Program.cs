using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HazelnutVeb.Data;
using Microsoft.Extensions.Logging;
using HazelnutVeb.Models;
var builder = WebApplication.CreateBuilder(args);

// Add connection string and services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=hazelnut.db";

builder.Services.AddDbContext<HazelnutVeb.Data.AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Add controllers with views
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure database is created and initialized
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<HazelnutVeb.Data.AppDbContext>();
        
        // Automatically create database if it does not exist
        context.Database.EnsureCreated();

        // Seed Inventory if empty
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