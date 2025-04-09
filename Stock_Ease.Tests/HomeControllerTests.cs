using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Stock_Ease.Controllers;
using Stock_Ease.Data;
using Stock_Ease.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Required for TempDataDictionary

namespace Stock_Ease.Tests
{
    [TestClass]
    public class HomeControllerTests
    {
        private DbContextOptions<Stock_EaseContext> _options;
        private Stock_EaseContext _context;
        private Mock<ILogger<HomeController>> _mockLogger;
        private HomeController _controller;

        [TestInitialize]
        public void Setup()
        {
            // Use unique database name for each test run
            _options = new DbContextOptionsBuilder<Stock_EaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new Stock_EaseContext(_options);

            // Seed initial data
            _context.Products.AddRange(
                new Product { ProductId = 1, Name = "Test Product 1", Quantity = 10, MinimumThreshold = 5, Barcode = "12345" },
                new Product { ProductId = 2, Name = "Test Product 2", Quantity = 20, MinimumThreshold = 10, Barcode = "67890" }
            );
            _context.SaveChanges();

            _mockLogger = new Mock<ILogger<HomeController>>();

            _controller = new HomeController(_mockLogger.Object, _context);

            // Mock TempData
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public void Index_ReturnsViewResult()
        {
            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task LookupProductByBarcode_ReturnsRedirectToIndex_WhenBarcodeIsEmpty()
        {
            // Act
            var result = await _controller.LookupProductByBarcode("");

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Index", redirectResult.ActionName);
            Assert.AreEqual("Barcode cannot be empty.", _controller.TempData["ErrorMessage"]);
        }

        [TestMethod]
        public async Task LookupProductByBarcode_ReturnsRedirectToProductDetails_WhenBarcodeFound()
        {
            // Arrange
            string existingBarcode = "12345";
            int expectedProductId = 1;

            // Act
            var result = await _controller.LookupProductByBarcode(existingBarcode);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Details", redirectResult.ActionName);
            Assert.AreEqual("Products", redirectResult.ControllerName);
            Assert.AreEqual(expectedProductId, redirectResult.RouteValues["id"]);
        }

        [TestMethod]
        public async Task LookupProductByBarcode_ReturnsRedirectToProductCreate_WhenBarcodeNotFound()
        {
            // Arrange
            string nonExistingBarcode = "99999";

            // Act
            var result = await _controller.LookupProductByBarcode(nonExistingBarcode);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Create", redirectResult.ActionName);
            Assert.AreEqual("Products", redirectResult.ControllerName);
            Assert.AreEqual($"Barcode '{nonExistingBarcode}' not found. Please register the product manually.", _controller.TempData["InfoMessage"]);
            Assert.AreEqual(nonExistingBarcode, _controller.TempData["InitialBarcode"]);
        }

        [TestMethod]
        public void Privacy_ReturnsViewResult()
        {
            // Act
            var result = _controller.Privacy();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void Error_ReturnsViewResultWithErrorViewModel()
        {
            // Arrange
            // Mock HttpContext for TraceIdentifier
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "test-trace-id";
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext,
            };

            // Act
            var result = _controller.Error();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.Model, typeof(ErrorViewModel));
            var model = (ErrorViewModel)viewResult.Model;
            Assert.IsNotNull(model.RequestId);
            Assert.AreEqual("test-trace-id", model.RequestId); // Check if TraceIdentifier is used
        }
    }
}
