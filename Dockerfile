FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FitCycle.sln ./
COPY src/FitCycle.Core/*.csproj src/FitCycle.Core/
COPY src/FitCycle.Infrastructure/*.csproj src/FitCycle.Infrastructure/
COPY src/FitCycle.Api/*.csproj src/FitCycle.Api/
RUN dotnet restore src/FitCycle.Api/FitCycle.Api.csproj

COPY src/FitCycle.Core/ src/FitCycle.Core/
COPY src/FitCycle.Infrastructure/ src/FitCycle.Infrastructure/
COPY src/FitCycle.Api/ src/FitCycle.Api/

RUN dotnet publish src/FitCycle.Api/FitCycle.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "FitCycle.Api.dll"]
