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
public class PostersController : ControllerBase
{
    private readonly CinemaDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;

    public PostersController(CinemaDbContext context, IMapper mapper, IWebHostEnvironment env)
    {
        _context = context;
        _mapper = mapper;
        _env = env;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PosterDTO>> GetPoster(int id)
    {
        var poster = await _context.Posters.FindAsync(id);
        if (poster == null)
        {
            return NotFound();
        }
        return Ok(_mapper.Map<PosterDTO>(poster));
    }

    [HttpGet("{id}/file")]
    public async Task<IActionResult> DownloadPosterFile(int id)
    {
        var poster = await _context.Posters.FindAsync(id);
        if (poster == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(poster.Url) || !poster.Url.StartsWith("/"))
        {
            return BadRequest("Poster URL points outside local storage or is invalid");
        }

        var wwwroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullPath = Path.Combine(wwwroot, poster.Url.TrimStart('/'));
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound("File not found on server");
        }

        var contentType = string.IsNullOrWhiteSpace(poster.MimeType) ? "application/octet-stream" : poster.MimeType;
        return PhysicalFile(fullPath, contentType);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePoster(int id)
    {
        var poster = await _context.Posters.FindAsync(id);
        if (poster == null)
        {
            return NotFound();
        }

        _context.Posters.Remove(poster);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
