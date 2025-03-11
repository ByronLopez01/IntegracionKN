# Utiliza el SDK de .NET 8.0 basado en Windows Server Core 2019
#FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build 
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY APIOrderConfirmation.csproj ./
RUN dotnet restore "APIOrderConfirmation.csproj"

# Copiar el resto de los archivos y construir la aplicación
COPY . ./
WORKDIR /src
RUN dotnet build "APIOrderConfirmation.csproj" -c Release -o /app/build

# Publicar la aplicación en una carpeta
FROM build AS publish
RUN dotnet publish "APIOrderConfirmation.csproj" -c Release -o /app/publish
# Crear la imagen final
#FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2019 AS final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/logs
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "APIOrderConfirmation.dll"]
