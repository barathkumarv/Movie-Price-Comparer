namespace MoviePriceComparer.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using MoviePriceComparer.Services;
    using MoviePriceComparer.Models;
    using System.Diagnostics;


    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]

    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(IMovieService movieService, ILogger<MoviesController> logger)
        {
            _movieService = movieService;
            _logger = logger;
        }

        /// <summary>
        /// Get a specific movie comparison by ID
        /// </summary>
        /// <param name="id">Movie ID</param>
        /// <returns>Movie comparison details</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<MovieComparison>>> GetMovieComparisonAsync()
        {

            var result = await _movieService.GetMoviePriceComparisonAsync();

            if (result.Success)
            {
                _logger.LogInformation("Successfully returned movie comparisons");
                return Ok(result);
            }

            _logger.LogWarning("Failed to get movie comparisons {Error}", result.ErrorMessage);
            return StatusCode(500, result);
        }
    }
}