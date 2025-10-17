using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.DTOs;

public class MovieListDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Genre { get; set; } = string.Empty;
    public double AverageRating { get; set; }
}

public class MovieDetailsDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public int DurationMinutes { get; set; }
    public string Director { get; set; } = string.Empty;
    public decimal? BoxOffice { get; set; }
    public string Genre { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public List<PosterDTO> Posters { get; set; } = new();
    public List<ReviewDTO> Reviews { get; set; } = new();
}

public class MovieCreate
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public int ReleaseYear { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int DurationMinutes { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Director { get; set; } = string.Empty;
    
    public decimal? BoxOffice { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Genre { get; set; } = string.Empty;
}

public class MovieUpdate : MovieCreate
{
}

public class PosterDTO
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}

public class PosterCreate
{
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;
}

public class ReviewDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int? MovieId { get; set; }
    public int? SeriesId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
