using System.Text.RegularExpressions;
using FluentValidation;
using ShipManagement.Api.DTOs;

namespace ShipManagement.Api.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full Name is required.")
            .MaximumLength(100).WithMessage("Full Name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(50).WithMessage("Role cannot exceed 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}

public class CreateShipRequestValidator : AbstractValidator<CreateShipRequest>
{
    public CreateShipRequestValidator()
    {
        RuleFor(x => x.ShipCode)
            .NotEmpty().WithMessage("ShipCode is required.")
            .MaximumLength(20).WithMessage("ShipCode cannot exceed 20 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ship name is required.")
            .MaximumLength(100).WithMessage("Ship name cannot exceed 100 characters.");

        RuleFor(x => x.FiscalYear)
            .NotEmpty().WithMessage("FiscalYear code is required.")
            .Matches(@"^[0-1][0-9][0-1][0-9]$").WithMessage("FiscalYear must be 4 digits (e.g. 0112, 0403).")
            .Must(fy =>
            {
                if (fy.Length != 4) return false;
                if (!int.TryParse(fy.Substring(0, 2), out var start)) return false;
                if (!int.TryParse(fy.Substring(2, 2), out var end)) return false;
                return start >= 1 && start <= 12 && end >= 1 && end <= 12;
            }).WithMessage("FiscalYear start and end months must be between 01 and 12.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => s is "Active" or "Inactive")
            .WithMessage("Status must be either 'Active' or 'Inactive'.");
    }
}

public class AssignShipRequestValidator : AbstractValidator<AssignShipRequest>
{
    public AssignShipRequestValidator()
    {
        RuleFor(x => x.ShipCode)
            .NotEmpty().WithMessage("ShipCode is required.")
            .MaximumLength(20).WithMessage("ShipCode cannot exceed 20 characters.");
    }
}

public class CrewListQueryRequestValidator : AbstractValidator<CrewListQueryRequest>
{
    private static readonly HashSet<string> ValidSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Rank Name", "RankName", "Crew ID", "CrewMemberId", "First Name", "FirstName",
        "Last Name", "LastName", "Age", "Nationality", "SignOnDate", "Status"
    };

    public CrewListQueryRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
        RuleFor(x => x.SortDirection)
            .Must(d => d.Equals("ASC", StringComparison.OrdinalIgnoreCase) || d.Equals("DESC", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be 'ASC' or 'DESC'.");

        RuleFor(x => x.SortColumn)
            .Must(col => string.IsNullOrWhiteSpace(col) || ValidSortColumns.Contains(col))
            .WithMessage("Invalid SortColumn. Permitted values: Rank Name, Crew ID, First Name, Last Name, Age, Nationality, SignOnDate, Status.");
    }
}

public class FinancialReportQueryValidator : AbstractValidator<FinancialReportQueryRequest>
{
    public FinancialReportQueryValidator()
    {
        RuleFor(x => x.AccountPeriod)
            .NotEmpty().WithMessage("AccountPeriod is required.")
            .Matches(@"^[1-2][0-9]{3}-(0[1-9]|1[0-2])$")
            .WithMessage("AccountPeriod must follow 'YYYY-MM' format (e.g. 2025-07).");
    }
}
