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
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public ReservationsController(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var reservations = await _context.Reservations
                    .Include(r => r.Client)
                    .OrderByDescending(r => r.Date)
                    .ToListAsync() ?? new List<Reservation>();
                return View(reservations);
            }
            catch (Exception)
            {
                return View(new List<Reservation>());
            }
        }

        public IActionResult Create()
        {
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name");
            return View(new Reservation { Date = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientId,Quantity,Date")] Reservation reservation)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", reservation.ClientId);
                return View(reservation);
            }

            try
            {
                reservation.Status = "Reserved";

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
                    ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", reservation.ClientId);
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

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "Name", reservation.ClientId);
                return View(reservation);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Complete(int id, double pricePerKg)
        {
            try
            {
                var reservation = await _context.Reservations.Include(r => r.Client).FirstOrDefaultAsync(r => r.Id == id);
                if (reservation == null || reservation.Status != "Reserved")
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
                    ClientName = reservation.Client?.Name ?? "Unknown"
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
                if (reservation == null || reservation.Status != "Reserved")
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
