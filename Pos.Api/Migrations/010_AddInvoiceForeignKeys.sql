-- Migration: 010_AddInvoiceForeignKeys
-- Run this ONLY after Users (Person A) and Products (Person B) tables exist.
-- Coordinate the exact number (010) with the team so it doesn't collide with
-- another migration created around the same time.

ALTER TABLE Invoices
    ADD CONSTRAINT FK_Invoices_Users
    FOREIGN KEY (CashierId) REFERENCES Users(Id);

ALTER TABLE InvoiceItems
    ADD CONSTRAINT FK_InvoiceItems_Products
    FOREIGN KEY (ProductId) REFERENCES Products(Id);

ALTER TABLE InvoiceReturns
    ADD CONSTRAINT FK_InvoiceReturns_Users
    FOREIGN KEY (ProcessedBy) REFERENCES Users(Id);

ALTER TABLE InvoiceReturns
    ADD CONSTRAINT FK_InvoiceReturns_Products
    FOREIGN KEY (ReplacementProductId) REFERENCES Products(Id);
