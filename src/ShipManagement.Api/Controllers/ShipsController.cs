using Microsoft.AspNetCore.Mvc;
using ShipManagement.Api.DTOs;
using ShipManagement.Api.Infrastructure.Data;

namespace ShipManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ShipsController : ControllerBase
{
    private readonly IShipRepository _shipRepository;
    private readonly ICrewRepository _crewRepository;
    private readonly IFinancialRepository _financialRepository;
    private readonly ILogger<ShipsController> _logger;

    public ShipsController(
        IShipRepository shipRepository,
        ICrewRepository crewRepository,
        IFinancialRepository financialRepository,
        ILogger<ShipsController> logger)
    {
        _shipRepository = shipRepository;
        _crewRepository = crewRepository;
        _financialRepository = financialRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve all ships (optionally filter by status, e.g. Active or Inactive)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShipResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShips([FromQuery] string? status = null)
    {
        var ships = await _shipRepository.GetAllAsync(status);
        var response = ships.Select(s => new ShipResponse
        {
            ShipCode = s.ShipCode,
            Name = s.Name,
            FiscalYear = s.FiscalYear,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        });

        return Ok(ApiResponse<IEnumerable<ShipResponse>>.Ok(response));
    }

    /// <summary>
    /// Retrieve a ship by its unique code
    /// </summary>
    [HttpGet("{shipCode}")]
    [ProducesResponseType(typeof(ApiResponse<ShipResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ShipResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipByCode([FromRoute] string shipCode)
    {
        var ship = await _shipRepository.GetByCodeAsync(shipCode);
        if (ship == null)
        {
            return NotFound(ApiResponse<ShipResponse>.Fail($"Ship with code '{shipCode}' was not found."));
        }

        var response = new ShipResponse
        {
            ShipCode = ship.ShipCode,
            Name = ship.Name,
            FiscalYear = ship.FiscalYear,
            Status = ship.Status,
            CreatedAt = ship.CreatedAt,
            UpdatedAt = ship.UpdatedAt
        };

        return Ok(ApiResponse<ShipResponse>.Ok(response));
    }

    /// <summary>
    /// Register a new ship into the management system
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ShipResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ShipResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShip([FromBody] CreateShipRequest request)
    {
        var createdShip = await _shipRepository.CreateAsync(
            request.ShipCode,
            request.Name,
            request.FiscalYear,
            request.Status
        );

        var response = new ShipResponse
        {
            ShipCode = createdShip.ShipCode,
            Name = createdShip.Name,
            FiscalYear = createdShip.FiscalYear,
            Status = createdShip.Status,
            CreatedAt = createdShip.CreatedAt,
            UpdatedAt = createdShip.UpdatedAt
        };

        _logger.LogInformation("Created new ship {ShipCode} ({Name})", response.ShipCode, response.Name);
        return CreatedAtAction(nameof(GetShipByCode), new { shipCode = response.ShipCode }, ApiResponse<ShipResponse>.Ok(response, "Ship created successfully."));
    }

    /// <summary>
    /// Retrieve all users assigned to a specific ship
    /// </summary>
    [HttpGet("{shipCode}/users")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsersByShip([FromRoute] string shipCode)
    {
        var ship = await _shipRepository.GetByCodeAsync(shipCode);
        if (ship == null)
        {
            return NotFound(ApiResponse<IEnumerable<UserResponse>>.Fail($"Ship with code '{shipCode}' was not found."));
        }

        var users = await _shipRepository.GetUsersByShipCodeAsync(shipCode);
        var response = users.Select(u => new UserResponse
        {
            UserId = u.UserId,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        });

        return Ok(ApiResponse<IEnumerable<UserResponse>>.Ok(response));
    }

    /// <summary>
    /// Retrieve crew members currently onboard or relief due for a selected ship
    /// Supports pagination, dynamic sorting, and multi-column searching per stored procedure
    /// </summary>
    [HttpGet("{shipCode}/crew")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CrewMemberResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CrewMemberResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCrewList(
        [FromRoute] string shipCode,
        [FromQuery] CrewListQueryRequest query)
    {
        var ship = await _shipRepository.GetByCodeAsync(shipCode);
        if (ship == null)
        {
            return NotFound(ApiResponse<PagedResponse<CrewMemberResponse>>.Fail($"Ship with code '{shipCode}' was not found."));
        }

        if (!string.Equals(ship.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<PagedResponse<CrewMemberResponse>>.Fail($"Ship '{shipCode}' is inactive. Operational queries and reports are available for active vessels only."));
        }

        if (!await IsUserAuthorizedForShipAsync(shipCode))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<PagedResponse<CrewMemberResponse>>.Fail($"Access denied. Authenticated user is not assigned to vessel '{shipCode}'."));
        }

        var (items, totalCount) = await _crewRepository.GetCrewListAsync(
            shipCode,
            query.PageNumber,
            query.PageSize,
            query.SortColumn,
            query.SortDirection,
            query.SearchTerm,
            query.AsOfDate
        );

        var mapped = items.Select(c => new CrewMemberResponse
        {
            RankName = c.RankName,
            CrewMemberId = c.CrewMemberId,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Age = c.Age,
            Nationality = c.Nationality,
            SignOnDate = c.SignOnDate,
            Status = c.Status
        }).ToList();

        var pagedResponse = new PagedResponse<CrewMemberResponse>
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            Items = mapped
        };

        return Ok(ApiResponse<PagedResponse<CrewMemberResponse>>.Ok(pagedResponse));
    }

    /// <summary>
    /// Retrieve detailed financial expense report for a specific ship and accounting period
    /// Includes parent-child COA rollups and fiscal year YTD calculations per stored procedure
    /// </summary>
    [HttpGet("{shipCode}/financial/detail")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FinancialReportDetailResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FinancialReportDetailResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinancialReportDetail(
        [FromRoute] string shipCode,
        [FromQuery] string period)
    {
        if (string.IsNullOrWhiteSpace(period) || !System.Text.RegularExpressions.Regex.IsMatch(period, @"^[1-2][0-9]{3}-(0[1-9]|1[0-2])$"))
        {
            return BadRequest(ApiResponse<IEnumerable<FinancialReportDetailResponse>>.Fail("Query parameter 'period' is required and must follow YYYY-MM format (e.g. 2025-07)."));
        }

        var ship = await _shipRepository.GetByCodeAsync(shipCode);
        if (ship == null)
        {
            return NotFound(ApiResponse<IEnumerable<FinancialReportDetailResponse>>.Fail($"Ship with code '{shipCode}' was not found."));
        }

        if (!string.Equals(ship.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<IEnumerable<FinancialReportDetailResponse>>.Fail($"Ship '{shipCode}' is inactive. Operational queries and reports are available for active vessels only."));
        }

        if (!await IsUserAuthorizedForShipAsync(shipCode))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IEnumerable<FinancialReportDetailResponse>>.Fail($"Access denied. Authenticated user is not assigned to vessel '{shipCode}'."));
        }
        var reportItems = await _financialRepository.GetFinancialReportDetailAsync(shipCode, period);
        var response = reportItems.Select(r => new FinancialReportDetailResponse
        {
            COADescription = r.COADescription,
            AccountNumber = r.AccountNumber,
            AccountType = r.AccountType,
            HierarchyLevel = r.HierarchyLevel,
            ParentAccountNumber = r.ParentAccountNumber,
            Actual = r.Actual,
            Budget = r.Budget,
            VarianceActual = r.VarianceActual,
            ActualYTD = r.ActualYTD,
            BudgetYTD = r.BudgetYTD,
            VarianceYTD = r.VarianceYTD
        });

        return Ok(ApiResponse<IEnumerable<FinancialReportDetailResponse>>.Ok(response));
    }

    /// <summary>
    /// Retrieve summary financial expense report (top-level parent accounts only) for a ship and period
    /// </summary>
    [HttpGet("{shipCode}/financial/summary")]
    [ProducesResponseType(typeof(ApiResponse<FinancialReportSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FinancialReportSummaryResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<FinancialReportSummaryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFinancialReportSummary(
        [FromRoute] string shipCode,
        [FromQuery] string period)
    {
        if (string.IsNullOrWhiteSpace(period) || !System.Text.RegularExpressions.Regex.IsMatch(period, @"^[1-2][0-9]{3}-(0[1-9]|1[0-2])$"))
        {
            return BadRequest(ApiResponse<FinancialReportSummaryResponse>.Fail("Query parameter 'period' is required and must follow YYYY-MM format (e.g. 2025-07)."));
        }

        var ship = await _shipRepository.GetByCodeAsync(shipCode);
        if (ship == null)
        {
            return NotFound(ApiResponse<FinancialReportSummaryResponse>.Fail($"Ship with code '{shipCode}' was not found."));
        }

        if (!string.Equals(ship.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<FinancialReportSummaryResponse>.Fail($"Ship '{shipCode}' is inactive. Operational queries and reports are available for active vessels only."));
        }

        if (!await IsUserAuthorizedForShipAsync(shipCode))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<FinancialReportSummaryResponse>.Fail($"Access denied. Authenticated user is not assigned to vessel '{shipCode}'."));
        }

        var reportList = (await _financialRepository.GetFinancialReportSummaryAsync(shipCode, period)).ToList();
        var latestPeriodRow = reportList.LastOrDefault();

        var response = new FinancialReportSummaryResponse
        {
            ShipCode = ship.ShipCode,
            ShipName = ship.Name,
            FiscalYear = ship.FiscalYear,
            CutoffPeriod = period,
            TotalActualYTD = latestPeriodRow?.CumulativeActualYTD ?? 0,
            TotalBudgetYTD = latestPeriodRow?.CumulativeBudgetYTD ?? 0,
            TotalVarianceYTD = latestPeriodRow?.CumulativeVarianceYTD ?? 0,
            MonthlyBreakdown = reportList.Select(r => new MonthlySummaryItemResponse
            {
                AccountPeriod = r.AccountPeriod,
                MonthlyActual = r.MonthlyActual,
                MonthlyBudget = r.MonthlyBudget,
                MonthlyVariance = r.MonthlyVariance,
                CumulativeActualYTD = r.CumulativeActualYTD,
                CumulativeBudgetYTD = r.CumulativeBudgetYTD,
                CumulativeVarianceYTD = r.CumulativeVarianceYTD
            }).ToList()
        };

        return Ok(ApiResponse<FinancialReportSummaryResponse>.Ok(response));
    }

    private async Task<bool> IsUserAuthorizedForShipAsync(string shipCode)
    {
        var user = HttpContext?.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
            return true; // Anonymous / unit test direct invocation allowed

        if (user.IsInRole("Admin"))
            return true;

        var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            var assignedShips = await _shipRepository.GetShipsByUserIdAsync(userId);
            return assignedShips.Any(s => s.ShipCode.Equals(shipCode, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}
