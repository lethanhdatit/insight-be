# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Define the app user ID (replace 1000 if needed)
ENV APP_UID=1000
RUN useradd -m -u $APP_UID appuser

# Set environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production

# Set working directory and expose ports
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Switch to the app user
USER $APP_UID


# Stage for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["InsightPlatform.Api/InsightPlatform.Api.csproj", "InsightPlatform.Api/"]
RUN dotnet restore "./InsightPlatform.Api/InsightPlatform.Api.csproj"
COPY . .
WORKDIR "/src/InsightPlatform.Api"
RUN dotnet build "./InsightPlatform.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build


# Stage for publishing the app
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InsightPlatform.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# Final stage for running the app
FROM base AS final

# Create the socket directory and set correct ownership
USER root
RUN mkdir -p /app/socket && chown -R appuser:appuser /app/socket

# Switch back to app user
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .

# Start the application
ENTRYPOINT ["dotnet", "InsightPlatform.Api.dll"]
