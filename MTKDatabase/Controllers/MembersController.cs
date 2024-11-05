using Microsoft.AspNetCore.Mvc;
using MTKDatabase.DAL;
using MTKDatabase.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MTKDatabase.Controllers
{
    public class MembersController : Controller
    {
        #region Dependency injection
        private readonly AppDbContext _db;
        private readonly PasswordHasher<ManagementBoard> _passwordHasher;

        public MembersController(AppDbContext db)
        {
            _db = db;
            _passwordHasher = new PasswordHasher<ManagementBoard>();
        }
        #endregion

        #region CreateNewMember

        [HttpPost("/members/create")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> CreateNewMember(
        [FromForm] ManagementBoardCreateDto memberDto,
        [FromHeader(Name = "Device-Id")] Guid deviceId)
        {
            // Validate the input model
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    messages = new[] { new { status = 400, code = "INVALID_DATA", message = "Invalid input data." } },
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // Check if the ComplexesId exists in the Complexes table
            bool complexExists = await _db.Complexes.AnyAsync(c => c.Id == memberDto.ComplexesId && c.IsActive);
            if (!complexExists)
            {
                return BadRequest(new
                {
                    messages = new[] { new { status = 400, code = "INVALID_COMPLEX_ID", message = "The specified Complex does not exist." } }
                });
            }

            // Check if the deviceId is valid
            if (deviceId == Guid.Empty)
            {
                return BadRequest(new
                {
                    messages = new[] { new { status = 400, code = "INVALID_DEVICE_ID", message = "Device ID is missing or invalid." } }
                });
            }

            // Check if the username is already in use
            bool usernameExists = await _db.ManagementBoards.AnyAsync(mb => mb.Username == memberDto.Username);
            if (usernameExists)
            {
                return BadRequest(new
                {
                    messages = new[] { new { status = 400, code = "USERNAME_TAKEN", message = "Username is already taken." } }
                });
            }

            try
            {
                // Create a new ManagementBoard object
                var member = new ManagementBoard
                {
                    ComplexesId = memberDto.ComplexesId,
                    Name = memberDto.Name,
                    Surname = memberDto.Surname,
                    PhoneNumber = memberDto.PhoneNumber,
                    Email = memberDto.Email,
                    Address = memberDto.Address,
                    IsMan = memberDto.IsMan,
                    Username = memberDto.Username,
                    DeviceId = deviceId, // Use the retrieved DeviceId
                    CreatedDate = DateTime.Now
                };

                // Hash the password before saving
                member.Password = _passwordHasher.HashPassword(member, memberDto.Password);

                // Add the new member to the ManagementBoard table
                await _db.ManagementBoards.AddAsync(member);
                await _db.SaveChangesAsync();

                // Return success response
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = new
                    {
                        id = member.Id,
                        complexesId = member.ComplexesId,
                        name = member.Name,
                        surname = member.Surname,
                        phoneNumber = member.PhoneNumber,
                        email = member.Email,
                        address = member.Address,
                        isMan = member.IsMan,
                        username = member.Username,
                        createdDate = member.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = member.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                // Log the exception details (optional)
                return StatusCode(500, new
                {
                    errorObjectType = "SERVER",
                    errorCode = "INTERNAL_SERVER_ERROR",
                    message = "An unexpected error occurred while processing your request.",
                    status = 500,
                    errorData = new[]
                    {
                        new { requestId = Guid.NewGuid().ToString() } // Unique ID for error tracking
                    },
                    timestamp = DateTime.UtcNow.ToString("o") // ISO 8601 format
                });
            }
        }

        #endregion

        #region GetAllMembers

        [HttpGet("/members/get")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Invalid page error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> GetAllMembers([FromQuery] int limit = 8, [FromQuery] int page = 1)
        {
            try
            {
                // Ensure the page is at least 1
                if (page < 1)
                {
                    return BadRequest(new
                    {
                        messages = new[] { new { status = 400, code = "INVALID_PAGE", message = "Page number cannot be less than 1." } }
                    });
                }

                // Calculate total number of merchants
                int totalMembers = await _db.ManagementBoards.CountAsync();

                // Calculate the maximum number of pages
                int maxPages = (int)Math.Ceiling((double)totalMembers / limit);

                // If the requested page exceeds the max pages, return an error
                if (page > maxPages)
                {
                    return BadRequest(new
                    {
                        messages = new[] { new { status = 400, code = "INVALID_PAGE", message = $"Page number exceeds the maximum number of pages. Maximum is {maxPages}." } }
                    });
                }

                // Calculate the offset
                int offset = (page - 1) * limit;

                // Retrieve a paginated list of members from the database
                var members = await _db.ManagementBoards
                    .OrderByDescending(x => x.Id)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                // Return the data in the expected format
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = members.Select(c => new
                    {
                        id = c.Id,
                        complexesId = c.ComplexesId,
                        name = c.Name,
                        surname = c.Surname,
                        phoneNumber = c.PhoneNumber,
                        email = c.Email,
                        address = c.Address,
                        isMan = c.IsMan,
                        username = c.Username,
                        deviceId = c.DeviceId,
                        createdDate = c.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = c.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        totalPages = maxPages,
                        totalMembers = totalMembers
                    }
                });
            }
            catch (Exception ex)
            {
                // Log the exception details (optional)
                return StatusCode(500, new
                {
                    errorObjectType = "SERVER",
                    errorCode = "INTERNAL_SERVER_ERROR",
                    message = "An unexpected error occurred while processing your request.",
                    status = 500,
                    errorData = new[]
                    {
                        new { requestId = Guid.NewGuid().ToString() } // Unique ID for error tracking
                    },
                    timestamp = DateTime.UtcNow.ToString("o") // ISO 8601 format
                });
            }
        }

        #endregion
    }
}
