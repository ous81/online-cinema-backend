using System.ComponentModel.DataAnnotations;

namespace OnlineCinema.API.DTOs;

public class FavoriteDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? MovieId { get; set; }
    public int? SeriesId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class FavoriteCreate
{
    public int? MovieId { get; set; }
    public int? SeriesId { get; set; }
}
