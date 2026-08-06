using Microsoft.AspNetCore.Authorization;

namespace Pos.Api.Security;

public static class Permissions
{
    public const string CreateInvoice = "create_invoice";
    public const string ProcessReturn = "process_return";
    public const string PrintInvoice = "print_invoice";
    public const string EditPrice = "edit_price";
    public const string ManageInventory = "manage_inventory";
    public const string ManageProducts = "manage_products";
    public const string ManageUsers = "manage_users";
    public const string ViewAllInvoices = "view_all_invoices";
    public const string ViewReports = "view_reports";
    public const string ViewAuditLog = "view_audit_log";
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole("Admin") ||
            context.User.HasClaim(
                "permission",
                requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}