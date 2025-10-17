using AutoMapper;
using OnlineCinema.API.Models;
using OnlineCinema.API.DTOs;

namespace OnlineCinema.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Movie, MovieListDTO>()
            .ForMember(dest => dest.ReleaseYear, opt => opt.MapFrom(src => src.ReleaseDate.Year));
        
        CreateMap<Movie, MovieDetailsDTO>()
            .ForMember(dest => dest.ReleaseYear, opt => opt.MapFrom(src => src.ReleaseDate.Year))
            .ForMember(dest => dest.DurationMinutes, opt => opt.MapFrom(src => src.Duration))
            .ForMember(dest => dest.Posters, opt => opt.MapFrom(src => src.Posters))
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Reviews));
        
        CreateMap<MovieCreate, Movie>()
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => new DateTime(src.ReleaseYear, 1, 1)))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.DurationMinutes));
        
        CreateMap<MovieUpdate, Movie>()
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => new DateTime(src.ReleaseYear, 1, 1)))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.DurationMinutes));

        CreateMap<Series, SeriesListDTO>()
            .ForMember(dest => dest.ReleaseYear, opt => opt.MapFrom(src => src.ReleaseDate.Year));
        
        CreateMap<Series, SeriesDetailsDTO>()
            .ForMember(dest => dest.ReleaseYear, opt => opt.MapFrom(src => src.ReleaseDate.Year))
            .ForMember(dest => dest.Episodes, opt => opt.MapFrom(src => src.Episodes))
            .ForMember(dest => dest.Posters, opt => opt.MapFrom(src => src.Posters))
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Reviews));
        
        CreateMap<SeriesCreate, Series>()
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => new DateTime(src.ReleaseYear, 1, 1)));
        
        CreateMap<SeriesUpdate, Series>()
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => new DateTime(src.ReleaseYear, 1, 1)));

        CreateMap<Episode, EpisodeDTO>();
        CreateMap<EpisodeCreate, Episode>();
        CreateMap<EpisodeUpdate, Episode>();

        CreateMap<Poster, PosterDTO>();
        CreateMap<PosterCreate, Poster>();

        CreateMap<Review, ReviewDTO>()
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email));
        
        CreateMap<ReviewCreate, Review>();
        CreateMap<ReviewUpdate, Review>();

        CreateMap<Favorite, FavoriteDTO>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => 
                src.Movie != null ? src.Movie.Title : src.Series!.Title))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => 
                src.Movie != null ? "Movie" : "Series"));
        
        CreateMap<FavoriteCreate, Favorite>();
    }
}
