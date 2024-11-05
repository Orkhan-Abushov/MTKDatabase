using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKDatabase.DAL;
using MTKDatabase.Models;

namespace MTKDatabase.Controllers
{
    public class CommentsController : Controller
    {
        #region Dependency injection

        private readonly AppDbContext _db;
        public CommentsController(AppDbContext db)
        {
            _db = db;
        }

        #endregion

        #region CreateNewComment

        [HttpPost("/comments/create")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Validation error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> CreateNewComment([FromForm] CommentCreateDto commentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        messages = new[] { new { status = 400, code = "INVALID_DATA", message = "Invalid input data." } },
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var comment = new Comment
                {
                    Name = commentDto.Name,
                    PhoneNumber = commentDto.PhoneNumber,
                    Email = commentDto.Email,
                    Description = commentDto.Description,
                    CreatedDate = DateTime.Now
                };

                await _db.Comments.AddAsync(comment);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = new
                    {
                        id = comment.Id,
                        name = comment.Name,
                        phoneNumber = comment.PhoneNumber,
                        email = comment.Email,
                        description = comment.Description,
                        createdDate = comment.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
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

        #region GetAllComments

        [HttpGet("/comments/get")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Invalid page error
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> GetAllComments([FromQuery] int limit = 3, [FromQuery] int page = 1)
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

                // Calculate total number of comments
                int totalComments = await _db.Comments.Where(x => x.IsActive).CountAsync();

                // Calculate the maximum number of pages
                int maxPages = (int)Math.Ceiling((double)totalComments / limit);

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

                // Retrieve a paginated list of comments from the database
                var comments = await _db.Comments
                    .Where(x => x.IsActive.Equals(true))
                    .OrderByDescending(x => x.Id)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();

                // Return the data in the expected format
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Successfully processed" } },
                    details = comments.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        phoneNumber = c.PhoneNumber,
                        email = c.Email,
                        description = c.Description,
                        createdDate = c.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        totalPages = maxPages,
                        totalComments = totalComments
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

        #region DeleteComment

        [HttpDelete("/comments/delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)] // Success response
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Resource not found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Server error
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                // Find the comments by ID
                var comments = await _db.Comments.FindAsync(id);
                if (comments == null)
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

                // Instead of deleting, mark the comment as inactive
                comments.IsActive = false;

                // Save changes to the database
                await _db.SaveChangesAsync();

                // Return success response
                return Ok(new
                {
                    messages = new[] { new { status = 200, code = "SUCCESS", message = "Comment successfully deactivated" } }
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
