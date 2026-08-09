-- ============================================================
-- إنشاء مستخدم تجريبي بدور InventoryOnly (صلاحية manage_inventory بس)
-- Username: inventory1
-- Password: 123456
-- ============================================================

INSERT INTO Users (FullName, Username, PasswordHash, Role, IsActive, CreatedAt) VALUES
('Inventory Staff', 'inventory1', '$2a$11$2.rKTdEy0FODC6dL2KGJfONHGes3PjCguF6nRbJpAPsm0NlcUGy0.', 'InventoryOnly', TRUE, UTC_TIMESTAMP(6));

INSERT INTO UserPermissions (UserId, PermissionId)
SELECT
    (SELECT Id FROM Users WHERE Username = 'inventory1'),
    Id
FROM Permissions
WHERE Name = 'manage_inventory';