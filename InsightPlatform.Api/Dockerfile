# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

ENV ASPNETCORE_ENVIRONMENT=Production
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Do not create any user, stay as root
# So we can write to mounted volumes like /app/socket

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

# Final image - running app as root
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create socket folder with root permissions
RUN mkdir -p /app/socket

# Entry point runs under root
ENTRYPOINT ["dotnet", "InsightPlatform.Api.dll"]
