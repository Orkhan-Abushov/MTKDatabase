using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKDatabase.DAL;
using MTKDatabase.Models;

namespace MTKDatabase.Controllers
{
    public class ComplexesController : Controller
    {
        #region Dependency Injection

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ComplexesController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        #endregion

        #region CreateNewComplex

        [HttpPost("/complexes/create")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> CreateNewComplex([FromForm] ComplexCreateDto complexCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        messages = new[]
                        {
                    new { status = 400, code = "ERROR", message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) }
                }
                    });
                }

                // Create new complex
                var complex = new Complex
                {
                    Title = complexCreateDto.Title,
                    Address = complexCreateDto.Address,
                    PhoneNumber = complexCreateDto.PhoneNumber,
                    Email = complexCreateDto.Email,
                    Web = complexCreateDto.Web,
                    Description = complexCreateDto.Description,
                    OpenYear = complexCreateDto.OpenYear,
                    Image = complexCreateDto.Image,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null
                };

                // Save to database
                _db.Complexes.Add(complex);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = new
                    {
                        id = complex.Id,
                        title = complex.Title,
                        address = complex.Address,
                        phoneNumber = complex.PhoneNumber,
                        email = complex.Email,
                        web = complex.Web,
                        image = complex.Image,
                        description = complex.Description,
                        openYear = complex.OpenYear.ToString("yyyy-MM-dd"),
                        createdDate = complex.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = complex.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
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
                new { requestId = Guid.NewGuid().ToString() }
            },
                    timestamp = DateTime.UtcNow.ToString("o")
                });
            }
        }
        #endregion

        #region GetAllComplexes

        [HttpGet("/complexes/get")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Invalid page error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> GetAllComplexes([FromQuery] int limit = 8, [FromQuery] int page = 1)
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
                int totalComplexes = await _db.Complexes.Where(x => x.IsActive).CountAsync();

                // Calculate the maximum number of pages
                int maxPages = (int)Math.Ceiling((double)totalComplexes / limit);

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

                // Retrieve a paginated list of complexes from the database
                var complexes = await _db.Complexes
                    .Where(x => x.IsActive.Equals(true))
                    .OrderByDescending(x => x.Id)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                // Return the data in the expected format
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = complexes.Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        address = c.Address,
                        phoneNumber = c.PhoneNumber,
                        email = c.Email,
                        web = c.Web,
                        image = c.Image, // Image is already stored as a string URL
                        description = c.Description,
                        openYear = c.OpenYear.ToString("yyyy-MM-dd"),  // Add OpenYear in yyyy format
                        createdDate = c.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = c.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        totalPages = maxPages,
                        totalComplexes = totalComplexes
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

        #region UpdateComplex

        [HttpPut("/complexes/update/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation or no update error
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Resource not found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> UpdateComplex(int id, [FromForm] ComplexUpdateDto complexUpdateDto)
        {
            try
            {
                // Find the complex by ID
                var complex = await _db.Complexes.FindAsync(id);
                if (complex == null)
                {
                    // Return a 404 response with a detailed error structure
                    return NotFound(new
                    {
                        errorObjectType = "Resource",
                        errorCode = "NOT_FOUND",
                        message = "The requested resource was not found.",
                        status = 404,
                        errorData = new[]
                        {
                        new { field = "id", rejectedValue = id.ToString(), error = "No record found for this ID." }
                    },
                        timestamp = DateTime.UtcNow.ToString("o") // ISO 8601 format
                    });
                }

                // Ensure that at least one field is updated
                bool isUpdated = false;

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(complexUpdateDto.PhoneNumber) && complexUpdateDto.PhoneNumber != complex.PhoneNumber)
                {
                    complex.PhoneNumber = complexUpdateDto.PhoneNumber;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(complexUpdateDto.Title) && complexUpdateDto.Title != complex.Title)
                {
                    complex.Title = complexUpdateDto.Title;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(complexUpdateDto.Address) && complexUpdateDto.Address != complex.Address)
                {
                    complex.Address = complexUpdateDto.Address;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(complexUpdateDto.Email) && complexUpdateDto.Email != complex.Email)
                {
                    complex.Email = complexUpdateDto.Email;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(complexUpdateDto.Web) && complexUpdateDto.Web != complex.Web)
                {
                    complex.Web = complexUpdateDto.Web;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(complexUpdateDto.Description) && complexUpdateDto.Description != complex.Description)
                {
                    complex.Description = complexUpdateDto.Description;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(complexUpdateDto.Image) && complexUpdateDto.Image != complex.Image)
                {
                    complex.Image = complexUpdateDto.Image;
                    isUpdated = true;
                }
                if (complexUpdateDto.OpenYear.HasValue && complexUpdateDto.OpenYear != complex.OpenYear)
                {
                    complex.OpenYear = complexUpdateDto.OpenYear.Value;
                    isUpdated = true;
                }
                // If no fields were updated, return an error
                if (!isUpdated)
                {
                    return BadRequest(new
                    {
                        messages = new[] { new { status = 400, code = "NO_UPDATE", message = "No changes detected, at least one field must be updated" } }
                    });
                }

                // Update the UpdatedDate
                complex.UpdatedDate = DateTime.Now;

                // Save the changes
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully updated" } },
                    details = new
                    {
                        id = complex.Id,
                        title = complex.Title,
                        address = complex.Address,
                        phoneNumber = complex.PhoneNumber,
                        email = complex.Email,
                        web = complex.Web,
                        image = complex.Image,
                        description = complex.Description,
                        openYear = complex.OpenYear.ToString("yyyy-MM-dd"),
                        createdDate = complex.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = complex.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
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

        #region DeleteComplex

        [HttpDelete("/complexes/delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Resource not found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> DeleteComplex(int id)
        {
            try
            {
                // Find the complex by ID
                var complex = await _db.Complexes.FindAsync(id);
                if (complex == null)
                {
                    // Return a 404 response with a detailed error structure
                    return NotFound(new
                    {
                        errorObjectType = "Resource",
                        errorCode = "NOT_FOUND",
                        message = "The requested resource was not found.",
                        status = 404,
                        errorData = new[]
                        {
                        new { field = "id", rejectedValue = id.ToString(), error = "No record found for this ID." }
                    },
                        timestamp = DateTime.UtcNow.ToString("o") // ISO 8601 format
                    });
                }

                // Instead of deleting, mark the complex as inactive
                complex.IsActive = false;
                complex.UpdatedDate = DateTime.Now;

                // Save changes to the database
                await _db.SaveChangesAsync();

                // Return success response
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Complex successfully deactivated" } }
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
