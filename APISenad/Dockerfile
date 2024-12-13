# Usa el SDK de .NET 8.0 basado en Windows Server Core 2019
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build # W.S.2019
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar las dependencias
COPY APISenad.csproj ./
RUN dotnet restore "APISenad.csproj"

# Copiar el resto de los archivos y compilar la aplicación
COPY . ./
WORKDIR /src
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
