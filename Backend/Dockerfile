# QuantWise backend — container image for Render (demo hosting).
# Build context is the Backend/ directory (Render "Root Directory" = Backend).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/API/Project.Api/Project.Api.csproj
RUN dotnet publish src/API/Project.Api/Project.Api.csproj \
    -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# Npgsql negotiates GSSAPI on connect and needs the Kerberos library, which the
# slim runtime image omits. Without it: "Cannot load library libgssapi_krb5.so.2".
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render injects $PORT (default 10000); bind Kestrel to it.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet Project.Api.dll"]
