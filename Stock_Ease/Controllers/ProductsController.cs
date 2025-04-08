using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stock_Ease.Models;
using Stock_Ease.Data;

namespace Stock_Ease.Controllers
{

    public class ProductsController(Stock_EaseContext context, AlertsController alertsController) : Controller
    {
        private readonly Stock_EaseContext _context = context;
        private readonly AlertsController _alertsController = alertsController;
        private const int LowStockThreshold = 10;


        public async Task<IActionResult> Index()
        {

            ViewData["LowStockThreshold"] = LowStockThreshold;
            return View(await _context.Products.ToListAsync());
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }


            ViewData["LowStockThreshold"] = LowStockThreshold;
            return View(product);
        }


        public IActionResult Create()
        {

            if (TempData["InitialBarcode"] != null)
            {
                ViewData["InitialBarcode"] = TempData["InitialBarcode"];
            }
            return View();
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Name,Barcode,Quantity,MinimumThreshold,ExpiryDate")] Product product)
        {

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                ModelState.AddModelError("Name", "Product name is required.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();


                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbEx)
                {


                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator. Error: " + dbEx.Message);

                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error creating product: {ex.Message}");
                    ModelState.AddModelError("", "An unexpected error occurred. Error: " + ex.Message);
                }
            }


            return View(product);
        }



        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Barcode,Quantity,MinimumThreshold,ExpiryDate")] Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    await _alertsController.CheckAndCreateLowStockAlert(product.ProductId);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
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
            return View(product);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
