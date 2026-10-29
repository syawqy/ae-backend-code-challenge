using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ShipManagement.Api.Controllers;
using ShipManagement.Api.Domain.Models;
using ShipManagement.Api.DTOs;
using ShipManagement.Api.Infrastructure.Auth;
using ShipManagement.Api.Infrastructure.Data;
using Xunit;

namespace ShipManagement.Tests.ControllerTests;

public class ControllersUnitTests
{
    // -------------------------------------------------------------------------
    // UsersController Tests
    // -------------------------------------------------------------------------
    [Fact]
    public async Task GetUsers_ReturnsOk_WithUserList()
    {
        var mockUserRepo = new Mock<IUserRepository>();
        var mockShipRepo = new Mock<IShipRepository>();
        var mockLogger = new Mock<ILogger<UsersController>>();

        mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
        {
            new() { UserId = 1, Username = "admin", FullName = "Admin", Email = "admin@ae.com", Role = "Admin", IsActive = true }
        });

        var controller = new UsersController(mockUserRepo.Object, mockShipRepo.Object, mockLogger.Object);
        var actionResult = await controller.GetUsers();

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<UserResponse>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserById_WhenUserNotFound_ReturnsNotFound()
    {
        var mockUserRepo = new Mock<IUserRepository>();
        var mockShipRepo = new Mock<IShipRepository>();
        var mockLogger = new Mock<ILogger<UsersController>>();

        mockUserRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var controller = new UsersController(mockUserRepo.Object, mockShipRepo.Object, mockLogger.Object);
        var actionResult = await controller.GetUserById(99);

        var notFound = actionResult as NotFoundObjectResult;
        notFound.Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // ShipsController Tests
    // -------------------------------------------------------------------------
    [Fact]
    public async Task GetShips_ReturnsOk_WithShipsList()
    {
        var mockShipRepo = new Mock<IShipRepository>();
        var mockCrewRepo = new Mock<ICrewRepository>();
        var mockFinancialRepo = new Mock<IFinancialRepository>();
        var mockLogger = new Mock<ILogger<ShipsController>>();

        mockShipRepo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(new List<Ship>
        {
            new() { ShipCode = "SHIP01", Name = "Flying Dutchman", FiscalYear = "0112", Status = "Active" }
        });

        var controller = new ShipsController(mockShipRepo.Object, mockCrewRepo.Object, mockFinancialRepo.Object, mockLogger.Object);
        var actionResult = await controller.GetShips();

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<ShipResponse>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().HaveCount(1);
        apiResponse.Data!.First().ShipCode.Should().Be("SHIP01");
    }

    [Fact]
    public async Task GetCrewList_ReturnsPagedResponse()
    {
        var mockShipRepo = new Mock<IShipRepository>();
        var mockCrewRepo = new Mock<ICrewRepository>();
        var mockFinancialRepo = new Mock<IFinancialRepository>();
        var mockLogger = new Mock<ILogger<ShipsController>>();

        mockShipRepo.Setup(r => r.GetByCodeAsync("SHIP01")).ReturnsAsync(new Ship { ShipCode = "SHIP01", Status = "Active" });

        var crewList = new List<CrewListItem>
        {
            new() { RankName = "Master", CrewMemberId = "CREW001", FirstName = "Soka", LastName = "Philip", Age = 45, Nationality = "Greek", SignOnDate = "05 Apr 2025", Status = "Onboard", TotalCount = 1 }
        };

        mockCrewRepo.Setup(r => r.GetCrewListAsync("SHIP01", 1, 10, "SignOnDate", "ASC", null, null))
            .ReturnsAsync((crewList, 1));

        var controller = new ShipsController(mockShipRepo.Object, mockCrewRepo.Object, mockFinancialRepo.Object, mockLogger.Object);
        var actionResult = await controller.GetCrewList("SHIP01", new CrewListQueryRequest());

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<PagedResponse<CrewMemberResponse>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.TotalCount.Should().Be(1);
        apiResponse.Data.Items.Should().HaveCount(1);
        apiResponse.Data.Items[0].RankName.Should().Be("Master");
    }

    [Fact]
    public async Task GetFinancialReportDetail_ReturnsDetailReport()
    {
        var mockShipRepo = new Mock<IShipRepository>();
        var mockCrewRepo = new Mock<ICrewRepository>();
        var mockFinancialRepo = new Mock<IFinancialRepository>();
        var mockLogger = new Mock<ILogger<ShipsController>>();

        mockShipRepo.Setup(r => r.GetByCodeAsync("SHIP01")).ReturnsAsync(new Ship { ShipCode = "SHIP01", Status = "Active" });
        mockFinancialRepo.Setup(r => r.GetFinancialReportDetailAsync("SHIP01", "2025-07")).ReturnsAsync(new List<FinancialReportDetailItem>
        {
            new()
            {
                COADescription = "OPERATING EXPENSES",
                AccountNumber = "7000000",
                AccountType = "Parent",
                HierarchyLevel = 1,
                Actual = 3300m,
                Budget = 3300m,
                VarianceActual = 0m,
                ActualYTD = 9600m,
                BudgetYTD = 9900m,
                VarianceYTD = -300m
            }
        });

        var controller = new ShipsController(mockShipRepo.Object, mockCrewRepo.Object, mockFinancialRepo.Object, mockLogger.Object);
        var actionResult = await controller.GetFinancialReportDetail("SHIP01", "2025-07");

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult!.Value as ApiResponse<IEnumerable<FinancialReportDetailResponse>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().HaveCount(1);
        apiResponse.Data!.First().COADescription.Should().Be("OPERATING EXPENSES");
    }
}
