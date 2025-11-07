FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AudioService/AudioService.csproj", "src/AudioService/"]
COPY ["src/core/HearLoveen.Domain/HearLoveen.Domain.csproj", "src/core/HearLoveen.Domain/"]
COPY ["src/core/HearLoveen.Application/HearLoveen.Application.csproj", "src/core/HearLoveen.Application/"]
COPY ["src/infrastructure/HearLoveen.Infrastructure/HearLoveen.Infrastructure.csproj", "src/infrastructure/HearLoveen.Infrastructure/"]
RUN dotnet restore "src/AudioService/AudioService.csproj"
COPY . .
WORKDIR "/src/src/AudioService"
RUN dotnet build "AudioService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AudioService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "AudioService.dll"]
