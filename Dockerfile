# ASP.NET Core API on .NET 10 (LTS).
# The aspnet image sets ASPNETCORE_HTTP_PORTS=8080 and ships a non-root "app"
# user, so Kestrel listens on 0.0.0.0:8080 with no extra configuration.

# ---- build ------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore from the project file alone so this layer caches between code edits.
COPY src/LearnDotNet.Api/LearnDotNet.Api.csproj src/LearnDotNet.Api/
RUN dotnet restore src/LearnDotNet.Api/LearnDotNet.Api.csproj
COPY src/ src/
RUN dotnet publish src/LearnDotNet.Api/LearnDotNet.Api.csproj -c Release -o /out --no-restore

# ---- runtime ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
# /app is read-only for the non-root user; keep the SQLite file in /tmp.
ENV ConnectionStrings__Roadmap="Data Source=/tmp/roadmap.db"
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "LearnDotNet.Api.dll"]
