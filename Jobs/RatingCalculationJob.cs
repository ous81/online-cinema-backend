using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineCinema.API.Data;
using Quartz;

namespace OnlineCinema.API.Jobs;

public class RatingCalculationJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RatingCalculationJob> _logger;

    public RatingCalculationJob(IServiceProvider serviceProvider, ILogger<RatingCalculationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting rating calculation job at {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        try
        {
            var movies = await dbContext.Movies
                .Include(m => m.Reviews)
                .ToListAsync();

            foreach (var movie in movies)
            {
                if (movie.Reviews.Any())
                {
                    movie.AverageRating = movie.Reviews.Average(r => r.Rating);
                }
                else
                {
                    movie.AverageRating = 0.0;
                }
            }

            var series = await dbContext.Series
                .Include(s => s.Reviews)
                .ToListAsync();

            foreach (var seriesItem in series)
            {
                if (seriesItem.Reviews.Any())
                {
                    seriesItem.AverageRating = seriesItem.Reviews.Average(r => r.Rating);
                }
                else
                {
                    seriesItem.AverageRating = 0.0;
                }
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Rating calculation job completed successfully at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during rating calculation job at {Time}", DateTime.UtcNow);
        }
    }
}
