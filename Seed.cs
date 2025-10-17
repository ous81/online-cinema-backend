using OnlineCinema.API.Data;
using OnlineCinema.API.Models;

namespace OnlineCinema.API;

public static class Seed
{

    public static async Task SeedDataAsync(CinemaDbContext context)
    {
        var adminUser = new User
        {
            Email = "admin@cinema.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = UserRole.Admin
        };
        context.Users.Add(adminUser);

        var regularUser = new User
        {
            Email = "user@cinema.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
            Role = UserRole.User
        };
        context.Users.Add(regularUser);

        await context.SaveChangesAsync();

        var movies = new[]
        {
        new Movie
        {
            Title = "The Matrix",
            Description = "A computer hacker learns from mysterious rebels about the true nature of his reality and his role in the war against its controllers.",
            ReleaseDate = new DateTime(1999, 3, 31),
            Duration = 136,
            Genre = "Action",
            Director = "The Wachowskis",
            BoxOffice = 463517383,
            AverageRating = 8.7
        },
        new Movie
        {
            Title = "Inception",
            Description = "A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.",
            ReleaseDate = new DateTime(2010, 7, 16),
            Duration = 148,
            Genre = "Sci-Fi",
            Director = "Christopher Nolan",
            BoxOffice = 836836967,
            AverageRating = 8.8
        }
    };

        context.Movies.AddRange(movies);
        await context.SaveChangesAsync();

        var series = new[]
        {
        new Series
        {
            Title = "Breaking Bad",
            Description = "A high school chemistry teacher diagnosed with inoperable lung cancer turns to manufacturing and selling methamphetamine in order to secure his family's future.",
            ReleaseDate = new DateTime(2008, 1, 20),
            Genre = "Drama",
            AverageRating = 9.5
        }
    };

        context.Series.AddRange(series);
        await context.SaveChangesAsync();

        var episodes = new[]
        {
        new Episode
        {
            SeriesId = series[0].Id,
            SeasonNumber = 1,
            EpisodeNumber = 1,
            Title = "Pilot",
            Description = "A high school chemistry teacher learns he has cancer and decides to cook meth to provide for his family.",
            Duration = 58
        },
        new Episode
        {
            SeriesId = series[0].Id,
            SeasonNumber = 1,
            EpisodeNumber = 2,
            Title = "Cat's in the Bag...",
            Description = "Walter and Jesse try to dispose of two bodies while Skyler becomes suspicious of Walter's behavior.",
            Duration = 48
        }
    };

        context.Episodes.AddRange(episodes);
        await context.SaveChangesAsync();

        var posters = new[]
        {
        new Poster
        {
            MovieId = movies[0].Id,
            Url = "https://example.com/matrix-poster.jpg",
            MimeType = "image/jpeg"
        },
        new Poster
        {
            MovieId = movies[1].Id,
            Url = "https://example.com/inception-poster.jpg",
            MimeType = "image/jpeg"
        },
        new Poster
        {
            SeriesId = series[0].Id,
            Url = "https://example.com/breaking-bad-poster.jpg",
            MimeType = "image/jpeg"
        }
    };

        context.Posters.AddRange(posters);
        await context.SaveChangesAsync();

        var reviews = new[]
        {
        new Review
        {
            UserId = regularUser.Id,
            MovieId = movies[0].Id,
            Text = "Mind-bending masterpiece that redefined sci-fi cinema!",
            Rating = 10,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        },
        new Review
        {
            UserId = regularUser.Id,
            MovieId = movies[1].Id,
            Text = "Nolan's best work. Complex, beautiful, and unforgettable.",
            Rating = 9,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        },
        new Review
        {
            UserId = regularUser.Id,
            SeriesId = series[0].Id,
            Text = "The greatest TV series ever made. Perfect from start to finish.",
            Rating = 10,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        }
    };

        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync();

        var favorites = new[]
        {
        new Favorite
        {
            UserId = regularUser.Id,
            MovieId = movies[0].Id,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        },
        new Favorite
        {
            UserId = regularUser.Id,
            SeriesId = series[0].Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        }
    };

        context.Favorites.AddRange(favorites);
        await context.SaveChangesAsync();
    }

}