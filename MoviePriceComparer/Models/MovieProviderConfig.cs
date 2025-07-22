namespace MoviePriceComparer.Models;

public class MovieProvidersOptions
{
    public const string SectionName = "MovieProviders";
    public List<MovieProviderConfig> MovieProviders { get; set; } = new();
}

public class MovieProviderConfig
{
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
}