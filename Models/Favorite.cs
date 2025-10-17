using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.Models;

public class Favorite
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public int? MovieId { get; set; }
    public int? SeriesId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual User User { get; set; } = null!;
    public virtual Movie? Movie { get; set; }
    public virtual Series? Series { get; set; }
}
