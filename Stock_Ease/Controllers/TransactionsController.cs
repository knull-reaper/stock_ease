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
using Microsoft.Extensions.Logging;

namespace Stock_Ease.Controllers
{

    public class TransactionsController(Stock_EaseContext context, IHubContext<TransactionHub> hubContext, AlertsController alertsController, ILogger<TransactionsController> logger) : Controller
    {
        private readonly Stock_EaseContext _context = context;
        private readonly IHubContext<TransactionHub> _hubContext = hubContext;
        private readonly AlertsController _alertsController = alertsController;
        private readonly ILogger<TransactionsController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            var stock_EaseContext = _context.Transactions.Include(t => t.Product).Include(t => t.User);
            return View(await stock_EaseContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
              .Include(t => t.Product)
              .Include(t => t.User)
              .FirstOrDefaultAsync(m => m.TransactionId == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        public IActionResult Create()
        {

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("TransactionId,UserId,ProductId,Quantity,TransactionDate")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {

                var product = await _context.Products.FindAsync(transaction.ProductId);
                if (product == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Product not found."
                    });
                }

                int quantityChange = -transaction.Quantity;
                product.Quantity += quantityChange;

                if (product.Quantity < 0)
                {

                    ModelState.AddModelError("Quantity", "Transaction quantity exceeds available stock.");

                    ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
                    ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Name", transaction.UserId);

                    var negStockErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new
                    {
                        success = false,
                        message = "Transaction quantity exceeds available stock.",
                        errors = negStockErrors
                    });
                }

                _context.Add(transaction);
                _context.Update(product);

                try
                {
                    await _context.SaveChangesAsync();

                    await _alertsController.CheckAndCreateLowStockAlert(product.ProductId);

                    await _hubContext.Clients.All.SendAsync("ReceiveTransactionUpdate", new
                    {
                        transaction,
                        product
                    });

                    return Json(new
                    {
                        success = true,
                        message = "AJAX SUCCESS: Transaction recorded via controller."
                    });
                }
                catch (Exception ex)
                {

                    _logger.LogError(ex, "Error occurred after saving transaction {TransactionId} but before completing post-save actions.", transaction.TransactionId);

                    return Json(new
                    {
                        success = false,
                        message = $ "Transaction saved, but an error occurred during post-processing: {ex.Message}"
                    });
                }
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Name", transaction.UserId);
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

            return Json(new
            {
                success = false,
                message = "Invalid data submitted.",
                errors = errors
            });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Name", transaction.UserId);
            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TransactionId,UserId,ProductId,Quantity,TransactionDate")] Transaction transaction)
        {

            if (id != transaction.TransactionId)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction ID mismatch."
                });
            }

            if (ModelState.IsValid)
            {
                try
                {

                    var originalTransaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.TransactionId == id);
                    if (originalTransaction == null) return NotFound(new
                    {
                        success = false,
                        message = "Original transaction not found."
                    });
                    int originalQuantity = originalTransaction.Quantity;

                    var product = await _context.Products.FindAsync(transaction.ProductId);
                    if (product == null) return NotFound(new
                    {
                        success = false,
                        message = "Product not found."
                    });

                    int quantityDifference = originalQuantity - transaction.Quantity;
                    product.Quantity += quantityDifference;

                    if (product.Quantity < 0)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Edit results in negative stock."
                        });
                    }

                    _context.Update(transaction);
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    await _alertsController.CheckAndCreateLowStockAlert(product.ProductId);

                    await _hubContext.Clients.All.SendAsync("ReceiveTransactionUpdate", new
                    {
                        transaction,
                        product
                    });

                    return Json(new
                    {
                        success = true,
                        message = "Transaction updated successfully."
                    });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionExists(transaction.TransactionId))
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "Transaction not found during update."
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Concurrency error during update."
                        });
                    }
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error updating transaction: {ex.Message}");
                    return Json(new
                    {
                        success = false,
                        message = "An error occurred during update."
                    });
                }
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", transaction.ProductId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Name", transaction.UserId);
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new
            {
                success = false,
                message = "Invalid data.",
                errors = errors
            });

        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
              .Include(t => t.Product)
              .Include(t => t.User)
              .FirstOrDefaultAsync(m => m.TransactionId == id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found."
                });
            }

            var product = await _context.Products.FindAsync(transaction.ProductId);
            if (product != null)
            {

                product.Quantity += transaction.Quantity;
                _context.Update(product);
                await _context.SaveChangesAsync();

                await _alertsController.CheckAndCreateLowStockAlert(product.ProductId);

            }
            else
            {

                await _context.SaveChangesAsync();
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveTransactionDeletion", new
            {
                transactionId = id,
                product
            });

            return Json(new
            {
                success = true,
                message = "Transaction deleted successfully."
            });

        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.TransactionId == id);
        }
    }
}