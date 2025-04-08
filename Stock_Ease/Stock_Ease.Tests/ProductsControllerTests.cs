using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Stock_Ease.Controllers;
using Stock_Ease.Data;
using Stock_Ease.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Stock_Ease.Tests
{
    [TestClass]
    public class ProductsControllerTests
    {
        private DbContextOptions<Stock_EaseContext> _options;
        private Stock_EaseContext _context;
        private Mock<AlertsController> _mockAlertsController;
        private ProductsController _controller;

        [TestInitialize]
        public void Setup()
        {
            // Use unique database name for each test run to ensure isolation
            _options = new DbContextOptionsBuilder<Stock_EaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new Stock_EaseContext(_options);

            // Seed initial data if needed for specific tests
            _context.Products.AddRange(
                new Product { ProductId = 1, Name = "Test Product 1", Quantity = 10, MinimumThreshold = 5 },
                new Product { ProductId = 2, Name = "Test Product 2", Quantity = 20, MinimumThreshold = 10 }
            );
            _context.SaveChanges();

            // Mock AlertsController dependency
            // We need a way to instantiate AlertsController or mock its dependencies too.
            // For simplicity here, let's assume AlertsController has a parameterless constructor
            // or we mock its dependencies if needed. A better approach might be an interface.
            // Since AlertsController itself has dependencies, mocking it directly is complex without an interface.
            // Let's mock its core method CheckAndCreateLowStockAlert directly.
            // We need a concrete instance for the constructor, so we'll mock its context dependency.
            var mockAlertsContextOptions = new DbContextOptionsBuilder<Stock_EaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString() + "_alerts") // Separate DB for alerts mock
                .Options;
            var mockAlertsContext = new Stock_EaseContext(mockAlertsContextOptions);
            _mockAlertsController = new Mock<AlertsController>(mockAlertsContext); // Pass mock context

            // Setup the specific method we need for ProductsController
             _mockAlertsController.Setup(ac => ac.CheckAndCreateLowStockAlert(It.IsAny<int>())).Returns(Task.CompletedTask);


            _controller = new ProductsController(_context, _mockAlertsController.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted(); // Clean up the in-memory database
            _context.Dispose();
        }

        [TestMethod]
        public async Task Index_ReturnsViewResult_WithListOfProducts()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = result as ViewResult;
            Assert.IsInstanceOfType(viewResult.Model, typeof(List<Product>));
            var model = viewResult.Model as List<Product>;
            Assert.AreEqual(2, model.Count); // Based on seeded data
        }

        [TestMethod]
        public async Task Details_ReturnsNotFoundResult_WhenIdIsNull()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_ReturnsNotFoundResult_WhenProductNotFound()
        {
            // Act
            var result = await _controller.Details(999); // Non-existent ID

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_ReturnsViewResult_WithProduct_WhenProductFound()
        {
            // Arrange
            int testProductId = 1;

            // Act
            var result = await _controller.Details(testProductId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = result as ViewResult;
            Assert.IsInstanceOfType(viewResult.Model, typeof(Product));
            var model = viewResult.Model as Product;
            Assert.AreEqual(testProductId, model.ProductId);
        }

        [TestMethod]
        public async Task CreatePost_ReturnsRedirectToAction_WhenModelStateIsValid()
        {
            // Arrange
            var newProduct = new Product { Name = "New Product 3", Quantity = 5, MinimumThreshold = 2 };

            // Act
            var result = await _controller.Create(newProduct);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("Index", redirectResult.ActionName);

            // Verify product was added
            var addedProduct = await _context.Products.FirstOrDefaultAsync(p => p.Name == "New Product 3");
            Assert.IsNotNull(addedProduct);
            Assert.AreEqual(5, addedProduct.Quantity);
        }

        [TestMethod]
        public async Task CreatePost_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            // Arrange
            var newProduct = new Product { Quantity = 5, MinimumThreshold = 2 }; // Missing Name
            _controller.ModelState.AddModelError("Name", "Product name is required."); // Simulate invalid state

            // Act
            var result = await _controller.Create(newProduct);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = result as ViewResult;
            Assert.AreEqual(newProduct, viewResult.Model); // Should return the same invalid model
        }

         [TestMethod]
        public async Task CreatePost_ReturnsViewResult_WhenNameIsEmpty()
        {
            // Arrange
            var newProduct = new Product { Name = "", Quantity = 5, MinimumThreshold = 2 }; // Empty Name

            // Act
            var result = await _controller.Create(newProduct);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = result as ViewResult;
            Assert.AreEqual(newProduct, viewResult.Model);
            Assert.IsTrue(_controller.ModelState.ContainsKey("Name"));
            Assert.AreEqual("Product name is required.", _controller.ModelState["Name"].Errors[0].ErrorMessage);
        }


        [TestMethod]
        public async Task EditPost_ReturnsRedirectToAction_WhenModelStateIsValid()
        {
            // Arrange
            int testProductId = 1;
            var productToUpdate = await _context.Products.FindAsync(testProductId);
            Assert.IsNotNull(productToUpdate);
            productToUpdate.Name = "Updated Product 1";
            productToUpdate.Quantity = 15;

            // Act
            var result = await _controller.Edit(testProductId, productToUpdate);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("Index", redirectResult.ActionName);

            // Verify update and alert check
            var updatedProduct = await _context.Products.FindAsync(testProductId);
            Assert.AreEqual("Updated Product 1", updatedProduct.Name);
            Assert.AreEqual(15, updatedProduct.Quantity);
            _mockAlertsController.Verify(ac => ac.CheckAndCreateLowStockAlert(testProductId), Times.Once);
        }

        [TestMethod]
        public async Task EditPost_ReturnsNotFoundResult_WhenIdMismatch()
        {
            // Arrange
            int testProductId = 1;
            var productToUpdate = new Product { ProductId = 999, Name = "Mismatch Product", Quantity = 10 }; // Different ID

            // Act
            var result = await _controller.Edit(testProductId, productToUpdate);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

         [TestMethod]
        public async Task EditPost_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            // Arrange
            int testProductId = 1;
            var productToUpdate = await _context.Products.FindAsync(testProductId);
            Assert.IsNotNull(productToUpdate);
             _controller.ModelState.AddModelError("Name", "Some error"); // Simulate invalid state

            // Act
            var result = await _controller.Edit(testProductId, productToUpdate);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
             var viewResult = result as ViewResult;
            Assert.AreEqual(productToUpdate, viewResult.Model);
        }


        [TestMethod]
        public async Task DeleteConfirmed_ReturnsRedirectToAction_WhenProductExists()
        {
            // Arrange
            int testProductId = 1;
            Assert.IsNotNull(await _context.Products.FindAsync(testProductId)); // Ensure it exists

            // Act
            var result = await _controller.DeleteConfirmed(testProductId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("Index", redirectResult.ActionName);

            // Verify deletion
            Assert.IsNull(await _context.Products.FindAsync(testProductId));
        }

         [TestMethod]
        public async Task DeleteConfirmed_ReturnsRedirectToAction_WhenProductDoesNotExist()
        {
            // Arrange
            int nonExistentId = 999;
            Assert.IsNull(await _context.Products.FindAsync(nonExistentId)); // Ensure it doesn't exist

            // Act
            var result = await _controller.DeleteConfirmed(nonExistentId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult)); // Should still redirect
            var redirectResult = result as RedirectToActionResult;
            Assert.AreEqual("Index", redirectResult.ActionName);
        }
    }
}
