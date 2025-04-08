-- T-SQL Script to Validate Product Data Integrity in Stock_Ease Database

-- Ensure the Stock_Ease database context is used
-- USE stock_ease; -- Uncomment if running manually outside of a specific context tool

PRINT 'Starting Product Constraint Checks...';
GO

-- Check 1: Ensure Product Quantity is not negative
PRINT 'Checking for negative Product Quantities...';
IF EXISTS (SELECT 1 FROM dbo.Products WHERE Quantity < 0)
BEGIN
    PRINT 'FAIL: Negative product quantities found!';
    -- Optionally, select the offending rows
    SELECT ProductId, Name, Quantity FROM dbo.Products WHERE Quantity < 0;
    -- Raise an error to potentially stop a deployment pipeline or alert administrators
    RAISERROR('Data Integrity Check Failed: Negative product quantities exist.', 16, 1);
END
ELSE
BEGIN
    PRINT 'PASS: No negative product quantities found.';
END
GO

-- Check 2: Ensure MinimumThreshold is not negative
PRINT 'Checking for negative Minimum Thresholds...';
IF EXISTS (SELECT 1 FROM dbo.Products WHERE MinimumThreshold < 0)
BEGIN
    PRINT 'FAIL: Negative minimum thresholds found!';
    SELECT ProductId, Name, MinimumThreshold FROM dbo.Products WHERE MinimumThreshold < 0;
    RAISERROR('Data Integrity Check Failed: Negative minimum thresholds exist.', 16, 1);
END
ELSE
BEGIN
    PRINT 'PASS: No negative minimum thresholds found.';
END
GO

-- Check 3: (Example) Ensure Product Name is not empty or just whitespace
PRINT 'Checking for empty or whitespace Product Names...';
IF EXISTS (SELECT 1 FROM dbo.Products WHERE LTRIM(RTRIM(Name)) = '')
BEGIN
    PRINT 'FAIL: Empty or whitespace-only product names found!';
    SELECT ProductId, Name FROM dbo.Products WHERE LTRIM(RTRIM(Name)) = '';
    RAISERROR('Data Integrity Check Failed: Empty or whitespace product names exist.', 16, 1);
END
ELSE
BEGIN
    PRINT 'PASS: No empty or whitespace-only product names found.';
END
GO

-- Add more checks as needed (e.g., foreign key consistency if not enforced by constraints, specific business rules)

PRINT 'Product Constraint Checks Completed.';
GO
