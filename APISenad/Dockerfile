# Usa el SDK de .NET 8.0 basado en Windows Server Core 2019
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build # W.S.2019
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["APISenad/APISenad.csproj", "APISenad/"]


RUN dotnet restore "APISenad/APISenad.csproj"
COPY APISenad/. APISenad/

COPY externalproperties/ExternalProperties.json /app/externalproperties/

WORKDIR "/src/APISenad"
RUN dotnet build "APISenad.csproj" -c Release -o /app/build

# Publicar la aplicaci�n en una carpeta
FROM build AS publish
RUN dotnet publish "APISenad.csproj" -c Release -o /app/publish

# Crear la imagen final
# FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2019 AS final # W.S.2019
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y libgssapi-krb5-2
WORKDIR /app

COPY --from=publish /app/publish .
COPY externalproperties/ExternalProperties.json ./externalproperties/

EXPOSE 8080
ENTRYPOINT ["dotnet", "APISenad.dll"]
