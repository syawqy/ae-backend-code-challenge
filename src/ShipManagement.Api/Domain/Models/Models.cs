namespace ShipManagement.Api.Domain.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class Ship
{
    public string ShipCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FiscalYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserShipAssignment
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ShipCode { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

public class CrewListItem
{
    public string RankName { get; set; } = string.Empty;
    public string CrewMemberId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string SignOnDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalCount { get; set; }
}

public class FinancialReportDetailItem
{
    public string COADescription { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public int HierarchyLevel { get; set; }
    public string? ParentAccountNumber { get; set; }
    public decimal? Actual { get; set; }
    public decimal? Budget { get; set; }
    public decimal? VarianceActual { get; set; }
    public decimal? ActualYTD { get; set; }
    public decimal? BudgetYTD { get; set; }
    public decimal? VarianceYTD { get; set; }
}

public class FinancialReportSummaryItem
{
    public string ShipCode { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public string FiscalYear { get; set; } = string.Empty;
    public string AccountPeriod { get; set; } = string.Empty;
    public decimal MonthlyActual { get; set; }
    public decimal MonthlyBudget { get; set; }
    public decimal MonthlyVariance { get; set; }
    public decimal CumulativeActualYTD { get; set; }
    public decimal CumulativeBudgetYTD { get; set; }
    public decimal CumulativeVarianceYTD { get; set; }
}
