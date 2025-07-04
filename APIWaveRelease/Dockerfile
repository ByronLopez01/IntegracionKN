# Multi-stage build with Alpine Linux for minimal footprint

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy and restore project files first (better layer caching)
COPY ["APIWaveRelease.csproj", "./"]
RUN dotnet restore "APIWaveRelease.csproj"
# Nota: --runtime alpine-x64 fue comentado porque puede causar errores si no está correctamente soportado

# Copy everything else and build
COPY . .
RUN dotnet build "APIWaveRelease.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "APIWaveRelease.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained false
# Nota: --runtime alpine-x64 comentado por compatibilidad

# Stage 3: Final image (Alpine runtime - 5x smaller than Windows)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copy only necessary files
COPY --from=publish /app/publish .
COPY --from=build /src/externalproperties/ExternalProperties.json ./externalproperties/

EXPOSE 8080
ENTRYPOINT ["dotnet", "APIWaveRelease.dll"]
