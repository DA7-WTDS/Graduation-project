# Multi-stage build for the QuantWise .NET backend (Project.Api).
# Build context is ./Backend, so paths are relative to the Backend/ folder.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# global.json pins the SDK (rollForward latestMinor); copied so restore honors it.
COPY . .

RUN dotnet restore src/API/Project.Api/Project.Api.csproj
RUN dotnet publish src/API/Project.Api/Project.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Listens on 8080 inside the container (compose maps it to the host).
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Project.Api.dll"]
