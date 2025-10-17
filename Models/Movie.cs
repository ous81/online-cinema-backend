using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.Models;

public class Movie
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public DateTime ReleaseDate { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Duration { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Genre { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Director { get; set; } = string.Empty;
    
    public decimal? BoxOffice { get; set; }
    
    public double AverageRating { get; set; } = 0.0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public virtual ICollection<Poster> Posters { get; set; } = new List<Poster>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
