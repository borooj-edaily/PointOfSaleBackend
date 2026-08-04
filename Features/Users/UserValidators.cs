using FluentValidation;

namespace Pos.Api.Features.Users;

internal static class UserRoles
{
    public static readonly string[] All = { "Admin", "Cashier", "InventoryOnly", "Custom" };
}

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.Role).Must(UserRoles.All.Contains).WithMessage("Invalid user role.");
        RuleForEach(x => x.PermissionIds).GreaterThan(0);
    }
}

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Role).Must(UserRoles.All.Contains).WithMessage("Invalid user role.");
        RuleFor(x => x.NewPassword).MinimumLength(8).MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
    }
}

public sealed class SetUserPermissionsValidator : AbstractValidator<SetUserPermissionsCommand>
{
    public SetUserPermissionsValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleForEach(x => x.PermissionIds).GreaterThan(0);
    }
}

public sealed class SetUserActiveValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

