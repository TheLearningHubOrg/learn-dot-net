# Multi-stage Dockerfile for ASP.NET Core 8 Enterprise API
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["LearnDotNet.sln", "./"]
COPY ["src/LearnDotNet.Api/LearnDotNet.Api.csproj", "src/LearnDotNet.Api/"]
COPY ["tests/LearnDotNet.Tests/LearnDotNet.Tests.csproj", "tests/LearnDotNet.Tests/"]

RUN dotnet restore "LearnDotNet.sln"

COPY . .
RUN dotnet test "tests/LearnDotNet.Tests/LearnDotNet.Tests.csproj" -c Release --no-restore
RUN dotnet publish "src/LearnDotNet.Api/LearnDotNet.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LearnDotNet.Api.dll"]
