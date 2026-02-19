using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HazelnutVeb.Data;
using HazelnutVeb.Models;
using HazelnutVeb.Services;

namespace HazelnutVeb.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public SalesController(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Sales.ToListAsync());
        }

        public IActionResult Create()
        {
            return View(new Sale { Date = DateTime.Now });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Sale sale)
        {
            // Fetch Inventory (create if not exists, but should exist from Program.cs)
            var inventory = await _context.Inventory.FirstOrDefaultAsync();
            if (inventory == null)
            {
                inventory = new Inventory { TotalKg = 0 };
                _context.Inventory.Add(inventory);
                await _context.SaveChangesAsync();
            }

            // Inventory Validation
            if (sale.QuantityKg > inventory.TotalKg)
            {
                ModelState.AddModelError("QuantityKg", $"Not enough inventory. Current stock: {inventory.TotalKg:N2} kg");
                return View(sale);
            }

            // Calculate Total if client didn't send it or recalculate for safety
            sale.Total = sale.QuantityKg * sale.PricePerKg;

            // Ensure Date is valid
            if (sale.Date == DateTime.MinValue)
            {
                sale.Date = DateTime.Now;
            }

            // Add Sale
            _context.Sales.Add(sale);
            
            // Update Inventory
            inventory.TotalKg -= sale.QuantityKg;
            _context.Update(inventory);
            
            await _context.SaveChangesAsync();

            if (inventory.TotalKg <= 5)
            {
                await _notificationService.SendLowInventoryNotification(inventory.TotalKg);
            }

            return RedirectToAction(nameof(Index));
        }

        // Keep simplified delete/details actions if needed, or stick to Index/Create as requested
    }
}
