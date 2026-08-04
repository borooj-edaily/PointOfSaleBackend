-- Bootstrap admin account so someone can log in for the first time.
-- TEST/DEV ONLY credentials — change this password after the first real login:
--   Username: admin
--   Password: 123456

INSERT INTO Users (FullName, Username, PasswordHash, Role, IsActive) VALUES
('System Admin', 'admin', '$2a$11$2.rKTdEy0FODC6dL2KGJfONHGes3PjCguF6nRbJpAPsm0NlcUGy0.', 'Admin', TRUE);

-- Give the admin every permission that exists
INSERT INTO UserPermissions (UserId, PermissionId)
SELECT (SELECT Id FROM Users WHERE Username = 'admin'), Id FROM Permissions;