using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stock_Ease.Models;
using Stock_Ease.Data;
using Microsoft.AspNetCore.SignalR;
using Stock_Ease.Hubs;

namespace Stock_Ease.Controllers
{

    public class AlertsController(Stock_EaseContext context) : Controller
    {
        private readonly Stock_EaseContext _context = context;


        // Index: Show only unread alerts.
        public async Task<IActionResult> Index()
        {
            var stock_EaseContext = _context.Alerts
                                            .Include(a => a.Product)
                                            .Where(a => !a.IsRead)
                                            .OrderByDescending(a => a.AlertDate);
            return View(await stock_EaseContext.ToListAsync());
        }

        // History: Show read alerts.
        public async Task<IActionResult> History()
        {
            var stock_EaseContext = _context.Alerts
                                            .Include(a => a.Product)
                                            .Where(a => a.IsRead)
                                            .OrderByDescending(a => a.AlertDate);
            ViewData["Title"] = "Alert History";
            return View(await stock_EaseContext.ToListAsync());
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alert = await _context.Alerts
                .Include(a => a.Product)
                .FirstOrDefaultAsync(m => m.AlertId == id);
            if (alert == null)
            {
                return NotFound();
            }

            return View(alert);
        }


        public IActionResult Create()
        {

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
            return View();
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AlertId,ProductId,Message,AlertDate")] Alert alert)
        {
            if (ModelState.IsValid)
            {
                _context.Add(alert);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", alert.ProductId);
            return View(alert);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null)
            {
                return NotFound();
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", alert.ProductId);
            return View(alert);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AlertId,ProductId,Message,AlertDate")] Alert alert)
        {
            if (id != alert.AlertId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(alert);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AlertExists(alert.AlertId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", alert.ProductId);
            return View(alert);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alert = await _context.Alerts
                .Include(a => a.Product)
                .FirstOrDefaultAsync(m => m.AlertId == id);
            if (alert == null)
            {
                return NotFound();
            }

            return View(alert);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert != null)
            {
                _context.Alerts.Remove(alert);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AlertExists(int id)
        {
            return _context.Alerts.Any(e => e.AlertId == id);
        }







        public virtual async Task CheckAndCreateLowStockAlert(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product != null && product.Quantity <= product.MinimumThreshold)
            {

                bool alertExists = await _context.Alerts
                    .AnyAsync(a => a.ProductId == productId && !a.IsRead && a.Message.Contains("is low on stock"));

                if (!alertExists)
                {
                    var alert = new Alert
                    {
                        ProductId = productId,
                        Message = $"Product '{product.Name}' is low on stock (Quantity: {product.Quantity}, Threshold: {product.MinimumThreshold}).",
                        AlertDate = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Alerts.Add(alert);
                    await _context.SaveChangesAsync();

                }
            }
        }







        [HttpGet]
        [Route("api/alerts/unread")]
        public async Task<IActionResult> GetUnreadAlerts()
        {
            var unreadAlerts = await _context.Alerts
                                             .Where(a => !a.IsRead)
                                             .Include(a => a.Product)
                                             .OrderByDescending(a => a.AlertDate)
                                             .ToListAsync();

            if (unreadAlerts.Any())
            {

                foreach (var alert in unreadAlerts)
                {
                    alert.IsRead = true;
                    _context.Update(alert);
                }
                await _context.SaveChangesAsync();
            }


            return Json(unreadAlerts);
        }
    }
}
