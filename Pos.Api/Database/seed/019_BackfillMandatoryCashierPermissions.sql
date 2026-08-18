-- Backfill: process_return is now a mandatory permission for the Cashier
-- role (see RolePermissions.MandatoryByRole in code) — it's part of the job,
-- not an admin opt-in. New/edited users get it automatically going forward,
-- but this heals any Cashier accounts that were created/edited *before* that
-- change and are still missing it. Safe to re-run (INSERT IGNORE + NOT EXISTS
-- guard means it never duplicates rows).

INSERT IGNORE INTO UserPermissions (UserId, PermissionId)
SELECT u.Id, p.Id
FROM Users u
JOIN Permissions p ON p.Name = 'process_return'
WHERE u.Role = 'Cashier'
  AND NOT EXISTS (
      SELECT 1 FROM UserPermissions up
      WHERE up.UserId = u.Id AND up.PermissionId = p.Id
  );