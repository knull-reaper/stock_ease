using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock_Ease.Models;
using Stock_Ease.Data;

namespace Stock_Ease.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly Stock_EaseContext _context;

    public HomeController(ILogger<HomeController> logger, Stock_EaseContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LookupProductByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            TempData["ErrorMessage"] = "Barcode cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _context.Products
                                    .FirstOrDefaultAsync(p => p.Barcode == barcode);

        if (product != null)
        {

            return RedirectToAction("Details", "Products", new { id = product.ProductId });
        }
        else
        {

            TempData["InfoMessage"] = $"Barcode '{barcode}' not found. Please register the product manually.";

            TempData["InitialBarcode"] = barcode;
            return RedirectToAction("Create", "Products");
        }
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
