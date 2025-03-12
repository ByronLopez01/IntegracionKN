# Usa el SDK de .NET 8.0 basado en Windows Server Core 2019
# FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2019 AS build # W.S.2019
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el archivo de proyecto para aprovechar la caché
COPY APIWaveRelease.csproj ./
RUN dotnet restore "APIWaveRelease.csproj"

# Copiar el resto del código y el archivo de propiedades externas
COPY . ./
COPY externalproperties/ExternalProperties.json /app/externalproperties/

# Compilar la aplicación en modo Release
RUN dotnet build "APIWaveRelease.csproj" -c Release -o /app/build

# Etapa de publicación
FROM build AS publish
RUN dotnet publish "APIWaveRelease.csproj" -c Release -o /app/publish

# Etapa final (runtime) con imagen ASP.NET Core más liviana
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "APIWaveRelease.dll"]
