namespace MoviePriceComparer.Models
{
    using System.Text.Json.Serialization;

    public class Movie
    {
        public string ID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Poster { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;

    }

    public class MovieDetail : Movie
    {
        public string Rated { get; set; } = string.Empty;
        public string Released { get; set; } = string.Empty;
        public string Runtime { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Director { get; set; } = string.Empty;
        public string Writer { get; set; } = string.Empty;
        public string Actors { get; set; } = string.Empty;
        public string Plot { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Metascore { get; set; } = string.Empty;
        public string Rating { get; set; } = string.Empty;
        public string Votes { get; set; } = string.Empty;

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Price { get; set; }
    }


    public class MoviesApiResponse
    {
        public List<Movie> Movies { get; set; } = new List<Movie>();
    }

    public class MovieComparison
    {
        public string MovieId { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }
        public string Poster { get; set; }
        public Dictionary<string, bool> ProviderAvailability { get; set; } = new();
        public Dictionary<string, decimal?> ProviderPrices { get; set; } = new();
        public string CheapestProvider { get; set; }
        public decimal? CheapestPrice { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class MoviesWrapper
    {
        public List<Movie> Movies { get; set; } = new();
    }
}
