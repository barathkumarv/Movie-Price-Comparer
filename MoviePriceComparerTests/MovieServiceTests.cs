using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;
using MoviePriceComparer.Services;
using MoviePriceComparer.Models;

[TestFixture]
public class MovieServiceTests
{
    private Mock<IMovieApiClient> _apiClientMock;
    private Mock<IConfiguration> _configMock;
    private Mock<ILogger<MovieService>> _loggerMock;
    private MovieService _movieService;

    [SetUp]
    public void Setup()
    {
        _apiClientMock = new Mock<IMovieApiClient>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<MovieService>>();

        var movieProviders = new List<MovieProviderConfig>
        {
            new() { Name = "Cinemaworld", BaseUrl = "https://webjetapitest.azurewebsites.net/api/cinemaworld/" },
            new() { Name = "Filmworld", BaseUrl = "https://webjetapitest.azurewebsites.net/api/filmworld/" }
        };

        _movieService = new MovieService(_apiClientMock.Object, _configMock.Object, _loggerMock.Object, movieProviders);
    }

    [Test]
    public async Task GetMoviePriceComparisonAsync_ReturnsCheapestProvider()
    {
        // Arrange
        var movieList = new ApiResponse<List<Movie>>
        {
            Success = true,
            Data =
            [
                  new Movie { ID = "m1", Title = "Inception", Year = "2010", Poster = "poster.jpg" , Provider="Cinemaworld"},
                  new Movie { ID = "m2", Title = "Inception", Year = "2010", Poster = "poster.jpg" , Provider="Filmworld"}

            ]
        };

        var detailCinemaworld = new ApiResponse<MovieDetail>
        {
            Success = true,
            Data = new MovieDetail { ID = "m1", Title = "Inception", Year = "2010", Price = 12.5m, Provider = "Cinemaworld" }
        };

        var detailFilmworld = new ApiResponse<MovieDetail>
        {
            Success = true,
            Data = new MovieDetail { ID = "m2", Title = "Inception", Year = "2010", Price = 10.0m, Provider = "Filmworld" }
        };

        _apiClientMock.Setup(x => x.GetMoviesFromProviderAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync(movieList);

        _apiClientMock.Setup(x => x.GetMovieDetailsAsync("m1", "https://webjetapitest.azurewebsites.net/api/cinemaworld/"))
                      .ReturnsAsync(detailCinemaworld);
        _apiClientMock.Setup(x => x.GetMovieDetailsAsync("m2", "https://webjetapitest.azurewebsites.net/api/filmworld/"))
       .ReturnsAsync(detailFilmworld);

        // Act
        var result = await _movieService.GetMoviePriceComparisonAsync();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count, Is.EqualTo(1));
        Assert.That(result.Data[0].CheapestProvider, Is.EqualTo("Filmworld"));
        Assert.That(result.Data[0].CheapestPrice, Is.EqualTo(10.0m));
    }
}
