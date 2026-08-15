-- Migration: 018_AddDebtToInvoices
-- Feature: Debt Notebook (v1 — nickname-based, no full customer table yet).
--
-- IsDebt = 1 means the invoice was recorded as deferred payment instead of
-- being paid in cash at checkout. DebtorNickname is a free-text label (e.g.
-- "Abu Khaled") since we are deliberately NOT building a full customer
-- registry in this pass. DebtPaidAt is NULL while the debt is outstanding
-- and gets stamped once someone marks it as paid.
--
-- Kept nullable/loose on purpose: this schema is meant to be upgradeable
-- later to a real Customers table (see CustomerId columns added in a future
-- migration) without breaking anything that reads IsDebt/DebtorNickname now.

ALTER TABLE Invoices
    ADD COLUMN IsDebt         TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN DebtorNickname VARCHAR(100) NULL,
    ADD COLUMN DebtPaidAt     DATETIME NULL;

CREATE INDEX IX_Invoices_IsDebt_DebtPaidAt ON Invoices (IsDebt, DebtPaidAt);