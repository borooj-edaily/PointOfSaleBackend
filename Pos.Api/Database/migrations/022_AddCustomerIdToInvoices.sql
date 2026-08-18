-- Migration: 022_AddCustomerIdToInvoices
-- Feature: Dept Notebook (v2 — full customer registry, +25).
--
-- CustomerId is nullable and additive on purpose: every invoice recorded
-- under the old v1 flow (DebtorNickname only, no Customers row) keeps
-- working exactly as before. Going forward, a debt can be linked to a real
-- Customers row (CustomerId set) OR still use a quick free-text nickname
-- for a one-off/unregistered debtor (CustomerId NULL, DebtorNickname set) —
-- see FinalizeInvoice, which now accepts either.

ALTER TABLE Invoices
    ADD COLUMN CustomerId INT NULL,
    ADD CONSTRAINT FK_Invoices_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id);

CREATE INDEX IX_Invoices_CustomerId ON Invoices (CustomerId);