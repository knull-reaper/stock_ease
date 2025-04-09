using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Stock_Ease.Controllers;
using Stock_Ease.Data;
using Stock_Ease.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory; // Add this using directive
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Stock_Ease.Services; // Add this using

namespace Stock_Ease.Tests
{
    [TestClass]
    public class ProductsControllerTests
    {
        private DbContextOptions<Stock_EaseContext> _options;
        private Stock_EaseContext _context;
        private Mock<AlertsController> _mockAlertsController;
        private Mock<IWeightSensorStatusService> _mockSensorStatusService;
        private ProductsController _controller;

        [TestInitialize]
        public void Setup()
        {
            // Use unique database name for each test run to ensure isolation
            _options = new DbContextOptionsBuilder<Stock_EaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new Stock_EaseContext(_options);

            // Seed initial data for tests
            _context.Products.AddRange(
                new Product { ProductId = 1, Name = "Test Product 1", Quantity = 10, MinimumThreshold = 5 },
                new Product { ProductId = 2, Name = "Test Product 2", Quantity = 20, MinimumThreshold = 10 }
            );
            _context.SaveChanges();

            // Set up a separate in-memory database for the alerts controller
            var mockAlertsContextOptions = new DbContextOptionsBuilder<Stock_EaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString() + "_alerts")
                .Options;
            var mockAlertsContext = new Stock_EaseContext(mockAlertsContextOptions);
            _mockAlertsController = new Mock<AlertsController>(mockAlertsContext);

