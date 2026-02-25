using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HazelnutVeb.Data;
using HazelnutVeb.Models;
using HazelnutVeb.Services;

namespace HazelnutVeb.Controllers
{
    [Authorize(Roles = "Admin,Client")]
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly EmailService _emailService;

        public ReservationsController(AppDbContext context, NotificationService notificationService, EmailService emailService)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var query = _context.Reservations
                    .Include(r => r.Client)
                    .AsQueryable();

                var email = User.Identity?.Name;
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (currentUser != null && currentUser.Role != "Admin")
                {
                    query = query.Where(r => r.Client.Email == email);
                }

                var reservations = await query
                    .OrderByDescending(r => r.Date)
                    .ToListAsync() ?? new List<Reservation>();
                return View(reservations);
            }
            catch (Exception)
            {
                return View(new List<Reservation>());
            }
        }

        public IActionResult MyOrders()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View(new Reservation { Date = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Quantity,Date")] Reservation reservation)
        {
            if (reservation == null)
            {
                ModelState.AddModelError("", "Reservation data is missing.");
                return View(new Reservation { Date = DateTime.UtcNow });
            }

            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Unable to verify current user session.");
                return View(reservation);
            }

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == email);
            if (client == null)
            {
                client = new Client
                {
                    Email = email,
                    Name = email,
                    Phone = "Unknown",
                    City = "Unknown"
                };
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
                
                // Reload Client
                client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == email);
            }

            if (client != null)
            {
                reservation.ClientId = client.Id;
            }

            if (!ModelState.IsValid)
            {
                return View(reservation);
            }

            try
            {
                if (reservation.Quantity <= 0)
                {
                    ModelState.AddModelError("Quantity", "Quantity must be greater than zero.");
                    return View(reservation);
                }

                reservation.Status = "Pending";

                // Ensure PostgreSQL DateTime strictly enforced dynamically to UTC without nullable errors
                if (reservation.Date != DateTime.MinValue)
                {
                    reservation.Date = DateTime.SpecifyKind(reservation.Date, DateTimeKind.Utc);
                }
                else 
                {
                    reservation.Date = DateTime.UtcNow;
                }

                var inventory = await _context.Inventory.FirstOrDefaultAsync();
                if (inventory == null)
                {
                    inventory = new Inventory { TotalKg = 0 };
                    _context.Inventory.Add(inventory);
                    await _context.SaveChangesAsync();
                }

                if (reservation.Quantity > inventory.TotalKg)
                {
                    ModelState.AddModelError("Quantity", $"Not enough inventory. Current stock: {inventory.TotalKg:N2} kg");
                    return View(reservation);
                }

                _context.Reservations.Add(reservation);
                
                inventory.TotalKg -= reservation.Quantity;
                _context.Update(inventory);
                
                await _context.SaveChangesAsync();

                if (inventory.TotalKg <= 5)
                {
                    await _notificationService.SendLowInventoryNotification(inventory.TotalKg);
                }

                await _emailService.SendEmailAsync("admin@hazelnut.com", "New Reservation", $"A new reservation for {reservation.Quantity} kg on {reservation.Date:yyyy-MM-dd} is pending approval.");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                return View(reservation);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var reservation = await _context.Reservations.Include(r => r.Client).FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null || reservation.Status != "Pending") return NotFound();

            reservation.Status = "Approved";
            _context.Update(reservation);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(reservation.Client?.Email))
            {
                await _emailService.SendEmailAsync(reservation.Client.Email, "Reservation Approved", $"Your reservation for {reservation.Quantity} kg on {reservation.Date:yyyy-MM-dd} has been approved.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var reservation = await _context.Reservations.Include(r => r.Client).FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null || reservation.Status != "Pending") return NotFound();

            reservation.Status = "Rejected";

            var inventory = await _context.Inventory.FirstOrDefaultAsync();
            if (inventory != null)
            {
                inventory.TotalKg += reservation.Quantity;
                _context.Update(inventory);
            }

            _context.Update(reservation);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(reservation.Client?.Email))
            {
                await _emailService.SendEmailAsync(reservation.Client.Email, "Reservation Rejected", $"Your reservation for {reservation.Quantity} kg on {reservation.Date:yyyy-MM-dd} has been rejected.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Complete(int id, double pricePerKg)
        {
            try
            {
                var reservation = await _context.Reservations.Include(r => r.Client).FirstOrDefaultAsync(r => r.Id == id);
                if (reservation == null || reservation.Status != "Approved")
                {
                    return NotFound();
                }

                reservation.Status = "Completed";
                _context.Update(reservation);

                var sale = new Sale
                {
                    Date = DateTime.UtcNow,
                    QuantityKg = reservation.Quantity,
                    PricePerKg = pricePerKg,
                    Total = reservation.Quantity * pricePerKg,
                    ClientName = reservation.Client?.Email ?? "Unknown"
                };

                _context.Sales.Add(sale);
                _context.Entry(sale).Property("ClientId").CurrentValue = reservation.ClientId;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
                if (reservation == null || (reservation.Status != "Pending" && reservation.Status != "Approved"))
                {
                    return NotFound();
                }

                reservation.Status = "Cancelled";
                _context.Update(reservation);

                var inventory = await _context.Inventory.FirstOrDefaultAsync();
                if (inventory == null)
                {
                    inventory = new Inventory { TotalKg = 0 };
                    _context.Inventory.Add(inventory);
                }

                inventory.TotalKg += reservation.Quantity;
                _context.Update(inventory);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
