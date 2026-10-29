using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShipManagement.Api.DTOs;
using Xunit;

namespace ShipManagement.Tests.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetShips_ReturnsSuccessStatusCode_AndShipsList()
    {
        var response = await _client.GetAsync("/api/ships");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<ShipResponse>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCrewList_ReturnsPagedCrewMembers()
    {
        var response = await _client.GetAsync("/api/ships/SHIP01/crew?pageNumber=1&pageSize=5&asOfDate=2026-10-28");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CrewMemberResponse>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Items.Should().NotBeEmpty();
        content.Data.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFinancialDetail_ReturnsFinancialReport()
    {
        var response = await _client.GetAsync("/api/ships/SHIP01/financial/detail?period=2025-07");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<FinancialReportDetailResponse>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AuthLogin_WithValidCredentials_ReturnsJwtToken()
    {
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Token.Should().NotBeNullOrWhiteSpace();
        content.Data.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task AuthLogin_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "WrongPassword!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFinancialSummary_ReturnsNonCOAYTDReport()
    {
        var response = await _client.GetAsync("/api/ships/SHIP01/financial/summary?period=2025-07");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<FinancialReportSummaryResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.ShipCode.Should().Be("SHIP01");
        content.Data.CutoffPeriod.Should().Be("2025-07");
        content.Data.TotalActualYTD.Should().BeGreaterThan(0);
        content.Data.MonthlyBreakdown.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCrewList_ForInactiveShip_ReturnsBadRequest()
    {
        // SHIP05 is Inactive
        var response = await _client.GetAsync("/api/ships/SHIP05/crew?pageNumber=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
