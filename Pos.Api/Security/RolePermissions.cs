namespace Pos.Api.Security;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, string[]> AllowedByRole =
        new Dictionary<string, string[]>
        {
            ["Cashier"] = new[]
            {
                Permissions.CreateInvoice,
                Permissions.ProcessReturn,
                Permissions.ProcessExchange,
                Permissions.PrintInvoice,
                Permissions.ViewAllInvoices,
                Permissions.ViewReports,
                Permissions.EditPrice,
                Permissions.RecordDebt,
            },
            ["InventoryOnly"] = new[]
            {
                Permissions.ManageInventory,
                Permissions.ManageProducts,
                Permissions.ViewReports,
            },
        };
    // Admin و Custom بدون تقييد بالـ dictionary أعلاه...

    /// <summary>
    /// بيرجع أسماء الصلاحيات (من permissionNames) يلي مش مسموحة للدور المحدد.
    /// لو الدور مش موجود بالـ dictionary أعلاه (يعني Admin أو Custom)، ما في أي تقييد
    /// وبترجع قائمة فاضية.
    /// </summary>
    public static List<string> Disallowed(string role, IEnumerable<string> permissionNames)
    {
        if (!AllowedByRole.TryGetValue(role, out var allowed))
            return new List<string>();

        return permissionNames
            .Where(name => !allowed.Contains(name))
            .ToList();
    }
}