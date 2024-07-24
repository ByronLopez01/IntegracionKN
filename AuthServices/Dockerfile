# Usa el SDK de .NET 8.0 para la construcción
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AuthServices/AuthServices.csproj", "AuthServices/"]
RUN dotnet restore "AuthServices/AuthServices.csproj"
COPY AuthServices/. AuthServices/
WORKDIR "/src/AuthServices"
RUN dotnet build "AuthServices.csproj" -c Release -o /app/build

# Publicar la aplicación en una carpeta
FROM build AS publish
RUN dotnet publish "AuthServices.csproj" -c Release -o /app/publish

# Crear la imagen final
# FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2019 AS base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthServices.dll"]
