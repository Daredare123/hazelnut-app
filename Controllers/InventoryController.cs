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
    public class InventoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public InventoryController(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var inventory = await _context.Inventory.FirstOrDefaultAsync();
            if (inventory == null)
            {
                inventory = new Inventory { TotalKg = 0 };
                _context.Inventory.Add(inventory);
                await _context.SaveChangesAsync();
            }
            return View(inventory);
        }

        public async Task<IActionResult> Update()
        {
            var inventory = await _context.Inventory.FirstOrDefaultAsync();
            if (inventory == null)
            {
                inventory = new Inventory { TotalKg = 0 };
                _context.Inventory.Add(inventory);
                await _context.SaveChangesAsync();
            }
            return View(inventory);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Inventory inventory)
        {
            if (inventory.TotalKg < 0)
            {
                ModelState.AddModelError("TotalKg", "Inventory cannot be negative.");
                return View(inventory);
            }

            var dbInventory = await _context.Inventory.FirstOrDefaultAsync();
            if (dbInventory == null)
            {
                dbInventory = new Inventory();
                _context.Inventory.Add(dbInventory);
            }
            
            dbInventory.TotalKg = inventory.TotalKg;
            await _context.SaveChangesAsync();

            if (dbInventory.TotalKg <= 5)
            {
                await _notificationService.SendLowInventoryNotification(dbInventory.TotalKg);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
