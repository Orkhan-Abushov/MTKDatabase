using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKDatabase.DAL;
using MTKDatabase.Models;

namespace MTKDatabase.Controllers
{
    public class MerchantsController : Controller
    {
        #region Dependency injection

        private readonly AppDbContext _db;
        public MerchantsController(AppDbContext db)
        {
            _db = db;
        }

        #endregion

        #region CreateNewMerchant

        [HttpPost("/merchants/create")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> CreateNewMerchant([FromForm] MerchantCreateDto merchantCreateDto)
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

            try
            {
                // Create new merchant
                var merchant = new Merchant
                {
                    Title = merchantCreateDto.Title,
                    Address = merchantCreateDto.Address,
                    PhoneNumber = merchantCreateDto.PhoneNumber,
                    Email = merchantCreateDto.Email,
                    Web = merchantCreateDto.Web,
                    Description = merchantCreateDto.Description,
                    Image = merchantCreateDto.Image, // Store the image URL as string
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null // Set UpdatedDate to null on creation
                };

                // Save to database
                _db.Merchants.Add(merchant);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = new
                    {
                        id = merchant.Id,
                        title = merchant.Title,
                        address = merchant.Address,
                        phoneNumber = merchant.PhoneNumber,
                        email = merchant.Email,
                        web = merchant.Web,
                        image = merchant.Image, // Image is now just a URL string
                        description = merchant.Description,
                        createdDate = merchant.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = merchant.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
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

        #region GetAllMerchants

        [HttpGet("/merchants/get")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Invalid page error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> GetAllMerchants([FromQuery] int limit = 3, [FromQuery] int page = 1)
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
                int totalMerchants = await _db.Merchants.Where(x => x.IsActive).CountAsync();

                // Calculate the maximum number of pages
                int maxPages = (int)Math.Ceiling((double)totalMerchants / limit);

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

                // Retrieve a paginated list of merchants from the database
                var merchants = await _db.Merchants
                    .Where(x => x.IsActive.Equals(true))
                    .OrderByDescending(x => x.Id)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                // Return the data in the expected format
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = merchants.Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        address = c.Address,
                        phoneNumber = c.PhoneNumber,
                        email = c.Email,
                        web = c.Web,
                        image = c.Image, // Image is already stored as a string URL
                        description = c.Description,
                        createdDate = c.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = c.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        totalPages = maxPages,
                        totalMerchants = totalMerchants
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

        #region UpdateMerchant

        [HttpPut("/merchants/update/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation or no update error
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Resource not found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> UpdateMerchant(int id, [FromForm] MerchantUpdateDto merchantUpdateDto)
        {
            try
            {
                // Find the merchant by ID
                var merchant = await _db.Merchants.FindAsync(id);
                if (merchant == null)
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
                if (!string.IsNullOrWhiteSpace(merchantUpdateDto.PhoneNumber) && merchantUpdateDto.PhoneNumber != merchant.PhoneNumber)
                {
                    merchant.PhoneNumber = merchantUpdateDto.PhoneNumber;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(merchantUpdateDto.Title) && merchantUpdateDto.Title != merchant.Title)
                {
                    merchant.Title = merchantUpdateDto.Title;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(merchantUpdateDto.Address) && merchantUpdateDto.Address != merchant.Address)
                {
                    merchant.Address = merchantUpdateDto.Address;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(merchantUpdateDto.Email) && merchantUpdateDto.Email != merchant.Email)
                {
                    merchant.Email = merchantUpdateDto.Email;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(merchantUpdateDto.Web) && merchantUpdateDto.Web != merchant.Web)
                {
                    merchant.Web = merchantUpdateDto.Web;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(merchantUpdateDto.Description) && merchantUpdateDto.Description != merchant.Description)
                {
                    merchant.Description = merchantUpdateDto.Description;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(merchantUpdateDto.Image) && merchantUpdateDto.Image != merchant.Image)
                {
                    merchant.Image = merchantUpdateDto.Image;
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
                merchant.UpdatedDate = DateTime.Now;

                // Save the changes
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully updated" } },
                    details = new
                    {
                        id = merchant.Id,
                        title = merchant.Title,
                        address = merchant.Address,
                        phoneNumber = merchant.PhoneNumber,
                        email = merchant.Email,
                        web = merchant.Web,
                        image = merchant.Image,
                        description = merchant.Description,
                        createdDate = merchant.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = merchant.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
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

        #region DeleteMerchant

        [HttpDelete("/merchants/delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Resource not found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> DeleteMerchant(int id)
        {
            try
            {
                // Find the merchant by ID
                var merchant = await _db.Merchants.FindAsync(id);
                if (merchant == null)
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

                // Instead of deleting, mark the merchant as inactive
                merchant.IsActive = false;
                merchant.UpdatedDate = DateTime.Now;

                // Save changes to the database
                await _db.SaveChangesAsync();

                // Return success response
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Merchant successfully deactivated" } }
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
