using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ShipManagement.Api.Infrastructure.Data;
using Xunit;

namespace ShipManagement.Tests.IntegrationTests;

public class StoredProcedureIntegrationTests
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICrewRepository _crewRepository;
    private readonly IFinancialRepository _financialRepository;
    private readonly IShipRepository _shipRepository;
    private readonly IUserRepository _userRepository;

    public StoredProcedureIntegrationTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=ShipManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
            })
            .Build();

        _connectionFactory = new SqlConnectionFactory(config);
        _crewRepository = new CrewRepository(_connectionFactory);
        _financialRepository = new FinancialRepository(_connectionFactory);
        _shipRepository = new ShipRepository(_connectionFactory);
        _userRepository = new UserRepository(_connectionFactory);
    }

    [Fact]
    public async Task Sp_GetShips_ShouldReturnAtLeast5Ships()
    {
        var ships = (await _shipRepository.GetAllAsync()).ToList();
        ships.Count.Should().BeGreaterThanOrEqualTo(5);
        ships.Should().Contain(s => s.FiscalYear == "0112");
        ships.Should().Contain(s => s.FiscalYear == "0403");
        ships.Should().Contain(s => s.Status == "Active");
        ships.Should().Contain(s => s.Status == "Inactive");
    }

    [Fact]
    public async Task Sp_GetCrewList_ForShip01_ShouldReturnActiveCrewWithPagination()
    {
        var asOf = new DateTime(2026, 10, 28);
        var (items, totalCount) = await _crewRepository.GetCrewListAsync(
            "SHIP01",
            pageNumber: 1,
            pageSize: 5,
            sortColumn: "SignOnDate",
            sortDirection: "ASC",
            searchTerm: null,
            asOfDate: asOf
        );

        totalCount.Should().BeGreaterThan(0);
        items.Count().Should().BeLessThanOrEqualTo(5);
        items.Should().OnlyContain(c => c.Status == "Onboard" || c.Status == "Relief Due");
        items.Should().NotContain(c => c.Status == "Signed Off");
    }

    [Fact]
    public async Task Sp_GetCrewList_SearchByPartialDate_ShouldFilterCorrectly()
    {
        var asOf = new DateTime(2026, 10, 28);
        var (items, totalCount) = await _crewRepository.GetCrewListAsync(
            "SHIP01",
            pageNumber: 1,
            pageSize: 50,
            sortColumn: "SignOnDate",
            sortDirection: "ASC",
            searchTerm: "Mar",
            asOfDate: asOf
        );

        items.Should().NotBeEmpty();
        items.Should().OnlyContain(c => c.SignOnDate.Contains("Mar"));
    }

    [Fact]
    public async Task Sp_GetFinancialReportDetail_ForShip01_ShouldAggregateParentAndChildAccounts()
    {
        var report = (await _financialRepository.GetFinancialReportDetailAsync("SHIP01", "2025-07")).ToList();

        report.Should().NotBeEmpty();

        // Must contain Parent accounts (Level 1)
        var parent = report.FirstOrDefault(r => r.AccountNumber == "7000000" && r.AccountType == "Parent");
        parent.Should().NotBeNull();
        parent!.Actual.Should().NotBeNull();
        parent.Budget.Should().NotBeNull();
        parent.VarianceActual.Should().Be(parent.Actual - parent.Budget);

        // Must contain Child accounts (Level 2)
        var childAccounts = report.Where(r => r.ParentAccountNumber == "7000000").ToList();
        childAccounts.Should().NotBeEmpty();

        // Verify parent actual is sum of child actuals
        var sumChildActuals = childAccounts.Sum(c => c.Actual ?? 0);
        parent.Actual.Should().Be(sumChildActuals);

        // Verify parent budget is sum of child budgets
        var sumChildBudgets = childAccounts.Sum(c => c.Budget ?? 0);
        parent.Budget.Should().Be(sumChildBudgets);
    }

    [Fact]
    public async Task Sp_GetFinancialReportSummary_ShouldReturnYTDPeriodsWithoutCOA()
    {
        var summary = (await _financialRepository.GetFinancialReportSummaryAsync("SHIP01", "2025-07")).ToList();

        summary.Should().NotBeEmpty();
        summary.Should().HaveCount(7); // Jan to Jul 2025
        summary.First().AccountPeriod.Should().Be("2025-01");
        summary.Last().AccountPeriod.Should().Be("2025-07");

        var latest = summary.Last();
        latest.CumulativeActualYTD.Should().BeGreaterThan(0);
        latest.CumulativeBudgetYTD.Should().BeGreaterThan(0);
        latest.CumulativeVarianceYTD.Should().Be(latest.CumulativeActualYTD - latest.CumulativeBudgetYTD);
    }

    [Fact]
    public async Task RankExclusivityTrigger_ShouldRejectOverlappingContracts()
    {
        using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var insertSql = @"
            INSERT INTO dbo.CrewServiceHistories (CrewMemberId, ShipCode, RankCode, SignOnDate, SignOffDate, EndOfContractDate, Notes)
            VALUES ('CREW099', 'SHIP01', 'MST', '2026-09-01', NULL, '2026-12-01', 'Test overlap');";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = insertSql;

        Func<Task> act = async () => await cmd.ExecuteNonQueryAsync();
        await act.Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>()
            .WithMessage("*Rank exclusivity violation*");
    }

    [Fact]
    public async Task Sp_GetFinancialReportDetail_CrossFiscalYear_0403_ShouldCalculateYTDFromApril()
    {
        // SHIP02 has FiscalYear 0403
        var report = (await _financialRepository.GetFinancialReportDetailAsync("SHIP02", "2025-07")).ToList();

        report.Should().NotBeEmpty();
        var parent = report.First(r => r.AccountNumber == "7000000");
        parent.ActualYTD.Should().NotBeNull();
        parent.BudgetYTD.Should().NotBeNull();
        parent.VarianceYTD.Should().Be(parent.ActualYTD - parent.BudgetYTD);
    }

    [Fact]
    public async Task Sp_GetUsers_ShouldReturnSeedUsers()
    {
        var users = (await _userRepository.GetAllAsync()).ToList();
        users.Count.Should().BeGreaterThanOrEqualTo(5);
        users.Should().Contain(u => u.Username == "admin");
    }
}
