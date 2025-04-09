PRINT 'Starting Product Constraint Checks...';
GO

PRINT 'Checking for negative Product Quantities...';
IF EXISTS (SELECT 1 FROM dbo.Products WHERE Quantity < 0)
BEGIN
    PRINT 'FAIL: Negative product quantities found!';
    SELECT ProductId, Name, Quantity FROM dbo.Products WHERE Quantity < 0;
    RAISERROR('Data Integrity Check Failed: Negative product quantities exist.', 16, 1);
END
ELSE
BEGIN
    PRINT 'PASS: No negative product quantities found.';
END
GO

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

PRINT 'Product Constraint Checks Completed.';
GO
