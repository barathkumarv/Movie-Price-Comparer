
Compare movie prices from 2 providers - Cinemaworld and Filmworld to find the cheapest option

---

# Access endpoints:
 - Swagger UI: https://localhost:8080/swagger/index.html
 - Health Check: https://localhost:8080/health
 - Metrics: https://localhost:8080/metrics
 - Movies API: https://localhost:8080/api/movies

# Movie Price Comparison App


---

## Tech Stack

- ASP.NET Core 9.0
  - Providers injected using configuration
  - Standard resilience handler (polly) to handle any transient errors, mainly retries
  - Prometheus metrics
- React 19
  - Standard react app
- Docker
- Prometheus

---


## API Key Configuration

The API requires `x-access-token` token in the header . It has not been exposed. It will be injected as environment variable in Docker when you follow these steps

1. Create a .env file in the same directory as .env.example
2. Copy `.env.example` to `.env`
3. Fill in your actual API tokens in `.env`
4. Run `docker-compose up` and it will take of it automatically

## Required Environment Variables
- `MOVIE_API_TOKEN`: Get this from [movie API provider]



For production it is recommened to use Azure Key Vault or AWS Secrets Manager depending on the cloud provider


## Metrics with Prometheus

-Metrics - 
- HELP movie_api_duration_seconds Movie API call duration
- TYPE movie_api_duration_seconds histogram
- HELP movie_api_errors_total Movie API errors
- TYPE movie_api_errors_total counter

Prometheus config example to scrape oour app
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'your-app'
    static_configs:
      - targets: ['your-app:8080']  # Use service name from docker-compose
    metrics_path: '/metrics'  # Your app's metrics endpoint
    scrape_interval: 10s



## Run Tests


cd MoviePriceComparerTests
dotnet test

---


## Future Enhancements

- Implement Caching on Backend (Azure Redis cache recommended)
- Add custom metrics and garfana dashboards to visualize the metrics
- Optimise react(Lazy loading images ,features like Searching and Pagination)
