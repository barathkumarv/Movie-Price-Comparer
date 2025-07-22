using System;
using MoviePriceComparer.Models;

namespace MoviePriceComparer.Services;

public interface IMovieApiClient
{
    Task<ApiResponse<List<Movie>>> GetMoviesFromProviderAsync(string baseUrl, string providerName);
    Task<ApiResponse<MovieDetail>> GetMovieDetailsAsync(string movieId, string baseUrl);
}
