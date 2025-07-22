using System.Text.Json;
using MoviePriceComparer.Models;
using MoviePriceComparer.Services;
using Prometheus;

public class MovieApiClient : IMovieApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MovieApiClient> _logger;

    private static readonly Histogram ApiDuration = Metrics.CreateHistogram(
    "movie_api_duration_seconds", "Movie API call duration",
    ["provider", "endpoint"]);

    private static readonly Counter ApiErrors = Metrics.CreateCounter(
    "movie_api_errors_total", "Movie API errors",
    ["provider", "error_type"]);


    public MovieApiClient(HttpClient httpClient, ILogger<MovieApiClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        var apiToken = _configuration["MovieApi:Token"] ?? throw new InvalidOperationException("MovieApi:Token is required");

        if (!string.IsNullOrEmpty(apiToken))
        {
            _httpClient.DefaultRequestHeaders.Add("x-access-token", apiToken);
            _logger.LogInformation("API token configured successfully");
        }
        else
        {
            _logger.LogWarning("API token not configured. Requests may fail.");
        }
    }

    public async Task<ApiResponse<List<Movie>>> GetMoviesFromProviderAsync(string baseUrl, string providerName)
    {
        using var timer = ApiDuration.WithLabels(providerName, "movies").NewTimer();

        _logger.LogInformation("Fetching movies from {Provider} at {Url}", providerName, $"{baseUrl}/movies");

        var response = await _httpClient.GetAsync($"{baseUrl}/movies");
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("{Provider} API response status: {StatusCode}", providerName, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("{Provider} returned empty response", providerName);
                return new ApiResponse<List<Movie>>
                {
                    Success = false,
                    ErrorMessage = $"{providerName} returned empty response"
                };
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            List<Movie>? movies = null;


            if (responseContent.Contains("\"Movies\""))
            {
                var moviesWrapper = JsonSerializer.Deserialize<MoviesWrapper>(responseContent, jsonOptions);
                movies = moviesWrapper?.Movies;
            }
            else
            {

                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<Movie>>>(responseContent, jsonOptions);
                if (apiResponse?.Success == true)
                {
                    movies = apiResponse.Data;
                }
            }

            if (movies != null)
            {
                _logger.LogInformation("Successfully retrieved {MovieCount} movies from {Provider}", movies.Count, providerName);
                return new ApiResponse<List<Movie>>
                {
                    Success = true,
                    Data = movies
                };
            }

            return new ApiResponse<List<Movie>>
            {
                Success = false,
                ErrorMessage = $"{providerName} returned unexpected data format"
            };
        }
        else
        {
            _logger.LogWarning("{Provider} movies API returned {StatusCode}. Response: {Response}",
                providerName, response.StatusCode, responseContent);
               ApiErrors.WithLabels(providerName, "connection").Inc();

            return new ApiResponse<List<Movie>>
            {
                Success = false,
                ErrorMessage = $"{providerName} service returned {response.StatusCode}"
            };
        }
    }



    public async Task<ApiResponse<MovieDetail>> GetMovieDetailsAsync(string movieId, string baseUrl)
    {
        using var timer = ApiDuration.WithLabels(movieId, "movies").NewTimer();

        _logger.LogInformation("Fetching movie details for {MovieId} from {BaseUrl}", movieId, baseUrl);


        var response = await _httpClient.GetAsync($"{baseUrl}/movie/{movieId}");
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("Movie details response for {MovieId}: {StatusCode}", movieId, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("Empty response for movie {MovieId}", movieId);
                return new ApiResponse<MovieDetail>
                {
                    Success = false,
                    ErrorMessage = "Empty response from service"
                };
            }


            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var movie = JsonSerializer.Deserialize<MovieDetail>(responseContent, jsonOptions);

            if (movie == null)
            {
                _logger.LogWarning("Failed to deserialize movie details for {MovieId}", movieId);
                return new ApiResponse<MovieDetail>
                {
                    Success = false,
                    ErrorMessage = "Invalid movie data format"
                };
            }

            _logger.LogDebug("Successfully deserialized movie details for {MovieId}. Price: {Price}", movieId, movie.Price);

            return new ApiResponse<MovieDetail>
            {
                Success = true,
                Data = movie
            };
        }
        else
        {
            _logger.LogWarning("Movie details API returned {StatusCode} for movie {MovieId}. Response: {Response}",
            response.StatusCode, movieId, responseContent);
            ApiErrors.WithLabels(movieId, "connection").Inc();
           

            return new ApiResponse<MovieDetail>
            {
                Success = false,
                ErrorMessage = response.StatusCode == System.Net.HttpStatusCode.NotFound ? "Movie not found" : "Service error"
            };
        }
    }
}
