using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HazelnutVeb.Data;
using HazelnutVeb.Models;

namespace HazelnutVeb.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Monthly(int? month, int? year)
        {
            int m = month ?? DateTime.Now.Month;
            int y = year ?? DateTime.Now.Year;

            // Using EF Core queries
            var sales = await _context.Sales
                .Where(s => s.Date.Month == m && s.Date.Year == y)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.Date.Month == m && e.Date.Year == y)
                .ToListAsync();

            var model = new MonthlyReportViewModel
            {
                Month = m,
                Year = y,
                TotalSales = sales.Sum(s => s.Total),
                TotalExpenses = expenses.Sum(e => e.Amount),
                TotalKg = sales.Sum(s => s.QuantityKg),
                TotalTransactions = sales.Count
            };

            model.Profit = model.TotalSales - model.TotalExpenses;

            return View(model);
        }

        public async Task<IActionResult> Yearly(int year)
        {
            if (year == 0)
            {
                year = DateTime.Now.Year;
            }

            var sales = await _context.Sales
                .Where(s => s.Date.Year == year)
                .ToListAsync();

            var model = new YearlyReportViewModel
            {
                Year = year,
                TotalRevenue = sales.Sum(s => s.QuantityKg * s.PricePerKg),
                TotalQuantity = sales.Sum(s => s.QuantityKg),
                TotalSalesCount = sales.Count
            };

            return View(model);
        }

        public async Task<IActionResult> DateRange(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Sales.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(s => s.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.Date <= endDate.Value);
            }

            var sales = await query.ToListAsync();

            var model = new DateRangeReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = sales.Sum(s => s.QuantityKg * s.PricePerKg),
                TotalQuantity = sales.Sum(s => s.QuantityKg),
                TotalSalesCount = sales.Count
            };

            return View(model);
        }
    }
}
