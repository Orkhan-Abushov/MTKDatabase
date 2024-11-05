using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKDatabase.DAL;
using MTKDatabase.Models;

namespace MTKDatabase.Controllers
{
    public class LatestNewsController : Controller
    {
        #region Dependency injection

        private readonly AppDbContext _db;
        public LatestNewsController(AppDbContext db)
        {
            _db = db;
        }

        #endregion

        #region CreateNewLatestNews

        [HttpPost("/latestNews/create")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> CreateNewLatestNews([FromForm] LatestNewsCreateDto latestNewsCreateDto)
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

                // Create new latestNews
                var latestNews = new LatestNews
                {
                    Title = latestNewsCreateDto.Title,
                    Description = latestNewsCreateDto.Description,
                    NewsTime = latestNewsCreateDto.NewsTime,
                    Image = latestNewsCreateDto.Image, // Store the image URL as string
                    CreatedDate = DateTime.Now,
                    UpdatedDate = null // Set UpdatedDate to null on creation
                };

                // Save to database
                _db.LatestNews.Add(latestNews);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = new
                    {
                        id = latestNews.Id,
                        title = latestNews.Title,
                        image = latestNews.Image, // Image is now just a URL string
                        description = latestNews.Description,
                        openYear = latestNews.NewsTime.ToString("yyyy-MM-dd"),
                        createdDate = latestNews.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = latestNews.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
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

        #region GetAllLatestNews

        [HttpGet("/latestNews/get")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Invalid page error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> GetAllLatestNews([FromQuery] int limit = 3, [FromQuery] int page = 1)
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
                int totalLatestNews = await _db.LatestNews.Where(x => x.IsActive).CountAsync();

                // Calculate the maximum number of pages
                int maxPages = (int)Math.Ceiling((double)totalLatestNews / limit);

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

                // Retrieve a paginated list of news from the database
                var latestNews = await _db.LatestNews
                    .Where(x => x.IsActive.Equals(true))
                    .OrderByDescending(x => x.Id)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                // Return the data in the expected format
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = latestNews.Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        image = c.Image, // Image is already stored as a string URL
                        description = c.Description,
                        newsTime = c.NewsTime.ToString("yyyy-MM-dd"),  // Add NewsTime in the response
                        createdDate = c.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = c.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        totalPages = maxPages,
                        totalLatestNews = totalLatestNews
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

        #region UpdateLatestNews

        [HttpPut("/latestNews/update/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation or no update error
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Resource not found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> UpdateLatestNews(int id, [FromForm] LatestNewsUpdateDto latestNewsUpdateDto)
        {
            try
            {
                // Find the news by ID
                var latestNews = await _db.LatestNews.FindAsync(id);
                if (latestNews == null)
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
                if (!string.IsNullOrWhiteSpace(latestNewsUpdateDto.Title) && latestNewsUpdateDto.Title != latestNews.Title)
                {
                    latestNews.Title = latestNewsUpdateDto.Title;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(latestNewsUpdateDto.Description) && latestNewsUpdateDto.Description != latestNews.Description)
                {
                    latestNews.Description = latestNewsUpdateDto.Description;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(latestNewsUpdateDto.Image) && latestNewsUpdateDto.Image != latestNews.Image)
                {
                    latestNews.Image = latestNewsUpdateDto.Image;
                    isUpdated = true;
                }
                if (latestNewsUpdateDto.NewsTime.HasValue && latestNewsUpdateDto.NewsTime != latestNews.NewsTime)
                {
                    latestNews.NewsTime = latestNewsUpdateDto.NewsTime.Value;
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
                latestNews.UpdatedDate = DateTime.Now;

                // Save the changes
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully updated" } },
                    details = new
                    {
                        id = latestNews.Id,
                        title = latestNews.Title,
                        image = latestNews.Image,
                        description = latestNews.Description,
                        openYear = latestNews.NewsTime.ToString("yyyy-MM-dd"),
                        createdDate = latestNews.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        updatedDate = latestNews.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")
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

        #region DeleteLatestNews

        [HttpDelete("/latestNews/delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Resource not found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> DeleteLatestNews(int id)
        {
            try
            {
                // Find the news by ID
                var latestNews = await _db.LatestNews.FindAsync(id);
                if (latestNews == null)
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

                // Instead of deleting, mark the news as inactive
                latestNews.IsActive = false;
                latestNews.UpdatedDate = DateTime.Now;

                // Save changes to the database
                await _db.SaveChangesAsync();

                // Return success response
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "News successfully deactivated" } }
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
