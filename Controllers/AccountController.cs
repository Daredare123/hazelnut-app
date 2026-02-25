using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HazelnutVeb.Data;
using HazelnutVeb.Models;

namespace HazelnutVeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public AccountController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                var hash = HashPassword(password);
                var user = await _userManager.FindByEmailAsync(email);
                
                // Allow fallback to EF lookup for legacy accounts before identity migration if needed, but FindByEmailAsync works.
                if (user == null)
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == hash);

                if (user != null && user.PasswordHash == hash)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var userRole = roles.FirstOrDefault() ?? "Client";

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Email!),
                        new Claim(ClaimTypes.Role, userRole)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties();

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    if (userRole == "Admin")
                    {
                        return RedirectToAction("Dashboard", "Home");
                    }
                    else
                    {
                        return RedirectToAction("MyOrders", "Reservations");
                    }
                }
            }

            ViewBag.ErrorMessage = "Invalid email or password";
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Email and password are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ViewBag.ErrorMessage = "Email is already registered.";
                return View();
            }

            var user = new User
            {
                UserName = email,
                Email = email,
                PasswordHash = HashPassword(password)
            };

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Client");

                bool clientExists = await _context.Clients.AnyAsync(c => c.Email == user.Email);
                if (!clientExists)
                {
                    var client = new Client
                    {
                        Name = user.Email!,
                        Email = user.Email
                    };
                    _context.Clients.Add(client);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}