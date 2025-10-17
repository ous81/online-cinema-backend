using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.Models;

public class Review
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public int? MovieId { get; set; }
    public int? SeriesId { get; set; }
    
    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Text { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 10)]
    public int Rating { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public virtual User User { get; set; } = null!;
    public virtual Movie? Movie { get; set; }
    public virtual Series? Series { get; set; }
}
