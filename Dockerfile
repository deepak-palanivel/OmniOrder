FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln* ./
COPY src/OmniOrder.Domain/*.csproj ./src/OmniOrder.Domain/
COPY src/OmniOrder.Application/*.csproj ./src/OmniOrder.Application/
COPY src/OmniOrder.Infrastructure/*.csproj ./src/OmniOrder.Infrastructure/
COPY src/OmniOrder.Api/*.csproj ./src/OmniOrder.Api/
COPY tests/OmniOrder.IntegrationTests/*.csproj ./tests/OmniOrder.IntegrationTests/

# Restore dependencies
RUN dotnet restore src/OmniOrder.Api/OmniOrder.Api.csproj

# Copy full source and build
COPY . .
WORKDIR /app/src/OmniOrder.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data

EXPOSE 8080
ENTRYPOINT ["dotnet", "OmniOrder.Api.dll"]
