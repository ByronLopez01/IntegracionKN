# Usa el SDK de .NET 8.0 basado en Windows Server Core 2019
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build # W.S.2019
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY APIWaveRelease.csproj ./


RUN dotnet restore "APIWaveRelease.csproj"
COPY . ./

COPY externalproperties/ExternalProperties.json /app/externalproperties/

WORKDIR /src
RUN dotnet build "APIWaveRelease.csproj" -c Release -o /app/build

# Publicar la aplicación en una carpeta
FROM build AS publish
RUN dotnet publish "APIWaveRelease.csproj" -c Release -o /app/publish