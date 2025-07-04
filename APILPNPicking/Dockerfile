# Multi-stage build with Alpine Linux for minimal footprint

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copiar y restaurar solo el archivo .csproj primero para mejor cache
COPY ["APILPNPicking.csproj", "./"]
RUN dotnet restore "APILPNPicking.csproj"

# Copiar todo el código fuente
COPY . .

# Construir el proyecto
RUN dotnet build "APILPNPicking.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "APILPNPicking.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained false

# Stage 3: Final image (Alpine runtime - 5x smaller than Windows)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copiar los archivos publicados desde la etapa de publish
COPY --from=publish /app/publish .

# Copiar el archivo ExternalProperties.json desde la etapa build
COPY --from=build /src/externalproperties/ExternalProperties.json ./externalproperties/

EXPOSE 8080
ENTRYPOINT ["dotnet", "APILPNPicking.dll"]
