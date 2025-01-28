# Usa la imagen base de .NET SDK 8.0
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build # W.S.2019
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Establece el directorio de trabajo
WORKDIR /src

# Copia el archivo del proyecto y restaura las dependencias
COPY APIOrderUpdate.csproj ./

RUN dotnet restore "APIOrderUpdate.csproj"

COPY externalproperties/ExternalProperties.json /app/externalproperties/

# Copia el resto de los archivos y construye el proyecto
COPY . ./
WORKDIR /src
RUN dotnet build "APIOrderUpdate.csproj" -c Release -o /app/build

# Publica la aplicación en una carpeta
FROM build AS publish
RUN dotnet publish "APIOrderUpdate.csproj" -c Release -o /app/publish

# Crear la imagen final
# FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2019 AS final # W.S.2019
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "APIOrderUpdate.dll"]
