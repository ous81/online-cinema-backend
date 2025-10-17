using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.Models;

public class Poster
{
    public int Id { get; set; }
    
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;
    
    public int? MovieId { get; set; }
    public int? SeriesId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual Movie? Movie { get; set; }
    public virtual Series? Series { get; set; }
}
