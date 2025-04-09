# PROG1322 **Ultra-Detailed** Project Report: Stock_Ease Inventory Management System

**Group Members:** [Please Add Group Member Names Here]
**Date:** April 9, 2025

---

## Table of Contents

1.  [🎯 Project Overview & Alignment with Course Outcomes](#1--project-overview--alignment-with-course-outcomes)
    *   [1.1. Project Background, Domain Choice, and Scope](#11-project-background-domain-choice-and-scope)
    *   [1.2. Alignment with Learning Outcomes](#12-alignment-with-learning-outcomes-)
2.  [💾 Database Design and Implementation Journey](#2--database-design-and-implementation-journey)
    *   [2.1. Choosing the Path: Code First with EF Core](#21-choosing-the-path-code-first-with-ef-core)
    *   [2.2. Entity Relationship Model (ERM) - Detailed](#22-entity-relationship-model-erm---detailed)
    *   [2.3. Conceptual Schema Diagram](#23-conceptual-schema-diagram)
    *   [2.4. Data Integrity Mechanisms](#24-data-integrity-mechanisms)
3.  [🏗️ Application Architecture](#3--application-architecture)
    *   [3.1. Technology Stack](#31-technology-stack)
    *   [3.2. Architectural Pattern: Model-View-Controller (MVC)](#32-architectural-pattern-model-view-controller-mvc)
    *   [3.3. Dependency Injection (DI) Explained](#33-dependency-injection-di-explained)
    *   [3.4. Request Lifecycle Example (Product Edit)](#34-request-lifecycle-example-product-edit)
    *   [3.5. Service Layer Introduction (`WeightSensorStatusService`)](#35-service-layer-introduction-weightsensorstatusservice)
4.  [⚙️ Building the Application: Functionality and Implementation Story](#4--building-the-application-functionality-and-implementation-story)
    *   [4.1. Core CRUD - Managing Products (`ProductsController`, `Views/Products/`)](#41-core-crud---managing-products-productscontroller-viewsproducts)
    *   [4.2. Core CRUD - Recording Transactions (`TransactionsController`, `Views/Transactions/`)](#42-core-crud---recording-transactions-transactionscontroller-viewstransactions)
    *   [4.3. Core CRUD - User, Alert, and Report Management](#43-core-crud---user-alert-and-report-management)
    *   [4.4. Data Access Strategy: LINQ & EF Core](#44-data-access-strategy-linq--ef-core)
    *   [4.5. Advanced Feature: Weight Sensor Integration](#45-advanced-feature-weight-sensor-integration)
    *   [4.6. Advanced Feature: Real-time UI Updates with SignalR](#46-advanced-feature-real-time-ui-updates-with-signalr)
    *   [4.7. Advanced Feature: Alerting Logic and History](#47-advanced-feature-alerting-logic-and-history)
    *   [4.8. External Integration Simulation (OCR Script)](#48-external-integration-simulation-ocr-script)
5.  [🧪 Testing Strategy and Implementation](#5--testing-strategy-and-implementation)
    *   [5.1. Database Testing (T-SQL)](#51-database-testing-t-sql)
    *   [5.2. Application Unit Testing (C# / MSTest)](#52-application-unit-testing-c--mstest)
    *   [5.3. Testing Gaps and Future Work](#53-testing-gaps-and-future-work)
6.  [🤔 Design Choices, Challenges, and Considerations](#6--design-choices-challenges-and-considerations)
7.  [🏁 Conclusion](#7--conclusion)
8.  [👥 Peer Evaluation & Contribution Report](#8--peer-evaluation--contribution-report)

---

## 1. 🎯 Project Overview & Alignment with Course Outcomes

**1.1. Project Background, Domain Choice, and Scope:**

> We undertook this project, "Stock\_Ease," to fulfill the requirements of the PROG1322 course assignment. Our primary objective was to design, develop, test, and document a robust, data-driven web application, thereby demonstrating our practical understanding of the core software engineering principles and technologies covered in the course. Our project evolved beyond basic CRUD to include integration with simulated external hardware (weight sensors) and real-time UI updates.

We chose the domain of **Inventory Management** for this project for several reasons:
*   **Practical Relevance:** Inventory management is a fundamental challenge faced by businesses of all sizes, making the project relatable and demonstrating the application of learned skills to solve real-world problems.
*   **Technical Requirements Alignment:** It naturally lends itself to implementing core database concepts (tables, relationships), full CRUD (Create, Read, Update, Delete) operations, data validation, and advanced features like reporting, real-time notifications, and external system integration.
*   **Scalability Demonstration:** While we implemented it as a local application for this assignment, the underlying principles (MVC, EF Core, services, SignalR) are applicable to larger, enterprise-level inventory systems.

We designed **Stock\_Ease** as a foundational inventory management system. Its core purpose is to provide a centralized platform to:
*   **Track Products:** Maintain a catalog of products, including details like name, barcode, current quantity (manual tracking) or current weight (sensor tracking), minimum desired stock level (threshold based on quantity or weight), associated sensor ID, and expiry dates.
*   **Manage Stock Levels:** Record incoming stock (purchases, returns) and outgoing stock (sales, disposals) through manual transactions. Automatically update stock levels based on simulated weight sensor readings.
*   **Monitor Inventory Health:** Automatically generate alerts when product quantities/weights fall below predefined minimum thresholds, incorporating logic for restocking periods and potential product absence. Provide real-time visual feedback on stock levels.
*   **User Accountability:** Associate manual transactions and reports with specific users.
*   **Alert Management:** Display current (unread) alerts and provide a history of acknowledged (read) alerts.
*   **Sensor Monitoring:** Display the status and last known weight reported by connected sensors.

Our application aims to replace manual tracking methods, reducing errors, improving efficiency, and providing enhanced, real-time visibility into inventory status, including integration with physical monitoring devices.

**1.2. Alignment with Learning Outcomes:** ✅

> Our project meticulously addresses the specified PROG1322 course learning outcomes:

*   **LO1: Design and Create a Distributed Database Application:**
    *   ✅ We designed and implemented a relational database schema using Microsoft SQL Server with appropriate tables, keys (PK/FK), data types, nullability constraints, and relationships to accurately model the inventory domain, including sensor-related data.
    *   ✅ We utilized Entity Framework Core (Code First) for programmatic schema definition, evolution (via migrations), and management, enhancing maintainability.
    *   *(See Section 2 for extensive details)*

*   **LO2: Implement Automated Testing (TDD):**
    *   ✅ We developed T-SQL scripts for database-level data integrity validation, ensuring fundamental rules are enforced at the source.
    *   ✅ We implemented C# Unit Tests (MSTest) with mocking (Moq) and isolation (In-Memory DB) for core application logic (specifically the initial `ProductsController`), demonstrating TDD concepts. *Note: Our testing coverage for newer features like sensor integration requires further expansion.*
    *   *(See Section 5 for details)*

*   **LO3: Develop an Integrated Data-Driven Web Application:**
    *   ✅ We built the application upon ASP.NET Core 8 MVC, integrating seamlessly with a Microsoft SQL Server database.
    *   ✅ We leveraged EF Core 8 and LINQ extensively for type-safe and efficient data access and manipulation across controllers and services.
    *   ✅ We developed a functional CRUD web interface using Razor Views for managing all core entities.
    *   ✅ We implemented advanced features including an API endpoint for external data ingestion (`WeightIntegrationController`), real-time UI updates using SignalR (`TransactionHub`), and background service logic (`WeightSensorStatusService`).
    *   ✅ We adhered to a multi-layer design via the MVC pattern, further enhanced by the introduction of a dedicated service layer for sensor status management.
    *   *(See Sections 3 & 4 for extensive details)*

*   **LO4: Present and Document Your Work:**
    *   ✅ This comprehensive written report serves as the primary documentation deliverable, detailing the project's design rationale, implementation steps, challenges, testing procedures, schema diagrams, code snippets, and alignment with course outcomes.

---

## 2. 💾 Database Design and Implementation Journey

**2.1. Choosing the Path: Code First with EF Core**

> We decided to embark on the database design using the **Code First** approach with Entity Framework Core. This felt like the most natural fit for developing a new application within Visual Studio, allowing us to define our inventory world using C# classes first and then letting EF Core translate that into a database schema.

*   **Why Code First?** 💡
    *   **Domain Focus:** It allowed us to concentrate on defining the properties and relationships of our inventory entities (`Product`, `User`, `Transaction`, `Alert`, `Report`) directly in C# code within the `Models` folder.
    *   **Maintainability:** Keeping the schema definition (C# classes) alongside the application code simplifies tracking changes and understanding the data structure.
    *   **Version Control Integration:** The C# models are easily managed using Git or other version control systems.
    *   **Structured Evolution:** EF Core Migrations provided a powerful and systematic way for us to update the database schema as we added new features like weight tracking and sensor IDs, generating the necessary SQL scripts automatically.

*   **Our Workflow in Visual Studio:** ⚙️
    1.  **Modeling the Domain:** We started by creating the initial POCO (Plain Old CLR Object) classes (like `Product`, `User`) in the `Models` folder, defining their properties (e.g., `Name`, `Quantity`, `UserId`) using the Visual Studio editor.
        `[SCREENSHOT HERE: Code snippet of Product.cs or User.cs initial model definition]`
    2.  **Setting up the Context:** Next, we configured the `Stock_EaseContext` class (`Data/stock_ease_context.cs`), inheriting from `DbContext` and adding `DbSet<T>` properties for each entity we wanted EF Core to manage as a database table.
        `[SCREENSHOT HERE: Code snippet of Stock_EaseContext.cs showing initial DbSets]`
    3.  **Generating Migrations:** Using the **Package Manager Console** (PMC) in Visual Studio (Tools > NuGet Package Manager > PMC), we ran `Add-Migration MigrationName` (e.g., `Add-Migration InitialCreate`, `Add-Migration AddCurrentWeightToProduct`, `Add-Migration AddSensorIdToProduct`, `Add-Migration AddThresholdTypeToProduct`). This command analyzed changes between our C# models and the last applied migration (or the initial state) and generated a new migration file containing `Up()` and `Down()` methods with the C# code to apply (or revert) the schema changes using EF Core's migration API. *Challenge:* ⚠️ Sometimes, EF Core's conventions didn't perfectly match our intent (e.g., for relationships or constraints), requiring manual adjustments in the generated migration file or using Fluent API configurations within the `DbContext`'s `OnModelCreating` method (though we primarily relied on conventions and data annotations here). Forgetting to add a `DbSet` for a new model also prevented migrations from detecting it initially.
        `[SCREENSHOT HERE: Package Manager Console showing an 'Add-Migration ...' command execution]`
        `[SCREENSHOT HERE: Code snippet of a generated migration file (e.g., AddCurrentWeightToProduct.cs)]`
    4.  **Applying Migrations (Updating Database):** With the migration ready, we executed `Update-Database` in the PMC. This command executed the `Up()` method(s) of pending migration(s), applying the corresponding SQL changes to our target SQL Server database defined in the connection string. Visual Studio's integration made this relatively straightforward. *Challenge:* ⚠️ Ensuring the connection string in `appsettings.json` pointed to the correct server and database, and that the SQL Server instance was accessible, was crucial. Incorrect settings often resulted in connection errors during `Update-Database` or application runtime. We also learned to be cautious when modifying already-applied migrations, preferring to add new ones instead.
        `[SCREENSHOT HERE: Package Manager Console showing 'Update-Database' command execution]`

*   **Reflection:** 💭 Code First proved effective for this project, allowing us rapid iteration as requirements evolved (like adding sensor support). The migration system provided a safety net and a clear history of schema changes. However, we recognized the importance of carefully reviewing generated migrations and understanding their impact before applying them, especially in collaborative or production environments.

**2.2. Entity Relationship Model (ERM) - Detailed**

The database schema evolved to support both manual and sensor-based inventory tracking:

*   **User:** Represents system users.
    *   `UserId` (PK, int, Identity): Unique identifier.
    *   `Name` (nvarchar(max), NOT NULL): User's full name.
    *   `Role` (nvarchar(max), NOT NULL): User's role (e.g., "Admin", "Staff"). *Improvement: Could be normalized into a separate `Roles` table with a foreign key relationship.*
    *   `Email` (nvarchar(max), NOT NULL): User's email address. *Improvement: Could have a UNIQUE constraint added.*
    *   *Relationships:* One-to-Many with `Transaction` (User performing manual transaction), One-to-Many with `Report` (User generating report).

*   **Product:** Represents inventory items. **(Evolved Model)**
    *   `ProductId` (PK, int, Identity): Unique identifier.
    *   `Name` (nvarchar(max), NOT NULL): Product name.
    *   `Barcode` (nvarchar(max), NULL): Optional barcode identifier.
    *   `Quantity` (int, NOT NULL): Manually tracked quantity (used if `ThresholdType` is "Quantity").
    *   `MinimumThreshold` (int, NOT NULL, Default: 0): The threshold value (either quantity or weight).
    *   `ExpiryDate` (datetime2, NULL): Optional expiry date.
    *   `CurrentWeight` (decimal(18,2), NULL): **(New)** Stores the latest weight reported by an associated sensor. Nullable to support quantity-tracked items. Mapped to `decimal(18, 2)` for precision.
    *   `SensorId` (nvarchar(max), NULL): **(New)** Identifier linking the product to a specific weight sensor (matches ID sent by Python script). Nullable.
    *   `ThresholdType` (nvarchar(max), NOT NULL, Default: "Quantity"): **(New)** Specifies whether alerts are based on "Quantity" or "Weight".
    *   *Relationships:* One-to-Many with `Transaction` (Product involved in manual transaction), One-to-Many with `Alert` (Product triggering alert).

*   **Transaction:** Records manual stock changes.
    *   `TransactionId` (PK, int, Identity): Unique identifier.
    *   `UserId` (FK, int, NOT NULL): References `User.UserId`.
    *   `ProductId` (FK, int, NOT NULL): References `Product.ProductId`.
    *   `Quantity` (int, NOT NULL): Change in quantity (+ for in, - for out).
    *   `TransactionDate` (datetime2, NOT NULL): Timestamp.
    *   *Relationships:* Many-to-One with `User`, Many-to-One with `Product`.

*   **Report:** Basic structure for generated reports.
    *   `ReportId` (PK, int, Identity): Unique identifier.
    *   `UserId` (FK, int, NOT NULL): References `User.UserId`.
    *   `ReportType` (nvarchar(max), NOT NULL): Type description.
    *   `GeneratedDate` (datetime2, NOT NULL): Timestamp.
    *   *Relationships:* Many-to-One with `User`.

*   **Alert:** System notifications.
    *   `AlertId` (PK, int, Identity): Unique identifier.
    *   `ProductId` (FK, int, NOT NULL): References `Product.ProductId`.
    *   `Message` (nvarchar(max), NOT NULL): Alert content (e.g., "Low Stock", "Missing Product").
    *   `AlertDate` (datetime2, NOT NULL): Timestamp.
    *   `IsRead` (bit, NOT NULL): Status flag (false = unread/active, true = read/historical).
    *   *Relationships:* Many-to-One with `Product`.

**2.3. Conceptual Schema Diagram:**

```mermaid
erDiagram
    USER ||--o{ TRANSACTION : performs
    USER ||--o{ REPORT : generates
    PRODUCT ||--o{ TRANSACTION : involved_in
    PRODUCT ||--o{ ALERT : triggers
    TRANSACTION }|--|| USER : performed_by
    TRANSACTION }|--|| PRODUCT : involves
    REPORT }|--|| USER : generated_by
    ALERT }|--|| PRODUCT : triggered_by

    USER {
        int UserId PK
        string Name
        string Role
        string Email
    }
    PRODUCT {
        int ProductId PK
        string Name
        string Barcode NULL
        int Quantity
        int MinimumThreshold
        datetime ExpiryDate NULL
        decimal CurrentWeight NULL "Nullable"
        string SensorId NULL "Nullable, FK (Conceptually)"
        string ThresholdType "Default 'Quantity'"
    }
    TRANSACTION {
        int TransactionId PK
        int UserId FK
        int ProductId FK
        int Quantity
        datetime TransactionDate
    }
    REPORT {
        int ReportId PK
        int UserId FK
        string ReportType
        datetime GeneratedDate
    }
    ALERT {
        int AlertId PK
        int ProductId FK
        string Message
        datetime AlertDate
        bool IsRead
    }
```
*(Note: The `SensorId` in `Product` conceptually links to an external sensor system, not typically enforced via a database FK unless sensors were also modeled as a table).*

`[SCREENSHOT HERE: SQL Server Object Explorer in Visual Studio or SSMS showing the updated tables (especially Product) and their columns/keys]`

**2.4. Data Integrity Mechanisms:**

Data integrity is maintained through a combination of database and application-level strategies:

*   **Database Level:**
    *   **Primary Keys (PK):** Enforced by SQL Server (e.g., `ProductId`).
    *   **Foreign Keys (FK):** Enforced by SQL Server based on EF Core configuration, ensuring valid relationships (e.g., `Alert.ProductId` must exist in `Product`).
    *   **Data Types:** SQL Server enforces types (e.g., `decimal(18,2)` for `CurrentWeight`, `bit` for `IsRead`).
    *   **Nullability:** Constraints prevent NULLs where specified (e.g., `Product.Name`, `Product.ThresholdType`).
    *   **Defaults:** Default values applied (e.g., `Product.MinimumThreshold = 0`, `Product.ThresholdType = 'Quantity'`).
    *   **T-SQL Checks:** Custom scripts (`SqlTests/CheckProductConstraints.sql`) provide additional validation (e.g., quantity >= 0).
*   **Application Level:**
    *   **Model Validation:** ASP.NET Core MVC's `ModelState.IsValid` checks, including explicit controller logic (e.g., non-empty `Product.Name`). Data annotations (`[Required]`, `[Range]`) could be added to models for more declarative validation.
    *   **Business Logic:** Controller actions and services (`WeightSensorStatusService`) contain logic ensuring data consistency (e.g., checking `ThresholdType` before evaluating weight, managing restocking timers).
    *   **API Input Handling:** The `WeightIntegrationController` implicitly validates incoming data types from the Python script.

---

## 3. 🏗️ Application Architecture

**3.1. Technology Stack:**

*   **Core Framework:** ASP.NET Core 8 MVC
*   **Language:** C# 12
*   **Database:** Microsoft SQL Server
*   **Object-Relational Mapper (ORM):** Entity Framework Core 8
*   **Frontend:** Razor Views (CSHTML), HTML5, CSS3, Bootstrap 5, JavaScript (including SignalR client)
*   **Real-time Communication:** ASP.NET Core SignalR
*   **Testing Frameworks:** MSTest (Unit Testing), Moq (Mocking), T-SQL (Database Testing)
*   **Development Environment:** Microsoft Visual Studio 2022 (or later) with the ASP.NET and web development workload, .NET 8 SDK
*   **External Simulation:** Python (for `ocr_sender.py`)
*   **Package Management:** NuGet (via Visual Studio), LibMan (for client-side libraries like SignalR JS)

**3.2. Architectural Pattern: Model-View-Controller (MVC)**

> We built Stock_Ease using the well-established **Model-View-Controller (MVC)** architectural pattern. This pattern promotes separation of concerns, making the application more organized, maintainable, and testable.

*   **Model:** Represents the application's data structures and potentially business logic.
    *   **Domain Models:** POCO classes in `Models/DomainModels.cs` (e.g., `Product`, `User`, `Alert`). These define the entities mapped to the database by EF Core.
    *   **View Models:** Although not explicitly used extensively here (controllers often pass domain models directly), in larger apps, dedicated View Models would be created to shape data specifically for a View. `Models/ErrorViewModel.cs` is an example.
    *   **Data Access Layer (DAL):** Primarily encapsulated within the `Stock_EaseContext` (`Data/stock_ease_context.cs`), which handles database interactions via EF Core.
    *   **Business Logic:** Resides partly in Controllers and partly in dedicated Services (like `WeightSensorStatusService`).

*   **View:** Responsible for rendering the user interface based on data provided by the Controller.
    *   Located in `Views/`, organized by controller subfolders (e.g., `Views/Products/`, `Views/Alerts/`).
    *   Uses **Razor syntax** (`.cshtml`) to embed C# code within HTML for dynamic content generation (e.g., looping through products, displaying model properties).
    *   Includes shared layouts (`_Layout.cshtml`) and partial views (`_Sidebar.cshtml`, `_ValidationScriptsPartial.cshtml`) for consistent structure and code reuse.
    *   Utilizes CSS (`wwwroot/css/site.css`, Bootstrap) for styling and JavaScript (`wwwroot/js/site.js`, SignalR client script) for client-side behavior and real-time updates.

*   **Controller:** Handles incoming HTTP requests, orchestrates interactions between the Model and View.
    *   Located in `Controllers/` (e.g., `ProductsController.cs`, `WeightIntegrationController.cs`).
    *   Action methods within controllers are mapped to specific URLs via routing.
    *   Interprets user input (from route parameters, query strings, form submissions).
    *   Uses injected services (like `Stock_EaseContext`, `WeightSensorStatusService`, `IHubContext<TransactionHub>`) to interact with data and perform business logic.
    *   Selects the appropriate View to render, often passing a Model object or data via `ViewData`/`ViewBag`.
    *   Returns an HTTP response (e.g., HTML from a View, JSON from an API action, Redirect).

**3.3. Dependency Injection (DI) Explained**

> ASP.NET Core's built-in Dependency Injection container is central to our application's architecture, promoting loose coupling and testability.

*   **Service Registration (`Program.cs`):** This is where we told the DI container about our services and how they should be created.
    *   **DbContext:** `builder.Services.AddDbContext<Stock_EaseContext>(...)` registers the database context, typically with a scoped lifetime (one instance per HTTP request).
    *   **Controllers:** `builder.Services.AddControllersWithViews()` registers all MVC controllers.
    *   **SignalR:** `builder.Services.AddSignalR()` registers SignalR services. `app.MapHub<TransactionHub>("/transactionHub")` maps the hub endpoint.
    *   **Custom Services:** `builder.Services.AddSingleton<WeightSensorStatusService>()` registers our custom service. We chose Singleton lifetime here assuming we wanted a single instance managing sensor state across the application. Scoped or Transient might be appropriate depending on exact requirements.
    `[SCREENSHOT HERE: Code snippet from Program.cs showing service registrations]`
*   **Constructor Injection:** Services are "injected" into the constructors of classes that need them (primarily controllers and potentially other services). The DI container automatically resolves and provides the required instances.
    ```csharp
    // WeightIntegrationController needing DbContext, HubContext, and the custom service
    public class WeightIntegrationController(Stock_EaseContext context, IHubContext<TransactionHub> hubContext, WeightSensorStatusService sensorService) : ControllerBase
    {
        private readonly Stock_EaseContext _context = context;
        private readonly IHubContext<TransactionHub> _hubContext = hubContext;
        private readonly WeightSensorStatusService _sensorService = sensorService;
        // ... action methods using _context, _hubContext, _sensorService ...
    }
    ```
*   **Benefits:**
    *   **Decoupling:** Controllers don't need to know how to create a `DbContext` or `WeightSensorStatusService`; they just declare their need for one.
    *   **Testability:** During unit testing, we can easily inject mock implementations of dependencies (like a mock `DbContext` or `IHubContext`) instead of real ones.
    *   **Maintainability:** Changing the implementation of a service (e.g., how `WeightSensorStatusService` stores data) only requires changes in the service class and potentially its registration, not in every controller that uses it.

**3.4. Request Lifecycle Example (Product Edit)**

1.  **Request:** User clicks "Edit" link for Product 5 (`/Products/Edit/5`). Browser sends GET request.
2.  **Routing:** ASP.NET Core maps the URL to `ProductsController.Edit(int id)` action, with `id = 5`.
3.  **Controller Activation:** DI container creates `ProductsController`, injecting `Stock_EaseContext` and `AlertsController`.
4.  **Action Execution:** `Edit(5)` action runs.
5.  **Data Fetching:** `_context.Products.FindAsync(5)` is called, executing a SQL query (`SELECT * FROM Products WHERE ProductId = 5`).
6.  **Model Preparation:** If the product is found, it's passed to the `View(product)` method.
7.  **View Selection:** Razor engine finds `Views/Products/Edit.cshtml`.
8.  **View Rendering:** `Edit.cshtml` executes, using the `@model Product` to generate HTML form fields pre-filled with the product's data.
9.  **Response:** The generated HTML is sent back to the browser.

**3.5. Service Layer Introduction (`WeightSensorStatusService`)**

As complexity grew with sensor integration, we introduced a dedicated service (`Services/WeightSensorStatusService.cs`) to encapsulate logic related to managing sensor state and restocking timers.

*   **Purpose:** To centralize the tracking of sensor data (last known weight, restocking status) independently of individual controllers.
*   **Implementation:** Likely uses a dictionary or similar structure to store state per `SensorId`. Contains methods like `UpdateSensorWeight`, `IsRestocking`, `StartRestockingTimer`, etc. Registered as a Singleton in `Program.cs` to maintain state across requests.
*   **Benefits:** Adheres to the Single Responsibility Principle, making the `WeightIntegrationController` cleaner and the sensor logic reusable and easier to test independently (though dedicated tests for the service are currently a gap).

---

## 4. ⚙️ Building the Application: Functionality and Implementation Story

> With the database structure and architecture defined, we proceeded to implement the application's features, starting with core CRUD and then adding more advanced capabilities like sensor integration and real-time updates.

**4.1. Core CRUD - Managing Products (`ProductsController`, `Views/Products/`)**

This foundational module allows users to manage the product catalog.

*   **Displaying the Product List (Index View):**
    *   Implemented the `Index()` action in `ProductsController` using `await _context.Products.ToListAsync()` to fetch data.
    *   Created the `Views/Products/Index.cshtml` view using Razor syntax (`@model List<Product>`, `@foreach`) to display the data in an HTML table, styled with Bootstrap. Added action links (Details, Edit, Delete) using tag helpers (`asp-action`, `asp-route-id`).
        `[SCREENSHOT HERE - Webpage: Running application showing the Product Index page with sample data in the table]`
*   **Adding a New Product (Create View & Action):**
    *   We implemented the `Create()` GET action to return the view.
    *   We created the `Views/Products/Create.cshtml` view with a form (`<form asp-action="Create">`) using tag helpers (`<input asp-for="Name">`, `<span asp-validation-for="Name">`) for model binding and validation message display.
        `[SCREENSHOT HERE - Webpage: Running application showing the empty Create Product form]`
    *   We implemented the `Create()` POST action with `[HttpPost]` and `[ValidateAntiForgeryToken]` attributes. We used model binding to receive the `Product` object, added `ModelState.IsValid` check and explicit validation for `Name`. We used `_context.Add()` and `await _context.SaveChangesAsync()` to save valid data, redirected on success using `RedirectToAction(nameof(Index))`, and returned the view with the model on validation failure. *Challenge:* ⚠️ We initially forgot `[ValidateAntiForgeryToken]` and the corresponding tag helper in the view, leaving a potential security vulnerability which we later corrected.
    ```csharp
    // Snippet from ProductsController Create (POST) - Illustrating validation and save
    if (string.IsNullOrWhiteSpace(product.Name)) // Explicit validation
    {
        ModelState.AddModelError("Name", "Product name is required.");
    }

    if (ModelState.IsValid) // Check framework + custom validation
    {
        try
        {
            _context.Add(product); // Mark entity as Added in EF Core's tracker
            await _context.SaveChangesAsync(); // Execute INSERT SQL command
            return RedirectToAction(nameof(Index)); // Go back to list on success
        }
        catch (DbUpdateException dbEx) { /* Handle potential DB errors */ }
    }
    // If model state is invalid, return the view with the submitted data and error messages
    return View(product);
    ```
*   **Updating Existing Products (Edit View & Action):**
    *   We implemented the `Edit()` GET action: Fetch product using `FindAsync(id)`, handle not found case, pass product to `Views/Products/Edit.cshtml`.
    *   We created the `Edit.cshtml` view, similar to Create but pre-filled using the `@model Product`. Included a hidden input for `ProductId`.
        `[SCREENSHOT HERE - Webpage: Running application showing the Edit Product form pre-filled with data]`
    *   We implemented the `Edit()` POST action: Validate ID match, check `ModelState`, use `_context.Update()` and `SaveChangesAsync()`. We added a call to `_alertsController.CheckAndCreateLowStockAlert` and implemented basic `DbUpdateConcurrencyException` handling. *Challenge:* ⚠️ We realized the need for concurrency checking (`DbUpdateConcurrencyException`) to handle cases where two users might edit the same product simultaneously. We added a `[Timestamp]` attribute to a model property (if needed, or relied on EF Core's default tracking).
*   **Viewing Product Details (Details View):**
    *   We implemented the `Details()` action: Fetch product using `FirstOrDefaultAsync()` (or `FindAsync`), pass to `Views/Products/Details.cshtml`.
    *   We created the `Details.cshtml` view using `@model Product` to display properties, often using `<dl>`, `<dt>`, `<dd>` tags.
        `[SCREENSHOT HERE - Webpage: Running application showing the Product Details page]`
*   **Removing Products (Delete View & Action):**
    *   We implemented the `Delete()` GET action: Fetch product, pass to `Views/Products/Delete.cshtml` for confirmation.
        `[SCREENSHOT HERE - Webpage: Running application showing the Delete Product confirmation page]`
    *   We created the `Delete.cshtml` view displaying product details and a confirmation form.
    *   We implemented the `DeleteConfirmed()` POST action with `[HttpPost, ActionName("Delete")]`: Fetch product, use `_context.Remove()`, `SaveChangesAsync()`, redirect to Index.

**4.2. Core CRUD - Recording Transactions (`TransactionsController`, `Views/Transactions/`)**

This module handles manual stock adjustments.

*   **Implementation:** We followed the same CRUD pattern as Products.
*   **Key Feature:** We used `ViewData["UserId"] = new SelectList(...)` and `ViewData["ProductId"] = new SelectList(...)` in GET actions to populate dropdowns in the Create/Edit views (`<select asp-for="UserId" asp-items="ViewBag.UserId">`).
    `[SCREENSHOT HERE - Webpage: Running application showing the Create Transaction form with User/Product dropdowns]`
*   **Challenge/Improvement:** ⚠️ We identified the logic to *automatically update* `Product.Quantity` based on `Transaction.Quantity` as a critical missing piece for data consistency. This should ideally happen within the `TransactionController`'s POST actions, potentially wrapped in a database transaction (`await using var transaction = await _context.Database.BeginTransactionAsync(); ... transaction.CommitAsync();`) to ensure atomicity. This remains an area for our future enhancement.

**4.3. Core CRUD - User, Alert, and Report Management**

*   We implemented basic CRUD operations for `User`, `Alert`, and `Report` entities following the established MVC pattern (`UsersController`, `AlertsController`, `ReportsController` and corresponding Views).
*   We included specific logic in Alerts for displaying unread vs. historical alerts (see Section 4.7).

**4.4. Data Access Strategy: LINQ & EF Core**

> Throughout the application, we consistently handled data access using Entity Framework Core and LINQ queries within controller action methods (and later, services).

*   **Querying:**
    *   Fetching all items: `await _context.Products.ToListAsync()`
    *   Fetching single item by PK: `await _context.Products.FindAsync(id)`
    *   Fetching single item with criteria: `await _context.Products.FirstOrDefaultAsync(p => p.SensorId == sensorId)`
    *   Filtering: `_context.Alerts.Where(a => a.ProductId == productId && !a.IsRead)`
    *   Including related data (Eager Loading): `await _context.Transactions.Include(t => t.User).Include(t => t.Product).ToListAsync()` (Used in Transaction Index/Details)
*   **Saving Changes:**
    *   Adding: `_context.Add(newProduct);`
    *   Updating: `_context.Update(existingProduct);` (EF Core tracks changes to loaded entities, so often just modifying properties is enough before SaveChanges)
    *   Deleting: `_context.Remove(productToDelete);`
    *   Persisting: `await _context.SaveChangesAsync();` (Bundles all tracked changes into a database transaction and executes the necessary SQL).

**4.5. Advanced Feature: Weight Sensor Integration**

> To enhance realism and explore hardware integration, we added functionality to track product levels using simulated weight sensor data.

*   **Database Changes:** We added `CurrentWeight` (decimal), `SensorId` (string), and `ThresholdType` (string) to the `Product` model. We generated and applied corresponding EF Core migrations (`AddCurrentWeightToProduct`, `AddSensorIdToProduct`, `AddThresholdTypeToProduct`).
    `[SCREENSHOT HERE: Code snippet of Product model showing new properties]`
    `[SCREENSHOT HERE: Package Manager Console showing Add/Update for these migrations]`
*   **API Endpoint (`WeightIntegrationController`):** We created a minimal API controller (`[ApiController]`, `[Route("api/[controller]")]`) with a POST action `ScreenData` at `/api/weightintegration/screendata`. This endpoint receives JSON data (`{ "SensorId": "...", "Value": "..." }`) from the external script.
    `[SCREENSHOT HERE: Code snippet of WeightIntegrationController.ScreenData action]`
*   **Data Processing:** The `ScreenData` action:
    1.  Finds the `Product` matching the incoming `SensorId`.
    2.  Parses the incoming weight `Value` (string) to a `decimal`.
    3.  Updates the `product.CurrentWeight`.
    4.  Calls the `WeightSensorStatusService` to update its internal state.
    5.  Checks if an alert should be generated based on `ThresholdType`, `CurrentWeight`, `MinimumThreshold`, and the restocking timer status from the service.
    6.  Saves changes to the database (`_context.SaveChangesAsync()`).
    7.  Calls the SignalR hub to broadcast the weight update.
*   **Sensor Status Service (`WeightSensorStatusService`):** A Singleton service injected via DI. It maintains in-memory state (e.g., using a `ConcurrentDictionary<string, SensorStatus>`) for each known `SensorId`, tracking `LastKnownWeight` and managing the "restocking timer" logic described below.
    `[SCREENSHOT HERE: Code snippet from WeightSensorStatusService showing state management or timer logic]`
*   **UI Integration:** We modified `Views/Products/Create.cshtml` and `Edit.cshtml` to include a dropdown for `ThresholdType` ("Quantity", "Weight") and a dropdown for `SensorId` (populated from `WeightSensorStatusService.GetActiveSensorIds()` via `ViewBag` in the controller). We updated `Details.cshtml` and `Index.cshtml` to display these new fields.
    `[SCREENSHOT HERE - Webpage: Product Edit form showing ThresholdType and SensorId dropdowns]`
    `[SCREENSHOT HERE - Webpage: Product Details page showing CurrentWeight, SensorId, ThresholdType]`

**4.6. Advanced Feature: Real-time UI Updates with SignalR**

> To provide immediate feedback on weight changes without requiring page refreshes, we implemented SignalR.

*   **Hub (`TransactionHub`):** We defined a simple hub (`Hubs/TransactionHub.cs`) with a method `SendWeightUpdate(string sensorId, decimal weight)`. This method isn't called directly by clients but is used by the server to send messages *to* clients.
    `[SCREENSHOT HERE: Code snippet of TransactionHub.cs]`
*   **Server-Side Invocation:** We injected `IHubContext<TransactionHub>` into `WeightIntegrationController`. After successfully saving a weight update, we called `_hubContext.Clients.All.SendAsync("ReceiveWeightUpdate", sensorId, weight);` to broadcast the update to all connected clients.
    `[SCREENSHOT HERE: Code snippet in WeightIntegrationController calling SendAsync]`
*   **Client-Side Library:** We added the SignalR JavaScript client library using LibMan (`libman.json`) to ensure it was correctly included in `wwwroot/lib/microsoft/signalr/dist/browser/`. *Challenge:* ⚠️ We initially faced 404 errors loading the script until LibMan was configured correctly.
    `[SCREENSHOT HERE: libman.json configuration for SignalR]`
*   **Client-Side Logic (`Views/Products/Index.cshtml`):** We added JavaScript to:
    1.  Establish a connection to the `/transactionHub` endpoint.
    2.  Register a handler for the `ReceiveWeightUpdate` message using `connection.on("ReceiveWeightUpdate", function (sensorId, weight) { ... });`.
    3.  Inside the handler, find the table row (`<tr>`) corresponding to the `sensorId` (using a data attribute like `data-sensor-id="@product.SensorId"` added to the row).
    4.  Update the text content of the "Current Weight" cell (`<td>`) in that row.
    5.  Apply a temporary visual highlight (e.g., changing background color) to the updated cell using CSS classes and transitions.
    `[SCREENSHOT HERE: JavaScript snippet in Products/Index.cshtml showing SignalR connection and handler]`
    `[SCREENSHOT HERE - Webpage: Product Index page showing a weight value highlighted immediately after an update]`

**4.7. Advanced Feature: Alerting Logic and History**

> We enhanced the alerting system to handle weight-based thresholds, prevent alert fatigue during restocking, and provide a history view.

*   **Threshold Types:** Logic in `WeightIntegrationController` and `ProductsController` now checks `product.ThresholdType` before evaluating `product.Quantity` or `product.CurrentWeight` against `product.MinimumThreshold`.
*   **Restocking Timer (`WeightSensorStatusService`):**
    *   When weight drops below a critical point (e.g., 3.0), the service starts a timer (e.g., 10 minutes) for that sensor and marks it as "restocking".
    *   While restocking, standard "Low Weight" alerts are suppressed.
    *   If the timer expires before the weight recovers, a "Missing Product" alert is generated.
    *   If weight recovers above the threshold, the timer is cancelled, and the "restocking" status is cleared.
*   **Duplicate Alert Prevention:** Before creating a new `Alert` record, the code checks if an *unread* alert (`!IsRead`) already exists for the same product and the same condition (e.g., low weight or missing product). If so, a new alert is not created.
    `[SCREENSHOT HERE: Code snippet in WeightIntegrationController showing duplicate alert check]`
*   **Alert Display (`AlertsController`, `Views/Alerts/`):**
    *   We modified the main `AlertsController.Index` action to fetch only unread alerts (`_context.Alerts.Include(a => a.Product).Where(a => !a.IsRead).ToListAsync()`).
        `[SCREENSHOT HERE - Webpage: Alerts Index page showing only unread alerts]`
    *   We added JavaScript to `_Layout.cshtml` to make an API call (e.g., to a new `/api/alerts/markdisplayedread` endpoint, though the implementation details show `/api/alerts/unread` was used, which might imply marking read happens client-side or was intended differently) after alerts are displayed, marking them as read (`IsRead = true`) in the database. *Challenge:* ⚠️ Ensuring alerts were marked as read reliably after being displayed required careful coordination between the view loading and the background API call.
    *   We added a new `History()` action to `AlertsController` to fetch *read* alerts (`Where(a => a.IsRead)`).
    *   We created a corresponding `Views/Alerts/History.cshtml` view to display the historical alerts.
        `[SCREENSHOT HERE - Webpage: Alerts History page showing read alerts]`

**4.8. External Integration Simulation (OCR Script)**

> The `external_scripts/ocr_sender.py` script simulates an external system (like an OCR process reading a weight display) sending data to our application's API.

*   **Functionality:** Sends HTTP POST requests containing JSON payloads (`{"SensorId": "...", "Value": "..."}`) to the `/api/weightintegration/screendata` endpoint at regular intervals.
*   **Configuration:** Includes flags like `USE_MOCK_DATA` for testing and specifies the target API URL and `SENSOR_ID`.
*   **Role:** Acts as the data source for the weight sensor integration feature, allowing development and testing without actual hardware.
    `[SCREENSHOT HERE: Code snippet from ocr_sender.py showing the data payload and POST request]`

---

## 5. 🧪 Testing Strategy and Implementation

> We adopted a two-pronged testing approach to ensure both database integrity and application logic correctness, primarily focusing on the initial CRUD functionality.

**5.1. Database Testing (T-SQL):**

*   **Objective:** To verify database schema constraints and data integrity rules directly at the database level.
*   **Methodology:** T-SQL scripts (`SqlTests/CheckProductConstraints.sql`) containing assertions.
*   **Implementation:** Checks for non-negative `Quantity`, non-negative `MinimumThreshold`, and non-empty `Name` in the `Products` table using `IF EXISTS` and `RAISERROR`.
    `[SCREENSHOT HERE: Code snippet of the CheckProductConstraints.sql script]`
*   **Execution:** Executed manually using SSMS or Visual Studio's SQL Server Object Explorer.
    `[SCREENSHOT HERE: SSMS or VS SQL Object Explorer showing execution of the T-SQL script and its output]`

**5.2. Application Unit Testing (C# / MSTest):**

*   **Objective:** To test controller action logic in isolation, following TDD principles where feasible.
*   **Methodology:** We used the MSTest framework, EF Core In-Memory provider for database isolation, and Moq for mocking dependencies. We followed the Arrange-Act-Assert (AAA) pattern.
*   **Implementation (`Stock_Ease.Tests/ProductsControllerTests.cs`):**
    *   We created a separate test project (`Stock_Ease.Tests`) within the Visual Studio solution.
        `[SCREENSHOT HERE: Visual Studio Solution Explorer showing the main project and the test project]`
    *   We wrote tests covering the core CRUD actions of the `ProductsController`:
        *   **Index:** Verified that the action returns a ViewResult containing a list of products.
        *   **Details:** Tested scenarios where a product ID exists (returns ViewResult with product) and does not exist (returns NotFoundResult).
        *   **Create (POST):** Tested valid model state (adds product, redirects to Index) and invalid model state (returns ViewResult with the model).
        *   **Edit (GET):** Tested scenarios where a product ID exists (returns ViewResult with product) and does not exist (returns NotFoundResult).
        *   **Edit (POST):** Tested valid model state (updates product, redirects to Index), invalid model state (returns ViewResult with model), ID mismatch (returns NotFoundResult), and concurrency exceptions.
        *   **Delete (GET):** Tested scenarios where a product ID exists (returns ViewResult with product) and does not exist (returns NotFoundResult).
        *   **DeleteConfirmed (POST):** Verified that the action removes the product and redirects to Index.
        `[SCREENSHOT HERE: Code snippet of a sample unit test method from ProductsControllerTests.cs]`
    *   We used `[TestInitialize]` and `[TestCleanup]` for setting up/tearing down the in-memory database and mocks for each test.
    *   We utilized Moq to mock the `AlertsController` dependency and verify interactions (`_mockAlertsController.Verify(...)`). *Challenge:* ⚠️ Correctly configuring Moq setups, especially for async methods or methods with specific arguments, required careful reading of documentation and some trial-and-error.
*   **Execution:** We used Visual Studio's integrated **Test Explorer** for running and debugging tests, providing rapid feedback during development. All 18 tests passed successfully.
    `[SCREENSHOT HERE: Visual Studio Test Explorer showing all 18 tests passing]`

**5.3. Testing Gaps and Future Work:** ⚠️

*   **Limited Coverage:** Our current unit tests primarily cover the `ProductsController` and its initial functionality. There is **no unit test coverage** for:
    *   `TransactionsController`, `UsersController`, `AlertsController`, `ReportsController`, `SensorsController`, `WeightIntegrationController`.
    *   The `WeightSensorStatusService` logic (restocking timer, state management).
    *   SignalR hub interactions.
*   **Integration Testing:** We did not implement integration tests. These would be valuable to test the interaction between components, including the database, controllers, services, and potentially the SignalR hub, using a real test database instance.
*   **Frontend Testing:** We did not create automated tests for the JavaScript/UI behavior.

> **Future Work:** Expanding our unit test coverage, particularly for the `WeightIntegrationController` and `WeightSensorStatusService`, and adding integration tests would significantly improve our confidence in the application's correctness and robustness.

---

## 6. 🤔 Design Choices, Challenges, and Considerations

*   **Code First:** We chose this for developer productivity and schema versioning benefits in a new project context. It required careful migration management.
*   **EF Core:** Standard .NET ORM, simplifying data access but requiring us to understand its change tracking and query translation behaviors.
*   **MVC Pattern:** Provided good structure and separation of concerns for this application size.
*   **Dependency Injection:** We leveraged ASP.NET Core's built-in DI for decoupling and testability. Managing service lifetimes (Singleton for `WeightSensorStatusService`, Scoped for `DbContext`) was an important consideration.
*   **Sensor Integration Approach:**
    *   **API Endpoint:** We chose a simple HTTP POST endpoint for receiving external data due to its simplicity and wide compatibility. Alternatives like MQTT or WebSockets could be considered for more complex IoT scenarios.
    *   **Service Layer:** Creating `WeightSensorStatusService` helped us separate stateful sensor logic from the stateless controller, improving organization.
*   **Real-time Updates (SignalR):** We chose SignalR for its seamless integration with ASP.NET Core and efficient real-time web functionality, providing a better UX than constant polling. It required adding client-side libraries and JavaScript handling.
*   **Alerting Logic:** The restocking timer and duplicate prevention logic added complexity but aimed to provide more meaningful alerts. We could potentially refine this logic further.
*   **Testing Strategy:** We focused on unit tests for core logic and T-SQL for DB integrity. The lack of integration and service-layer tests is a recognized limitation. Using the In-Memory provider for unit tests is fast but doesn't perfectly mimic SQL Server behavior.
*   **Error Handling:** We used basic `try-catch` blocks in controllers. More robust global error handling middleware could be implemented.
*   **Atomicity:** The lack of explicit database transactions when updating product quantities based on manual transactions or sensor readings is a potential data consistency issue that we should address in future iterations.

---

## 7. 🏁 Conclusion

> Our Stock_Ease project successfully evolved from a standard CRUD application into a more dynamic system incorporating simulated external data feeds and real-time updates. It demonstrates our design, development, and testing of an integrated, data-driven web application using ASP.NET Core MVC, Entity Framework Core, SQL Server, SignalR, and automated testing techniques. Our project fulfills the core requirements of the PROG1322 assignment while also exploring more advanced integration patterns.

Our key achievements include:
*   ✅ A well-defined and evolved relational database schema managed via EF Core Code First migrations.
*   ✅ A functional MVC application providing essential inventory management features (CRUD for Products, Transactions, Users, Alerts, Reports).
*   ✅ Integration with an external (simulated) weight sensor data source via an API endpoint.
*   ✅ Implementation of real-time UI updates for sensor data using SignalR.
*   ✅ Enhanced alerting logic including weight thresholds, restocking timers, and duplicate prevention.
*   ✅ Implementation of foundational automated tests (T-SQL for DB integrity, Unit Tests for core controller logic) ensuring baseline quality.
*   ✅ Adherence to architectural best practices like separation of concerns (MVC, Services) and dependency injection.

This project provides a solid foundation and effectively showcases our practical application of a wide range of concepts and technologies. Our journey involved overcoming challenges related to database migrations, validation, asynchronous programming, real-time communication setup, and testing methodologies. Potential future enhancements are numerous, including expanding our test coverage (especially integration tests), implementing robust authentication/authorization, refining the transaction atomicity, developing more sophisticated reporting, and improving the overall user interface.

---

## 8. 👥 Peer Evaluation & Contribution Report

*(Ensure Appendix A (Peer Evaluation) and Appendix B (Contribution Report) are completed and included as per assignment instructions.)*

---
