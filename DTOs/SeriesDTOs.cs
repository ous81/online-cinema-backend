using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.DTOs;

public class SeriesListDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Genre { get; set; } = string.Empty;
    public double AverageRating { get; set; }
}

public class SeriesDetailsDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Genre { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public List<EpisodeDTO> Episodes { get; set; } = new();
    public List<PosterDTO> Posters { get; set; } = new();
    public List<ReviewDTO> Reviews { get; set; } = new();
}

public class SeriesCreate
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public int ReleaseYear { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Genre { get; set; } = string.Empty;
}

public class SeriesUpdate : SeriesCreate
{
}

public class EpisodeDTO
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
}

public class EpisodeCreate
{
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
}

public class EpisodeUpdate : EpisodeCreate
{
}
