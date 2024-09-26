# Usa el SDK de .NET 8.0 basado en Windows Server Core 2019
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build # W.S.2019
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["APISenad/APISenad.csproj", "APISenad/"]
RUN dotnet restore "APISenad/APISenad.csproj"
COPY APISenad/. APISenad/
WORKDIR "/src/APISenad"
RUN dotnet build "APISenad.csproj" -c Release -o /app/build

# Publicar la aplicación en una carpeta
FROM build AS publish
RUN dotnet publish "APISenad.csproj" -c Release -o /app/publish

# Crear la imagen final
# FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2019 AS final # W.S.2019
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "APISenad.dll"]
