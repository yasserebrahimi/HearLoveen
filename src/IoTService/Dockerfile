FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5005

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/IoTService/IoTService.csproj", "src/IoTService/"]
RUN dotnet restore "src/IoTService/IoTService.csproj"
COPY . .
WORKDIR "/src/src/IoTService"
RUN dotnet build "IoTService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IoTService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5005
ENTRYPOINT ["dotnet", "IoTService.dll"]
