using Microsoft.AspNetCore.Mvc;
using HazelnutVeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HazelnutVeb.Controllers
{
    public class SalesController : Controller
    {
        // In-memory storage as requested
        private static List<Sale> _sales = new List<Sale>();

        public IActionResult Index()
        {
            return View(_sales);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Sale sale)
        {
            // Simple ID generation
            sale.Id = _sales.Any() ? _sales.Max(s => s.Id) + 1 : 1;
            
            // Auto-set Date
            sale.Date = DateTime.Now;

            // Auto-calculate Total
            sale.Total = sale.QuantityKg * sale.PricePerKg;

            _sales.Add(sale);

            return RedirectToAction(nameof(Index));
        }
    }
}