            _mockAlertsController.Setup(ac => ac.CheckAndCreateLowStockAlert(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            // Mock the sensor status service
            _mockSensorStatusService = new Mock<IWeightSensorStatusService>();
            _mockSensorStatusService.Setup(s => s.GetActiveSensors(It.IsAny<TimeSpan>()))
                                    .Returns(new List<SensorStatus>());

            // Instantiate the controller with the required mocked services
            _controller = new ProductsController(_context, _mockAlertsController.Object, _mockSensorStatusService.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task Index_ReturnsViewResult_WithListOfProducts()
        {
            var result = await _controller.Index();

            // Assert: Check if the result is a ViewResult
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result; // Direct cast after type assertion

            // Assert: Check if the model is not null and is of the expected type
            Assert.IsNotNull(viewResult.Model, "The view model should not be null.");
            Assert.IsInstanceOfType(viewResult.Model, typeof(List<Product>));
            var model = (List<Product>)viewResult.Model; // Direct cast after type assertion

            // Assert: Check the count of products in the model
            Assert.AreEqual(2, model.Count);
        }

        [TestMethod]
        public async Task Details_ReturnsNotFoundResult_WhenIdIsNull()
        {
            var result = await _controller.Details(null);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_ReturnsNotFoundResult_WhenProductNotFound()
        {
            var result = await _controller.Details(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_ReturnsViewResult_WithProduct_WhenProductFound()
        {
            int testProductId = 1;

            var result = await _controller.Details(testProductId);

            // Assert: Check if the result is a ViewResult
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result; // Direct cast

            // Assert: Check if the model is not null and is of the expected type
            Assert.IsNotNull(viewResult.Model, "The view model should not be null.");
            Assert.IsInstanceOfType(viewResult.Model, typeof(Product));
            var model = (Product)viewResult.Model; // Direct cast

            // Assert: Check the ProductId
            Assert.AreEqual(testProductId, model.ProductId);
        }

        [TestMethod]
        public async Task CreatePost_ReturnsRedirectToAction_WhenModelStateIsValid()
        {
            var newProduct = new Product { Name = "New Product 3", Quantity = 5, MinimumThreshold = 2 };

            var result = await _controller.Create(newProduct);

            // Assert: Check if the result is a RedirectToActionResult
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result; // Direct cast

            // Assert: Check the action name
            Assert.AreEqual("Index", redirectResult.ActionName);

            // Assert: Check if the product was added to the context
            var addedProduct = await _context.Products.FirstOrDefaultAsync(p => p.Name == "New Product 3");
            Assert.IsNotNull(addedProduct);
            Assert.AreEqual(5, addedProduct.Quantity);
        }

        [TestMethod]
        public async Task CreatePost_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            var newProduct = new Product { Quantity = 5, MinimumThreshold = 2 };
            _controller.ModelState.AddModelError("Name", "Product name is required.");

            var result = await _controller.Create(newProduct);

            // Assert: Check if the result is a ViewResult
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result; // Direct cast

            // Assert: Check if the model is not null and matches the input
            Assert.IsNotNull(viewResult.Model, "The view model should not be null.");
            Assert.AreEqual(newProduct, viewResult.Model);
        }

        [TestMethod]
        public async Task CreatePost_ReturnsViewResult_WhenNameIsEmpty()
        {
            var newProduct = new Product { Name = "", Quantity = 5, MinimumThreshold = 2 };
            _controller.ModelState.AddModelError("Name", "Product name is required.");

            var result = await _controller.Create(newProduct);

            // Assert: Check if the result is a ViewResult
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result; // Direct cast

            // Assert: Check if the model is not null and matches the input
            Assert.IsNotNull(viewResult.Model, "The view model should not be null.");
            Assert.AreEqual(newProduct, viewResult.Model);

            // Assert: Check ModelState for the specific error
            Assert.IsTrue(_controller.ModelState.ContainsKey("Name"));
            Assert.AreEqual("Product name is required.", _controller.ModelState["Name"].Errors[0].ErrorMessage);
        }

        [TestMethod]
        public async Task EditPost_ReturnsRedirectToAction_WhenModelStateIsValid()
        {
            int testProductId = 1;
            var productToUpdate = await _context.Products.FindAsync(testProductId);
            Assert.IsNotNull(productToUpdate);
            productToUpdate.Name = "Updated Product 1";
            productToUpdate.Quantity = 15;

            var result = await _controller.Edit(testProductId, productToUpdate);

            // Assert: Check if the result is a RedirectToActionResult
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result; // Direct cast

            // Assert: Check the action name
            Assert.AreEqual("Index", redirectResult.ActionName);

            // Assert: Check if the product was updated in the context
            var updatedProduct = await _context.Products.FindAsync(testProductId);
            Assert.AreEqual("Updated Product 1", updatedProduct.Name);
            Assert.AreEqual(15, updatedProduct.Quantity);
        }

        [TestMethod]
        public async Task EditPost_ReturnsNotFoundResult_WhenIdMismatch()
        {
            int testProductId = 1;
            var productToUpdate = new Product { ProductId = 999, Name = "Mismatch Product", Quantity = 10 };

            var result = await _controller.Edit(testProductId, productToUpdate);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task EditPost_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            int testProductId = 1;
            var productToUpdate = await _context.Products.FindAsync(testProductId);
            Assert.IsNotNull(productToUpdate);
            _controller.ModelState.AddModelError("Name", "Some error");

            var result = await _controller.Edit(testProductId, productToUpdate);

            // Assert: Check if the result is a ViewResult
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result; // Direct cast

            // Assert: Check if the model is not null and matches the input
            Assert.IsNotNull(viewResult.Model, "The view model should not be null.");
            Assert.AreEqual(productToUpdate, viewResult.Model);
        }

        [TestMethod]
        public async Task DeleteConfirmed_ReturnsRedirectToAction_WhenProductExists()
        {
            int testProductId = 1;
            Assert.IsNotNull(await _context.Products.FindAsync(testProductId));

            var result = await _controller.DeleteConfirmed(testProductId);

            // Assert: Check if the result is a RedirectToActionResult
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result; // Direct cast

            // Assert: Check the action name
            Assert.AreEqual("Index", redirectResult.ActionName);

            // Assert: Check if the product was deleted from the context
            Assert.IsNull(await _context.Products.FindAsync(testProductId));
        }

        [TestMethod]
        public async Task DeleteConfirmed_ReturnsRedirectToAction_WhenProductDoesNotExist()
        {
            int nonExistentId = 999;
            Assert.IsNull(await _context.Products.FindAsync(nonExistentId));

            var result = await _controller.DeleteConfirmed(nonExistentId);

            // Assert: Check if the result is a RedirectToActionResult
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result; // Direct cast

            // Assert: Check the action name
            Assert.AreEqual("Index", redirectResult.ActionName);
        }
    }
}
