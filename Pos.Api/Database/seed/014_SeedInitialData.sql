-- ============================================================
-- Migration: 014_SeedInitialData
-- Description: Consistent seed data across Categories / Products / StockMovements
--              CreatedByUserId = 1 is a temporary placeholder (Users table
--              does not exist yet - owned by Person A)
-- Depends on: 001, 002, 003
-- ============================================================

-- ========================================================
-- Categories
-- ========================================================
INSERT INTO Categories (Name, IsActive, CreatedByUserId) VALUES
    ('Beverages', TRUE, 1),
    ('Groceries', TRUE, 1),
    ('Cleaning Supplies', TRUE, 1);

-- ========================================================
-- Products
-- Note: CategoryId here relies on insertion order above
-- (1=Beverages, 2=Groceries, 3=Cleaning Supplies)
-- ========================================================

-- Product 1: Pepsi - sold by Piece and Package (Both)
-- One package of Pepsi = 24 pieces
INSERT INTO Products
    (Name, CategoryId, SellBy, PiecesPerPackage, PricePerPiece, PricePerPackage, StockInPieces, IsActive, CreatedByUserId)
VALUES
    ('Pepsi 330ml', 1, 3, 24, 1.50, 30.00, 240, TRUE, 1);

-- Product 2: Bottled Water - sold by Piece only
INSERT INTO Products
    (Name, CategoryId, SellBy, PiecesPerPackage, PricePerPiece, PricePerPackage, StockInPieces, IsActive, CreatedByUserId)
VALUES
    ('Bottled Water 600ml', 1, 1, NULL, 1.00, NULL, 150, TRUE, 1);

-- Product 3: Rice - sold by Package only (bag treated as the base "package" unit)
INSERT INTO Products
    (Name, CategoryId, SellBy, PiecesPerPackage, PricePerPiece, PricePerPackage, StockInPieces, IsActive, CreatedByUserId)
VALUES
    ('American Rice 5kg', 2, 2, 1, NULL, 12.50, 40, TRUE, 1);

-- Product 4: Chocolate - sold by Piece and Package (Both)
-- One package of chocolate = 12 pieces
INSERT INTO Products
    (Name, CategoryId, SellBy, PiecesPerPackage, PricePerPiece, PricePerPackage, StockInPieces, IsActive, CreatedByUserId)
VALUES
    ('Kit Kat Chocolate', 2, 3, 12, 0.75, 8.00, 96, TRUE, 1);

-- Product 5: Floor Cleaner - sold by Piece only
INSERT INTO Products
    (Name, CategoryId, SellBy, PiecesPerPackage, PricePerPiece, PricePerPackage, StockInPieces, IsActive, CreatedByUserId)
VALUES
    ('Floor Cleaner 1L', 3, 1, NULL, 4.50, NULL, 25, TRUE, 1);

-- ========================================================
-- StockMovements
-- Every product starts from a zero balance, and movements accumulate
-- until they match the final StockInPieces recorded above in Products
-- (full consistency between the two tables).
-- ProductId here relies on insertion order above
-- (1=Pepsi, 2=Water, 3=Rice, 4=Chocolate, 5=Floor Cleaner)
-- ========================================================

-- Product 1: Pepsi - Restock in two batches (0 -> 240)
INSERT INTO StockMovements (ProductId, Type, QuantityInPieces, BalanceBefore, BalanceAfter, Reason, CreatedByUserId) VALUES
    (1, 1, 168, 0,   168, NULL, 1),   -- Restock: received 7 packages (7x24)
    (1, 1, 72,  168, 240, NULL, 1);   -- Restock: received 3 additional packages (3x24)

-- Product 2: Bottled Water - single Restock (0 -> 150)
INSERT INTO StockMovements (ProductId, Type, QuantityInPieces, BalanceBefore, BalanceAfter, Reason, CreatedByUserId) VALUES
    (2, 1, 150, 0, 150, NULL, 1);

-- Product 3: Rice - Restock, then Sale, then Manual Deduction (0 -> 50 -> 45 -> 40)
INSERT INTO StockMovements (ProductId, Type, QuantityInPieces, BalanceBefore, BalanceAfter, Reason, CreatedByUserId) VALUES
    (3, 1, 50, 0,  50, NULL, 1),                                  -- Restock
    (3, 2, 5,  50, 45, NULL, 1),                                  -- Sale
    (3, 4, 5,  45, 40, 'Damaged due to warehouse moisture', 1);   -- ManualDeduction (Reason required)

-- Product 4: Chocolate - Restock, then Return from an invoice (0 -> 84 -> 96)
INSERT INTO StockMovements (ProductId, Type, QuantityInPieces, BalanceBefore, BalanceAfter, Reason, ReferenceInvoiceId, CreatedByUserId) VALUES
    (4, 1, 84, 0,  84, NULL, NULL, 1),   -- Restock: 7 packages (7x12)
    (4, 3, 12, 84, 96, NULL, 101,  1);   -- Return: full package returned from invoice #101

-- Product 5: Floor Cleaner - Restock, then Manual Addition (inventory correction) (0 -> 20 -> 25)
INSERT INTO StockMovements (ProductId, Type, QuantityInPieces, BalanceBefore, BalanceAfter, Reason, CreatedByUserId) VALUES
    (5, 1, 20, 0,  20, NULL, 1),                                              -- Restock
    (5, 5, 5,  20, 25, 'Inventory correction - unrecorded existing stock', 1); -- ManualAddition (Reason required)