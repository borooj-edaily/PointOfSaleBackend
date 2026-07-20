-- Migration: 001_CreateInvoicesTable
-- Owner: Person C (Invoices module)
-- NOTE: CashierId is NOT a foreign key yet because the Users table (Person A)
-- may not exist when this migration runs. Once Users exists, add the FK in a
-- later, separately numbered migration (see 010_AddInvoiceForeignKeys.sql).

CREATE TABLE IF NOT EXISTS Invoices (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    InvoiceNumber   INT NOT NULL,
    CashierId       INT NOT NULL,
    HasReturn       TINYINT(1) NOT NULL DEFAULT 0,
    Subtotal        DECIMAL(12,2) NOT NULL,
    DiscountType    VARCHAR(20) NULL,       -- 'fixed' | 'percentage' | NULL
    DiscountValue   DECIMAL(12,2) NULL,
    Total           DECIMAL(12,2) NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Invoices_InvoiceNumber UNIQUE (InvoiceNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
