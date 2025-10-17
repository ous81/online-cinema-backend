using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.Models;

public class Episode
{
    public int Id { get; set; }
    
    [Required]
    public int SeriesId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int SeasonNumber { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int EpisodeNumber { get; set; }
    
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Duration { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual Series Series { get; set; } = null!;
}
