# PROG1322 Project Presentation: Stock_Ease

**(Slide 1: Title Slide)**

*   **Title:** Stock_Ease: An Inventory Management System
*   **Course:** PROG1322
*   **Group Members:** [List Names Here]
*   **Date:** April 8, 2025

---

**(Slide 2: Project Background & Goals)**

*   **Assignment Overview:** Briefly mention the PROG1322 project requirements (data-driven web app, CRUD, testing, etc.).
*   **Domain Choice:** Inventory Management System (Stock_Ease).
    *   Why this domain? (Practical, relatable, good fit for requirements).
*   **Project Goal:** To build a functional web application demonstrating proficiency in ASP.NET Core MVC, EF Core, SQL Server, and automated testing for managing product inventory.
*   **Key Features:** Product Tracking, Transaction Logging, User Management, Low-Stock Alerts, Reporting Basics.

---

**(Slide 3: Application Architecture)**

*   **Technology Stack:**
    *   ASP.NET Core 8 MVC (C#)
    *   Entity Framework Core 8 (Code First)
    *   SQL Server (Local)
    *   HTML/CSS/Bootstrap/JS
    *   MSTest / Moq / EF Core In-Memory (Unit Testing)
    *   T-SQL (Database Testing)
    *   SignalR (Real-time - Optional mention if implemented significantly)
*   **Pattern:** Model-View-Controller (MVC)
    *   Briefly explain Model (Data/Logic - `Models/`, `Data/`), View (UI - `Views/`), Controller (Input/Orchestration - `Controllers/`).
    *   Highlight how this achieves separation of concerns / layered design.

---

**(Slide 4: Database Schema)**

*   **Approach:** Code First with EF Core.
*   **Key Entities:** User, Product, Transaction, Alert, Report.
*   **(Include a Schema Diagram Here)** - *You will need to create this visually based on `Models/DomainModels.cs` and relationships.*
    *   Show tables and primary/foreign key relationships.
*   **Data Integrity:** How it's maintained (PKs, FKs, Data Types, Application Validation, T-SQL Checks).

---

**(Slide 5: Core Functionality - Product Management - Screenshot/Demo)**

*   Show the Product List (Index View).
*   Show the Create Product Form.
*   Show the Edit Product Form.
*   Show the Product Details View.
*   *(Briefly explain the CRUD operations)*

---

**(Slide 6: Core Functionality - Transaction Management - Screenshot/Demo)**

*   Show the Transaction List.
*   Show the Create Transaction Form (mention linking to User/Product).
*   *(Briefly explain the CRUD operations)*

---

**(Slide 7: Core Functionality - Alerts & Users - Screenshot/Demo)**

*   Show the Alerts List (mention `IsRead` flag).
*   Show the User List/Management screens.
*   *(Briefly explain the relevant CRUD operations)*

---

**(Slide 8: Key Code Section - Controller Example)**

*   **File:** `Controllers/ProductsController.cs`
*   **Snippet:** Show the `Edit` (POST) action.
    ```csharp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Barcode,Quantity,MinimumThreshold,ExpiryDate")] Product product)
    {
        if (id != product.ProductId) { return NotFound(); }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product); // EF Core tracks changes
                await _context.SaveChangesAsync(); // Saves to DB

                // Dependency Injection & Calling another component
                await _alertsController.CheckAndCreateLowStockAlert(product.ProductId);
            }
            catch (DbUpdateConcurrencyException) { /*...*/ }
            return RedirectToAction(nameof(Index));
        }
        return View(product); // Return view if model state is invalid
    }
    ```
*   **Explanation:** Highlight use of `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[Bind]`, `ModelState.IsValid`, EF Core `Update`/`SaveChangesAsync`, error handling, redirection, dependency injection (`_alertsController`).

---

**(Slide 9: Key Code Section - Model & DbContext)**

*   **File:** `Models/DomainModels.cs`
*   **Snippet:** Show the `Product` class definition.
    ```csharp
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Barcode { get; set; } // Nullable
        public int Quantity { get; set; }
        public int MinimumThreshold { get; set; } = 0;
        public DateTime? ExpiryDate { get; set; } // Nullable
        // Navigation Properties
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
    ```
*   **File:** `Data/stock_ease_context.cs`
*   **Snippet:** Show `DbSet<Product> Products { get; set; }`
*   **Explanation:** Explain POCOs, attributes (`[Key]`), nullability, navigation properties, `DbContext` and `DbSet` role in EF Core.

---

**(Slide 10: Testing Methodology - T-SQL)**

*   **Purpose:** Validate database integrity directly.
*   **File:** `SqlTests/CheckProductConstraints.sql`
*   **Snippet:**
    ```sql
    -- Check 1: Ensure Product Quantity is not negative
    PRINT 'Checking for negative Product Quantities...';
    IF EXISTS (SELECT 1 FROM dbo.Products WHERE Quantity < 0)
    BEGIN
        PRINT 'FAIL: Negative product quantities found!';
        RAISERROR('Data Integrity Check Failed...', 16, 1);
    END
    ELSE BEGIN PRINT 'PASS: No negative product quantities found.'; END
    GO
    ```
*   **Explanation:** Show how scripts check constraints (e.g., non-negative quantity) and report PASS/FAIL. Mention manual execution (SSMS) or potential pipeline integration.

---

**(Slide 11: Testing Methodology - Unit Testing)**

*   **Purpose:** Test application logic (Controllers) in isolation (TDD).
*   **Frameworks:** MSTest, Moq, EF Core In-Memory.
*   **File:** `Stock_Ease.Tests/ProductsControllerTests.cs`
*   **Snippet:** Show the `EditPost_ReturnsRedirectToAction_WhenModelStateIsValid` test method.
    ```csharp
    [TestMethod]
    public async Task EditPost_ReturnsRedirectToAction_WhenModelStateIsValid()
    {
        // Arrange
        int testProductId = 1;
        var productToUpdate = await _context.Products.FindAsync(testProductId);
        productToUpdate.Name = "Updated Product 1";
        // ... setup mock _mockAlertsController ...

        // Act
        var result = await _controller.Edit(testProductId, productToUpdate);

        // Assert
        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        // ... verify context changes ...
        _mockAlertsController.Verify(ac => ac.CheckAndCreateLowStockAlert(testProductId), Times.Once); // Verify mock call
    }
    ```
*   **Explanation:** Explain Arrange-Act-Assert pattern, use of In-Memory DB, Mocking (`_mockAlertsController`), Assertions (`Assert.IsInstanceOfType`), Verifying mock interactions (`_mockAlertsController.Verify`).

---

**(Slide 12: Conclusion & Future Work)**

*   **Summary:** Stock_Ease successfully meets the project requirements, demonstrating a functional CRUD application with database integration and automated testing.
*   **Learning Outcomes Met:** Briefly reiterate how LO1-LO4 were addressed.
*   **Possible Future Enhancements:**
    *   More sophisticated reporting.
    *   Improved transaction model (e.g., explicit TransactionType).
    *   Enhanced UI/UX (e.g., using a JS framework).
    *   User authentication/authorization details.
    *   Deployment to a cloud service.

---

**(Slide 13: Q&A)**

*   Open floor for questions.

---
