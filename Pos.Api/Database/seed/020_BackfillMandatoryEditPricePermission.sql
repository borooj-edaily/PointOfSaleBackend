-- Backfill: edit_price (Price Override) is now also a mandatory permission
-- for the Cashier role, alongside process_return (see 019 and
-- RolePermissions.MandatoryByRole in code). New/edited users get it
-- automatically going forward; this heals Cashier accounts created/edited
-- before this change. Safe to re-run.

INSERT IGNORE INTO UserPermissions (UserId, PermissionId)
SELECT u.Id, p.Id
FROM Users u
JOIN Permissions p ON p.Name = 'edit_price'
WHERE u.Role = 'Cashier'
  AND NOT EXISTS (
      SELECT 1 FROM UserPermissions up
      WHERE up.UserId = u.Id AND up.PermissionId = p.Id
  );