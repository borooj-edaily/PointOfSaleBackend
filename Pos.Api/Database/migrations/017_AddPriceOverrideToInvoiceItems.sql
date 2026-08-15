-- Migration: 017_AddPriceOverrideToInvoiceItems
-- Feature: Price Override — a cashier holding the edit_price permission can
-- override a single line's unit price at checkout time (e.g. damaged item,
-- loyal-customer discount) without going through Admin > Products first.
--
-- OriginalUnitPrice is only populated when a line WAS overridden; it stores
-- what the catalog price would otherwise have been, purely for audit/reporting
-- (so "how much discount did we give away via overrides" is answerable later).
-- UnitPriceSnapshot keeps being the price actually charged, exactly as before,
-- so every other query/report that already reads UnitPriceSnapshot keeps working
-- unchanged.

ALTER TABLE InvoiceItems
    ADD COLUMN OriginalUnitPrice   DECIMAL(12,2) NULL,
    ADD COLUMN PriceOverrideReason VARCHAR(255)  NULL;