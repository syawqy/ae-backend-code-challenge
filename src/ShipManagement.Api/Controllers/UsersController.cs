using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipManagement.Api.DTOs;
using ShipManagement.Api.Infrastructure.Data;

namespace ShipManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IShipRepository _shipRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IShipRepository shipRepository,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _shipRepository = shipRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve all registered application users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepository.GetAllAsync();
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
    /// Retrieve a user by their unique User ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById([FromRoute] int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<UserResponse>.Fail($"User with ID {id} was not found."));
        }

        var response = new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

        return Ok(ApiResponse<UserResponse>.Ok(response));
    }

    /// <summary>
    /// Register a new system user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var createdUser = await _userRepository.CreateAsync(
            request.Username,
            request.FullName,
            request.Email,
            request.Role,
            passwordHash
        );

        var response = new UserResponse
        {
            UserId = createdUser.UserId,
            Username = createdUser.Username,
            FullName = createdUser.FullName,
            Email = createdUser.Email,
            Role = createdUser.Role,
            IsActive = createdUser.IsActive,
            CreatedAt = createdUser.CreatedAt
        };

        _logger.LogInformation("Created new user {Username} with ID {UserId}", response.Username, response.UserId);
        return CreatedAtAction(nameof(GetUserById), new { id = response.UserId }, ApiResponse<UserResponse>.Ok(response, "User created successfully."));
    }

    /// <summary>
    /// Retrieve all ships assigned to a specific user
    /// </summary>
    [HttpGet("{id:int}/ships")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShipResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShipResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipsByUser([FromRoute] int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<IEnumerable<ShipResponse>>.Fail($"User with ID {id} was not found."));
        }

        var ships = await _shipRepository.GetShipsByUserIdAsync(id);
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
    /// Assign a ship to a user
    /// </summary>
    [HttpPost("{id:int}/ships")]
    [ProducesResponseType(typeof(ApiResponse<UserShipAssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserShipAssignmentResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignShipToUser([FromRoute] int id, [FromBody] AssignShipRequest request)
    {
        var assignment = await _shipRepository.AssignShipToUserAsync(id, request.ShipCode);
        var response = new UserShipAssignmentResponse
        {
            UserId = assignment.UserId,
            Username = assignment.Username,
            ShipCode = assignment.ShipCode,
            ShipName = assignment.ShipName,
            AssignedAt = assignment.AssignedAt
        };

        _logger.LogInformation("Assigned ship {ShipCode} to user ID {UserId}", request.ShipCode, id);
        return Ok(ApiResponse<UserShipAssignmentResponse>.Ok(response, "Ship assigned to user successfully."));
    }
}
