# Usa el SDK de .NET 8.0 basado en Windows Server Core 2019
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build # W.S.2019
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["APIWaveRelease/APIWaveRelease.csproj", "APIWaveRelease/"]


RUN dotnet restore "APIWaveRelease/APIWaveRelease.csproj"
COPY APIWaveRelease/. APIWaveRelease/

COPY externalproperties/ExternalProperties.json /app/externalproperties/

WORKDIR "/src/APIWaveRelease"
RUN dotnet build "APIWaveRelease.csproj" -c Release -o /app/build

# Publicar la aplicaci�n en una carpeta
FROM build AS publish
RUN dotnet publish "APIWaveRelease.csproj" -c Release -o /app/publish

# Crear la imagen final
# FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2019 AS final # W.S.2019
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app

COPY --from=publish /app/publish .
COPY externalproperties/ExternalProperties.json ./externalproperties/

EXPOSE 8080
ENTRYPOINT ["dotnet", "APIWaveRelease.dll"]
