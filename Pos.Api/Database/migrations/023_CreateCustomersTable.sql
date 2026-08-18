-- Migration: 021_CreateCustomersTable
-- Feature: Dept Notebook (v2 — full customer registry, +25).
--
-- This is the upgrade path the 018_AddDebtToInvoices migration comment
-- pointed at: a real customer file instead of a bare free-text nickname.
-- Phone is optional (a lot of walk-in "regulars" won't have one on file
-- day one) but Name is required so the debt notebook always has something
-- readable to show. IsActive lets a customer be retired without deleting
-- their purchase/debt history.

CREATE TABLE Customers (
    Id               INT AUTO_INCREMENT PRIMARY KEY,
    Name             VARCHAR(150) NOT NULL,
    Phone            VARCHAR(30) NULL,
    Notes            VARCHAR(500) NULL,
    IsActive         TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt        DATETIME NOT NULL,
    CreatedByUserId  INT NULL,
    UpdatedAt        DATETIME NULL,
    UpdatedByUserId  INT NULL
);

CREATE INDEX IX_Customers_Name ON Customers (Name);
CREATE INDEX IX_Customers_Phone ON Customers (Phone);