namespace MoviePriceComparer.Services
{
    using MoviePriceComparer.Models;

    public interface IMovieService
    {
        Task<ApiResponse<List<MovieComparison>>> GetMoviePriceComparisonAsync();
    }
}
