using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCinema.API.Data;
using OnlineCinema.API.DTOs;
using OnlineCinema.API.Models;
using AutoMapper;

namespace OnlineCinema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EpisodesController : ControllerBase
{
    private readonly CinemaDbContext _context;
    private readonly IMapper _mapper;

    public EpisodesController(CinemaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EpisodeDTO>> UpdateEpisode(int id, EpisodeUpdate request)
    {
        var episode = await _context.Episodes
            .Include(e => e.Series)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (episode == null)
        {
            return NotFound();
        }

        var existingEpisode = await _context.Episodes
            .FirstOrDefaultAsync(e => e.SeriesId == episode.SeriesId && 
                e.SeasonNumber == request.SeasonNumber && 
                e.EpisodeNumber == request.EpisodeNumber &&
                e.Id != id);

        if (existingEpisode != null)
        {
            return BadRequest("Episode with this season and episode number already exists");
        }

        _mapper.Map(request, episode);
        episode.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<EpisodeDTO>(episode));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEpisode(int id)
    {
        var episode = await _context.Episodes.FindAsync(id);
        if (episode == null)
        {
            return NotFound();
        }

        _context.Episodes.Remove(episode);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
