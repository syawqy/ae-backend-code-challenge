namespace ShipManagement.Api.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error, List<string>? errors = null) =>
        new() { Success = false, Message = error, Errors = errors ?? new List<string> { error } };
}

public class PagedResponse<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
}

// Auth
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// Users
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Ships
public class CreateShipRequest
{
    public string ShipCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FiscalYear { get; set; } = string.Empty; // e.g. 0112, 0403
    public string Status { get; set; } = "Active"; // Active | Inactive
}

public class ShipResponse
{
    public string ShipCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FiscalYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AssignShipRequest
{
    public string ShipCode { get; set; } = string.Empty;
}

public class UserShipAssignmentResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ShipCode { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

// Crew
public class CrewListQueryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortColumn { get; set; } = "SignOnDate";
    public string SortDirection { get; set; } = "ASC";
    public string? SearchTerm { get; set; }
    public DateTime? AsOfDate { get; set; }
}

public class CrewMemberResponse
{
    public string RankName { get; set; } = string.Empty;
    public string CrewMemberId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string SignOnDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// Financial
public class FinancialReportQueryRequest
{
    public string AccountPeriod { get; set; } = string.Empty; // Format: YYYY-MM
}

public class FinancialReportDetailResponse
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

public class FinancialReportSummaryResponse
{
    public string ShipCode { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public string FiscalYear { get; set; } = string.Empty;
    public string CutoffPeriod { get; set; } = string.Empty;
    public decimal TotalActualYTD { get; set; }
    public decimal TotalBudgetYTD { get; set; }
    public decimal TotalVarianceYTD { get; set; }
    public List<MonthlySummaryItemResponse> MonthlyBreakdown { get; set; } = new();
}

public class MonthlySummaryItemResponse
{
    public string AccountPeriod { get; set; } = string.Empty;
    public decimal MonthlyActual { get; set; }
    public decimal MonthlyBudget { get; set; }
    public decimal MonthlyVariance { get; set; }
    public decimal CumulativeActualYTD { get; set; }
    public decimal CumulativeBudgetYTD { get; set; }
    public decimal CumulativeVarianceYTD { get; set; }
}
