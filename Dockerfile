# Multi-stage Dockerfile for Anglo-Eastern Ship Management API (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/ShipManagement.Api/ShipManagement.Api.csproj", "src/ShipManagement.Api/"]
COPY ["src/ShipManagement.Tests/ShipManagement.Tests.csproj", "src/ShipManagement.Tests/"]
RUN dotnet restore "src/ShipManagement.Api/ShipManagement.Api.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/ShipManagement.Api"
RUN dotnet publish "ShipManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShipManagement.Api.dll"]
