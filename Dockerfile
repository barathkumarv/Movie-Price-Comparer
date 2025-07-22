# Multi-stage Dockerfile for both .NET API and React Frontend

# Stage 1: Build React Frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app/Movie-Price-Comparer-App

# Copy package files and install dependencies
COPY Movie-Price-Comparer-App/package*.json ./
RUN npm ci --only=production

# Copy frontend source and build
COPY Movie-Price-Comparer-App/ ./
RUN npm run build

# Stage 2: Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /src

# Copy csproj and restore dependencies
COPY MoviePriceComparer/*.csproj ./
RUN dotnet restore

# Copy API source and build
COPY MoviePriceComparer/ ./
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Final runtime image with .NET serving static files
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published .NET API
COPY --from=backend-build /app/publish ./

# Copy built React app to wwwroot for .NET to serve
COPY --from=frontend-build /app/Movie-Price-Comparer-App/build ./wwwroot

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MoviePriceComparer.dll"]