-- Migration: 002_CreateInvoiceItemsTable
-- Owner: Person C (Invoices module)
-- NOTE: ProductId is NOT a foreign key yet because the Products table
-- (Person B) may not exist when this migration runs. Add the FK later
-- in 010_AddInvoiceForeignKeys.sql once Products exists.

CREATE TABLE IF NOT EXISTS InvoiceItems (
    Id                      INT AUTO_INCREMENT PRIMARY KEY,
    InvoiceId               INT NOT NULL,
    ProductId               INT NOT NULL,
    UnitSold                VARCHAR(10) NOT NULL,   -- 'piece' | 'package'
    Quantity                INT NOT NULL,
    UnitPriceSnapshot       DECIMAL(12,2) NOT NULL, -- BR-04: price frozen at sale time
    QuantityInBaseUnits     INT NOT NULL,           -- converted to pieces for stock deduction
    LineTotal               DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY (InvoiceId)
        REFERENCES Invoices(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
