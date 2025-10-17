using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCinema.API.Data;
using OnlineCinema.API.DTOs;
using OnlineCinema.API.Models;
using AutoMapper;
using System.Security.Claims;

namespace OnlineCinema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly CinemaDbContext _context;
    private readonly IMapper _mapper;

    public ReviewsController(CinemaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet("movies/{movieId}")]
    public async Task<ActionResult<IEnumerable<ReviewDTO>>> GetMovieReviews(int movieId)
    {
        var movie = await _context.Movies.FindAsync(movieId);
        if (movie == null)
        {
            return NotFound();
        }

        var reviews = await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.MovieId == movieId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<ReviewDTO>>(reviews));
    }

    [HttpGet("series/{seriesId}")]
    public async Task<ActionResult<IEnumerable<ReviewDTO>>> GetSeriesReviews(int seriesId)
    {
        var series = await _context.Series.FindAsync(seriesId);
        if (series == null)
        {
            return NotFound();
        }

        var reviews = await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.SeriesId == seriesId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<ReviewDTO>>(reviews));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDTO>> CreateReview(ReviewCreate request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if ((request.MovieId.HasValue && request.SeriesId.HasValue) || 
            (!request.MovieId.HasValue && !request.SeriesId.HasValue))
        {
            return BadRequest("Either MovieId or SeriesId must be specified, but not both");
        }

        if (request.MovieId.HasValue)
        {
            var movie = await _context.Movies.FindAsync(request.MovieId.Value);
            if (movie == null)
            {
                return NotFound("Movie not found");
            }
        }
        else
        {
            var series = await _context.Series.FindAsync(request.SeriesId!.Value);
            if (series == null)
            {
                return NotFound("Series not found");
            }
        }

        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId.Value && 
                ((request.MovieId.HasValue && r.MovieId == request.MovieId.Value) ||
                 (request.SeriesId.HasValue && r.SeriesId == request.SeriesId.Value)));

        if (existingReview != null)
        {
            return BadRequest("You have already reviewed this item");
        }

        var review = _mapper.Map<Review>(request);
        review.UserId = userId.Value;

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        var createdReview = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == review.Id);

        var dto = _mapper.Map<ReviewDTO>(createdReview);

        if (request.MovieId.HasValue)
        {
            return CreatedAtAction(nameof(GetMovieReviews), new { movieId = request.MovieId }, dto);
        }
        else
        {
            return CreatedAtAction(nameof(GetSeriesReviews), new { seriesId = request.SeriesId }, dto);
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<ReviewDTO>> UpdateReview(int id, ReviewUpdate request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        if (review.UserId != userId)
        {
            return Forbid();
        }

        review.Text = request.Text;
        review.Rating = request.Rating;
        review.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        var updatedReview = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        return Ok(_mapper.Map<ReviewDTO>(updatedReview));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        var userRole = GetCurrentUserRole();
        
        if (review.UserId != userId && userRole != "Admin")
        {
            return Forbid();
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
