using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ShipManagement.Api.Domain.Models;

namespace ShipManagement.Api.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string is missing.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}

// -----------------------------------------------------------------------------
// User Repository (Calls Stored Procedures Only)
// -----------------------------------------------------------------------------
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateAsync(string username, string fullName, string email, string role, string? passwordHash);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<User>(
            "dbo.sp_GetUsers",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            "dbo.sp_GetUserById",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            "dbo.sp_GetUserByUsername",
            new { Username = username },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<User> CreateAsync(string username, string fullName, string email, string role, string? passwordHash)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<User>(
            "dbo.sp_CreateUser",
            new
            {
                Username = username,
                FullName = fullName,
                Email = email,
                Role = role,
                PasswordHash = passwordHash
            },
            commandType: CommandType.StoredProcedure
        );
    }
}

// -----------------------------------------------------------------------------
// Ship Repository (Calls Stored Procedures Only)
// -----------------------------------------------------------------------------
public interface IShipRepository
{
    Task<IEnumerable<Ship>> GetAllAsync(string? status = null);
    Task<Ship?> GetByCodeAsync(string shipCode);
    Task<Ship> CreateAsync(string shipCode, string name, string fiscalYear, string status);
    Task<UserShipAssignment> AssignShipToUserAsync(int userId, string shipCode);
    Task<IEnumerable<Ship>> GetShipsByUserIdAsync(int userId);
    Task<IEnumerable<User>> GetUsersByShipCodeAsync(string shipCode);
}

public class ShipRepository : IShipRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ShipRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Ship>> GetAllAsync(string? status = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Ship>(
            "dbo.sp_GetShips",
            new { Status = status },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Ship?> GetByCodeAsync(string shipCode)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Ship>(
            "dbo.sp_GetShipByCode",
            new { ShipCode = shipCode },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Ship> CreateAsync(string shipCode, string name, string fiscalYear, string status)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Ship>(
            "dbo.sp_CreateShip",
            new
            {
                ShipCode = shipCode,
                Name = name,
                FiscalYear = fiscalYear,
                Status = status
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<UserShipAssignment> AssignShipToUserAsync(int userId, string shipCode)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<UserShipAssignment>(
            "dbo.sp_AssignShipToUser",
            new { UserId = userId, ShipCode = shipCode },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<Ship>> GetShipsByUserIdAsync(int userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Ship>(
            "dbo.sp_GetShipsByUserId",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<User>> GetUsersByShipCodeAsync(string shipCode)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<User>(
            "dbo.sp_GetUsersByShipCode",
            new { ShipCode = shipCode },
            commandType: CommandType.StoredProcedure
        );
    }
}

// -----------------------------------------------------------------------------
// Crew Repository (Calls Stored Procedures Only)
// -----------------------------------------------------------------------------
public interface ICrewRepository
{
    Task<(IEnumerable<CrewListItem> Items, int TotalCount)> GetCrewListAsync(
        string shipCode,
        int pageNumber,
        int pageSize,
        string sortColumn,
        string sortDirection,
        string? searchTerm,
        DateTime? asOfDate
    );
}

public class CrewRepository : ICrewRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CrewRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(IEnumerable<CrewListItem> Items, int TotalCount)> GetCrewListAsync(
        string shipCode,
        int pageNumber,
        int pageSize,
        string sortColumn,
        string sortDirection,
        string? searchTerm,
        DateTime? asOfDate)
    {
        using var connection = _connectionFactory.CreateConnection();
        var results = (await connection.QueryAsync<CrewListItem>(
            "dbo.sp_GetCrewList",
            new
            {
                ShipCode = shipCode,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortColumn = sortColumn,
                SortDirection = sortDirection,
                SearchTerm = searchTerm,
                AsOfDate = asOfDate?.ToString("yyyy-MM-dd")
            },
            commandType: CommandType.StoredProcedure
        )).ToList();

        int totalCount = results.FirstOrDefault()?.TotalCount ?? 0;
        return (results, totalCount);
    }
}

// -----------------------------------------------------------------------------
// Financial Repository (Calls Stored Procedures Only)
// -----------------------------------------------------------------------------
public interface IFinancialRepository
{
    Task<IEnumerable<FinancialReportDetailItem>> GetFinancialReportDetailAsync(string shipCode, string accountPeriod);
    Task<IEnumerable<FinancialReportSummaryItem>> GetFinancialReportSummaryAsync(string shipCode, string accountPeriod);
}

public class FinancialRepository : IFinancialRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FinancialRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<FinancialReportDetailItem>> GetFinancialReportDetailAsync(string shipCode, string accountPeriod)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<FinancialReportDetailItem>(
            "dbo.sp_GetFinancialReportDetail",
            new { ShipCode = shipCode, AccountPeriod = accountPeriod },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<FinancialReportSummaryItem>> GetFinancialReportSummaryAsync(string shipCode, string accountPeriod)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<FinancialReportSummaryItem>(
            "dbo.sp_GetFinancialReportSummary",
            new { ShipCode = shipCode, AccountPeriod = accountPeriod },
            commandType: CommandType.StoredProcedure
        );
    }
}
