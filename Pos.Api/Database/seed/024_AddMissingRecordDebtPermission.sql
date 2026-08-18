-- Patch: some local databases were seeded from 001_SeedPermissions.sql
-- before 'record_debt' was added to that file, so the Permissions table
-- is missing this row. INSERT IGNORE makes this safe to re-run even if
-- the row already exists (Permissions.Name is UNIQUE).

INSERT IGNORE INTO Permissions (Name, Description) VALUES
('record_debt', 'Record an invoice as deferred payment (debt notebook) and mark debts as paid');
