namespace MoviePriceComparer.Services
{
    using MoviePriceComparer.Models;
    using System.Text.Json;
    using System.Net.Http;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using Prometheus;

    public class MovieService : IMovieService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MovieService> _logger;
        private readonly IReadOnlyList<MovieProviderConfig> _movieProviders;
        private readonly IMovieApiClient _movieApiClient;

        public MovieService(IMovieApiClient movieApiClient, IConfiguration configuration, ILogger<MovieService> logger, IReadOnlyList<MovieProviderConfig> movieProviders)
        {
            _movieApiClient = movieApiClient;
            _configuration = configuration;
            _logger = logger;
            _movieProviders = movieProviders ?? [];

        }

        public async Task<ApiResponse<List<MovieComparison>>> GetMoviePriceComparisonAsync()
        {
            try
            {
                if (!_movieProviders.Any())
                {
                    _logger.LogError("No movie providers configured");
                    return new ApiResponse<List<MovieComparison>>
                    {
                        Success = false,
                        ErrorMessage = "No movie providers configured"
                    };
                }

                var allMovieDetails = new List<MovieDetail>();

                foreach (var provider in _movieProviders)
                {
                    var summaries = await _movieApiClient.GetMoviesFromProviderAsync(provider.BaseUrl, provider.Name);

                    if (summaries?.Data == null) continue;

                    var detailTasks = summaries.Data.Select(async movie =>
                    {
                        var detail = await _movieApiClient.GetMovieDetailsAsync(movie.ID, provider.BaseUrl);
                        if (detail?.Success == true && detail.Data != null)
                        {
                            return new MovieDetail
                            {
                                ID = movie.ID,
                                Title = movie.Title,
                                Year = movie.Year,
                                Poster = movie.Poster,
                                Price = detail.Data.Price,
                                Provider = provider.Name
                            };
                        }
                        return null;
                    });

                    var details = await Task.WhenAll(detailTasks);
                    allMovieDetails.AddRange(details.Where(d => d != null)!);
                }

                var comparisons = allMovieDetails
                    .GroupBy(m => $"{m.Title}_{m.Year}")
                    .Select(group =>
                    {
                        var cheapest = group.OrderBy(m => m.Price).First();

                        return new MovieComparison
                        {
                            MovieId = cheapest.ID,
                            Title = cheapest.Title,
                            Year = cheapest.Year,
                            Poster = cheapest.Poster,
                            CheapestProvider = cheapest.Provider,
                            CheapestPrice = cheapest.Price,
                            ProviderPrices = group.ToDictionary(g => g.Provider, g => (decimal?)g.Price),
                            ProviderAvailability = group.ToDictionary(g => g.Provider, g => true)
                        };
                    })
                    .ToList();

                return new ApiResponse<List<MovieComparison>>
                {
                    Success = true,
                    Data = comparisons
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while comparing movie prices");
                return new ApiResponse<List<MovieComparison>>
                {
                    Success = false,
                    ErrorMessage = "An error occurred"
                };
            }
        }

    }
}
