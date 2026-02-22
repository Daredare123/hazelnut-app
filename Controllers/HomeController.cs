using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HazelnutVeb.Data;
using HazelnutVeb.Models;
using System.Diagnostics;

namespace HazelnutVeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            // Fetch everything asynchronously
            var sales = await _context.Sales.ToListAsync() ?? new List<Sale>();
            var expenses = await _context.Expenses.ToListAsync() ?? new List<Expense>();
            var inventory = await _context.Inventory.FirstOrDefaultAsync();

            if (inventory == null)
            {
                inventory = new Inventory { TotalKg = 0 };
                _context.Inventory.Add(inventory);
                await _context.SaveChangesAsync();
            }

            double stock = inventory.TotalKg;

            // Totals
            ViewBag.TotalSalesAmount = sales.Any() ? sales.Sum(s => s.Total) : 0;
            ViewBag.TotalQuantityKg = sales.Any() ? sales.Sum(s => s.QuantityKg) : 0;
            ViewBag.SalesCount = sales.Count;

            // Financial
            var totalRevenue = sales.Any() ? sales.Sum(s => s.Total) : 0;
            var totalCosts = expenses.Any() ? expenses.Sum(e => e.Amount) : 0;
            var totalProfit = totalRevenue - totalCosts;

            ViewBag.TotalSales = totalRevenue;
            ViewBag.TotalExpenses = totalCosts;
            ViewBag.TotalProfit = totalProfit;

            // Monthly
            DateTime start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime end = start.AddMonths(1).AddDays(-1);

            // Using end.AddDays(1) or handling time safely without .Date.Month
            var monthlySales = sales
                .Where(s => s.Date >= start && s.Date < end.AddDays(1))
                .ToList();

            ViewBag.MonthlyAmount = monthlySales.Any() ? monthlySales.Sum(s => s.Total) : 0;
            ViewBag.MonthlyKg = monthlySales.Any() ? monthlySales.Sum(s => s.QuantityKg) : 0;
            ViewBag.MonthlyCount = monthlySales.Count;

            // Inventory
            ViewBag.CurrentStock = stock;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
