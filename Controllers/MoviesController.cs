using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCinema.API.Data;
using OnlineCinema.API.DTOs;
using OnlineCinema.API.Mapping;
using OnlineCinema.API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;

namespace OnlineCinema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly CinemaDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public MoviesController(CinemaDbContext context, IMapper mapper, IWebHostEnvironment env)
    {
        _context = context;
        _mapper = mapper;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieListDTO>>> GetMovies()
    {
        var movies = await _context.Movies
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<MovieListDTO>>(movies));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MovieDetailsDTO>> GetMovie(int id)
    {
        var movie = await _context.Movies
            .Include(m => m.Posters)
            .Include(m => m.Reviews)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<MovieDetailsDTO>(movie));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovieDetailsDTO>> CreateMovie(MovieCreate request)
    {
        var movie = _mapper.Map<Movie>(request);
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        var createdMovie = await _context.Movies
            .Include(m => m.Posters)
            .Include(m => m.Reviews)
            .FirstOrDefaultAsync(m => m.Id == movie.Id);

        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, 
            _mapper.Map<MovieDetailsDTO>(createdMovie));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MovieDetailsDTO>> UpdateMovie(int id, MovieUpdate request)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        _mapper.Map(request, movie);
        movie.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        var updatedMovie = await _context.Movies
            .Include(m => m.Posters)
            .Include(m => m.Reviews)
            .FirstOrDefaultAsync(m => m.Id == id);

        return Ok(_mapper.Map<MovieDetailsDTO>(updatedMovie));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/posters")]
    public async Task<ActionResult<IEnumerable<PosterDTO>>> GetMoviePosters(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        var posters = await _context.Posters
            .Where(p => p.MovieId == id)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<PosterDTO>>(posters));
    }

    [HttpPost("{id}/posters")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PosterDTO>> AddMoviePoster(int id, PosterCreate request)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        var poster = _mapper.Map<Poster>(request);
        poster.MovieId = id;
        
        _context.Posters.Add(poster);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMoviePosters), new { id }, 
            _mapper.Map<PosterDTO>(poster));
    }

    [HttpPost("{id}/posters/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PosterDTO>> UploadMoviePoster(int id, [FromForm] IFormFile file)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
        {
            return NotFound();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest("Unsupported image format. Allowed: jpeg, png, webp");
        }

        var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var targetDir = Path.Combine(wwwroot, "posters", "movies", id.ToString());
        Directory.CreateDirectory(targetDir);

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = file.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".img"
            };
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(targetDir, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/posters/movies/{id}/{fileName}";

        var poster = new Poster
        {
            MovieId = id,
            Url = relativeUrl,
            MimeType = file.ContentType
        };

        _context.Posters.Add(poster);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMoviePosters), new { id }, _mapper.Map<PosterDTO>(poster));
    }
}
