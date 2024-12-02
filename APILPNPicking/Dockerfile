# Usa el SDK de .NET 8.0 basado en Windows Server Core 2019
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build # W.S.2019
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY APILPNPicking.csproj ./

RUN dotnet restore "APILPNPicking.csproj"
COPY . ./
COPY externalproperties/ExternalProperties.json /app/externalproperties/

WORKDIR /src
RUN dotnet build "APILPNPicking.csproj" -c Release -o /app/build

# Publicar la aplicación en una carpeta
FROM build AS publish
RUN dotnet publish "APILPNPicking.csproj" -c Release -o /app/publish

# Crear la imagen final
# FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2019 AS final # W.S.2019
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
COPY externalproperties/ExternalProperties.json ./externalproperties/
EXPOSE 8080
ENTRYPOINT ["dotnet", "APILPNPicking.dll"]
