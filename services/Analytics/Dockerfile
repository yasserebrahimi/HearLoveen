FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5300

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Analytics.csproj", "./"]
RUN dotnet restore "Analytics.csproj"
COPY . .
RUN dotnet build "Analytics.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Analytics.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Analytics.dll"]
