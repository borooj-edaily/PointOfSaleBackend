-- TEST/DEV ONLY — يوزر كاشير للتجربة
--   Username: cashier
--   Password: 123456

INSERT INTO Users (FullName, Username, PasswordHash, Role, IsActive) VALUES
('Cashier One', 'cashier', '$2a$11$2.rKTdEy0FODC6dL2KGJfONHGes3PjCguF6nRbJpAPsm0NlcUGy0.', 'Cashier', TRUE);

-- Bug fix: the seeded cashier had NO permissions at all, so a freshly-seeded
-- cashier account could not even open a cart or print a receipt after logging
-- in. Grant the basic day-to-day cashier-desk permissions out of the box:
-- create invoices, process returns/exchanges, and print receipts.
INSERT INTO UserPermissions (UserId, PermissionId)
SELECT
    (SELECT Id FROM Users WHERE Username = 'cashier'),
    Id
FROM Permissions
WHERE Name IN ('create_invoice', 'process_return', 'print_invoice');