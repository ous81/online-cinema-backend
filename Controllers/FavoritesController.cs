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
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly CinemaDbContext _context;
    private readonly IMapper _mapper;

    public FavoritesController(CinemaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<FavoriteDTO>>> GetMyFavorites()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var favorites = await _context.Favorites
            .Include(f => f.Movie)
            .Include(f => f.Series)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<FavoriteDTO>>(favorites));
    }

    [HttpPost]
    public async Task<ActionResult<FavoriteDTO>> AddToFavorites(FavoriteCreate request)
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

        var existingFavorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && 
                ((request.MovieId.HasValue && f.MovieId == request.MovieId.Value) ||
                 (request.SeriesId.HasValue && f.SeriesId == request.SeriesId.Value)));

        if (existingFavorite != null)
        {
            return BadRequest("Item is already in favorites");
        }

        var favorite = _mapper.Map<Favorite>(request);
        favorite.UserId = userId.Value;

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

        var createdFavorite = await _context.Favorites
            .Include(f => f.Movie)
            .Include(f => f.Series)
            .FirstOrDefaultAsync(f => f.Id == favorite.Id);

        return CreatedAtAction(nameof(GetMyFavorites), null, _mapper.Map<FavoriteDTO>(createdFavorite));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromFavorites(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

        if (favorite == null)
        {
            return NotFound();
        }

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
    }
}
