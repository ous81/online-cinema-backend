using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCinema.API.Data;
using OnlineCinema.API.DTOs;
using OnlineCinema.API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;

namespace OnlineCinema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeriesController : ControllerBase
{
    private readonly CinemaDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public SeriesController(CinemaDbContext context, IMapper mapper, IWebHostEnvironment env)
    {
        _context = context;
        _mapper = mapper;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SeriesListDTO>>> GetSeries()
    {
        var series = await _context.Series
            .OrderByDescending(s => s.ReleaseDate)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<SeriesListDTO>>(series));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SeriesDetailsDTO>> GetSeries(int id)
    {
        var series = await _context.Series
            .Include(s => s.Episodes.OrderBy(e => e.SeasonNumber).ThenBy(e => e.EpisodeNumber))
            .Include(s => s.Posters)
            .Include(s => s.Reviews)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (series == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<SeriesDetailsDTO>(series));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SeriesDetailsDTO>> CreateSeries(SeriesCreate request)
    {
        var series = _mapper.Map<Series>(request);
        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        var createdSeries = await _context.Series
            .Include(s => s.Episodes)
            .Include(s => s.Posters)
            .Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.Id == series.Id);

        return CreatedAtAction(nameof(GetSeries), new { id = series.Id }, 
            _mapper.Map<SeriesDetailsDTO>(createdSeries));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SeriesDetailsDTO>> UpdateSeries(int id, SeriesUpdate request)
    {
        var series = await _context.Series.FindAsync(id);
        if (series == null)
        {
            return NotFound();
        }

        _mapper.Map(request, series);
        series.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        var updatedSeries = await _context.Series
            .Include(s => s.Episodes)
            .Include(s => s.Posters)
            .Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.Id == id);

        return Ok(_mapper.Map<SeriesDetailsDTO>(updatedSeries));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSeries(int id)
    {
        var series = await _context.Series.FindAsync(id);
        if (series == null)
        {
            return NotFound();
        }

        _context.Series.Remove(series);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/episodes")]
    public async Task<ActionResult<IEnumerable<EpisodeDTO>>> GetSeriesEpisodes(int id)
    {
        var series = await _context.Series.FindAsync(id);
        if (series == null)
        {
            return NotFound();
        }

        var episodes = await _context.Episodes
            .Where(e => e.SeriesId == id)
            .OrderBy(e => e.SeasonNumber)
            .ThenBy(e => e.EpisodeNumber)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<EpisodeDTO>>(episodes));
    }

    [HttpPost("{id}/episodes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EpisodeDTO>> AddEpisode(int id, EpisodeCreate request)
    {
        var series = await _context.Series.FindAsync(id);
        if (series == null)
        {
            return NotFound();
        }

        var existingEpisode = await _context.Episodes
            .FirstOrDefaultAsync(e => e.SeriesId == id && 
                e.SeasonNumber == request.SeasonNumber && 
                e.EpisodeNumber == request.EpisodeNumber);

        if (existingEpisode != null)
        {
            return BadRequest("Episode with this season and episode number already exists");
        }

        var episode = _mapper.Map<Episode>(request);
        episode.SeriesId = id;
        
        _context.Episodes.Add(episode);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSeriesEpisodes), new { id }, 
            _mapper.Map<EpisodeDTO>(episode));
    }

    [HttpGet("{id}/posters")]
    public async Task<ActionResult<IEnumerable<PosterDTO>>> GetSeriesPosters(int id)
    {
        var series = await _context.Series.FindAsync(id);
        if (series == null)
        {
            return NotFound();
        }

        var posters = await _context.Posters
            .Where(p => p.SeriesId == id)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<PosterDTO>>(posters));
    }

    [HttpPost("{id}/posters")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PosterDTO>> AddSeriesPoster(int id, PosterCreate request)
    {
        var series = await _context.Series.FindAsync(id);
        if (series == null)
        {
            return NotFound();
        }

        var poster = _mapper.Map<Poster>(request);
        poster.SeriesId = id;
        
        _context.Posters.Add(poster);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSeriesPosters), new { id }, 
            _mapper.Map<PosterDTO>(poster));
    }

    [HttpPost("{id}/posters/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PosterDTO>> UploadSeriesPoster(int id, [FromForm] IFormFile file)
    {
        var series = await _context.Series.FindAsync(id);
        if (series == null)
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
        var targetDir = Path.Combine(wwwroot, "posters", "series", id.ToString());
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

        var relativeUrl = $"/posters/series/{id}/{fileName}";

        var poster = new Poster
        {
            SeriesId = id,
            Url = relativeUrl,
            MimeType = file.ContentType
        };

        _context.Posters.Add(poster);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSeriesPosters), new { id }, _mapper.Map<PosterDTO>(poster));
    }
}
