FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AnalysisService/AnalysisService.csproj", "src/AnalysisService/"]
COPY ["src/core/HearLoveen.Domain/HearLoveen.Domain.csproj", "src/core/HearLoveen.Domain/"]
COPY ["src/infrastructure/HearLoveen.Infrastructure/HearLoveen.Infrastructure.csproj", "src/infrastructure/HearLoveen.Infrastructure/"]
RUN dotnet restore "src/AnalysisService/AnalysisService.csproj"
COPY . .
WORKDIR "/src/src/AnalysisService"
RUN dotnet build "AnalysisService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AnalysisService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5002
ENTRYPOINT ["dotnet", "AnalysisService.dll"]
