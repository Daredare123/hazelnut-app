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
            var sales = await _context.Sales.ToListAsync();
            var expenses = await _context.Expenses.ToListAsync();
            var inventory = await _context.Inventory.FirstOrDefaultAsync();

            double stock = inventory?.TotalKg ?? 0;

            // Totals
            ViewBag.TotalSalesAmount = sales.Sum(s => s.Total);
            ViewBag.TotalQuantityKg = sales.Sum(s => s.QuantityKg);
            ViewBag.SalesCount = sales.Count;

            // Financial
            var totalRevenue = sales.Sum(s => s.Total);
            var totalCosts = expenses.Sum(e => e.Amount);
            var totalProfit = totalRevenue - totalCosts;

            ViewBag.TotalSales = totalRevenue;
            ViewBag.TotalExpenses = totalCosts;
            ViewBag.TotalProfit = totalProfit;

            // Monthly
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var monthlySales = sales
                .Where(s => s.Date.Month == currentMonth && s.Date.Year == currentYear)
                .ToList();

            ViewBag.MonthlyAmount = monthlySales.Sum(s => s.Total);
            ViewBag.MonthlyKg = monthlySales.Sum(s => s.QuantityKg);
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
