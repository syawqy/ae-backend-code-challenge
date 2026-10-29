using FluentAssertions;
using ShipManagement.Api.DTOs;
using ShipManagement.Api.Validators;
using Xunit;

namespace ShipManagement.Tests.ValidationTests;

public class ValidatorTests
{
    private readonly CreateShipRequestValidator _shipValidator = new();
    private readonly CreateUserRequestValidator _userValidator = new();
    private readonly CrewListQueryRequestValidator _crewQueryValidator = new();
    private readonly FinancialReportQueryValidator _financialQueryValidator = new();

    // -------------------------------------------------------------------------
    // Ship Validation
    // -------------------------------------------------------------------------
    [Theory]
    [InlineData("SHIP01", "Flying Dutchman", "0112", "Active", true)]
    [InlineData("SHIP02", "Thousand Sunny", "0403", "Active", true)]
    [InlineData("SHIP03", "Black Pearl", "0112", "Inactive", true)]
    [InlineData("", "Black Pearl", "0112", "Active", false)]           // Empty ShipCode
    [InlineData("SHIP01", "", "0112", "Active", false)]               // Empty Name
    [InlineData("SHIP01", "Ship", "1301", "Active", false)]           // Month 13 invalid
    [InlineData("SHIP01", "Ship", "0000", "Active", false)]           // Month 00 invalid
    [InlineData("SHIP01", "Ship", "0112", "Archived", false)]         // Invalid status (must be Active/Inactive)
    public void CreateShipRequest_Validation(string code, string name, string fy, string status, bool isValid)
    {
        var request = new CreateShipRequest
        {
            ShipCode = code,
            Name = name,
            FiscalYear = fy,
            Status = status
        };

        var result = _shipValidator.Validate(request);
        result.IsValid.Should().Be(isValid);
    }

    // -------------------------------------------------------------------------
    // User Validation
    // -------------------------------------------------------------------------
    [Theory]
    [InlineData("jdoe", "John Doe", "john.doe@angloeastern.com", "Admin", "Secret123!", true)]
    [InlineData("", "John Doe", "john.doe@angloeastern.com", "Admin", "Secret123!", false)]        // Empty username
    [InlineData("jdoe", "", "john.doe@angloeastern.com", "Admin", "Secret123!", false)]        // Empty fullname
    [InlineData("jdoe", "John Doe", "invalid-email", "Admin", "Secret123!", false)]             // Invalid email
    [InlineData("jdoe", "John Doe", "john.doe@angloeastern.com", "", "Secret123!", false)]        // Empty role
    [InlineData("jdoe", "John Doe", "john.doe@angloeastern.com", "Admin", "123", false)]         // Password < 6 chars
    public void CreateUserRequest_Validation(string username, string fullName, string email, string role, string pw, bool isValid)
    {
        var request = new CreateUserRequest
        {
            Username = username,
            FullName = fullName,
            Email = email,
            Role = role,
            Password = pw
        };

        var result = _userValidator.Validate(request);
        result.IsValid.Should().Be(isValid);
    }

    // -------------------------------------------------------------------------
    // Crew List Query Validation
    // -------------------------------------------------------------------------
    [Theory]
    [InlineData(1, 10, "SignOnDate", "ASC", true)]
    [InlineData(2, 50, "Rank Name", "DESC", true)]
    [InlineData(0, 10, "SignOnDate", "ASC", false)]                   // PageNumber < 1
    [InlineData(1, 0, "SignOnDate", "ASC", false)]                    // PageSize < 1
    [InlineData(1, 101, "SignOnDate", "ASC", false)]                  // PageSize > 100
    [InlineData(1, 10, "SignOnDate", "SIDEWAYS", false)]              // Invalid sort direction
    [InlineData(1, 10, "NonExistentColumn", "ASC", false)]           // Unrecognized sort column
    public void CrewListQueryRequest_Validation(int pageNum, int pageSize, string sortCol, string sortDir, bool isValid)
    {
        var request = new CrewListQueryRequest
        {
            PageNumber = pageNum,
            PageSize = pageSize,
            SortColumn = sortCol,
            SortDirection = sortDir
        };

        var result = _crewQueryValidator.Validate(request);
        result.IsValid.Should().Be(isValid);
    }

    // -------------------------------------------------------------------------
    // Financial Query Validation
    // -------------------------------------------------------------------------
    [Theory]
    [InlineData("2025-07", true)]
    [InlineData("2024-01", true)]
    [InlineData("2025-12", true)]
    [InlineData("2025-13", false)]   // Month 13 invalid
    [InlineData("2025-00", false)]   // Month 00 invalid
    [InlineData("25-07", false)]     // Year not 4 digits
    [InlineData("2025/07", false)]   // Slash delimiter invalid
    [InlineData("", false)]          // Empty
    public void FinancialReportQuery_Validation(string period, bool isValid)
    {
        var request = new FinancialReportQueryRequest
        {
            AccountPeriod = period
        };

        var result = _financialQueryValidator.Validate(request);
        result.IsValid.Should().Be(isValid);
    }
}
