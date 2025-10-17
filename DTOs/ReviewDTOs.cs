using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.DTOs;

public class ReviewCreate
{
    public int? MovieId { get; set; }
    public int? SeriesId { get; set; }
    
    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Text { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 10)]
    public int Rating { get; set; }
}

public class ReviewUpdate : ReviewCreate
{
}
