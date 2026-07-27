-- Bootstrap admin account so someone can log in for the first time.
-- IMPORTANT: 'REPLACE_WITH_REAL_BCRYPT_HASH' below is NOT a usable hash.
-- Generate a real one once BCrypt.Net-Next is installed: temporarily add this
-- line anywhere early in Program.cs, run the API once, copy the printed hash
-- into this file below, then delete that line again:
--
--   Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("Admin@123"));
--
-- Change this password after the first real login.

INSERT INTO Users (FullName, Username, PasswordHash, Role, IsActive) VALUES
('System Admin', 'admin', 'REPLACE_WITH_REAL_BCRYPT_HASH', 'Admin', TRUE);

-- Give the admin every permission that exists
INSERT INTO UserPermissions (UserId, PermissionId)
SELECT (SELECT Id FROM Users WHERE Username = 'admin'), Id FROM Permissions;
