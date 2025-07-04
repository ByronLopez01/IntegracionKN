# Multi-stage build with Alpine Linux for minimal footprint

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copiar y restaurar solo el archivo .csproj primero para mejor cache
COPY ["APISenad.csproj", "./"]
RUN dotnet restore "APISenad.csproj"
# --runtime alpine-x64 fue comentado porque no es compatible por defecto

# Copiar todo el código fuente
COPY . .

# Construir el proyecto
RUN dotnet build "APISenad.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "APISenad.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained false
# --runtime alpine-x64 fue comentado

# Stage 3: Final image (Alpine runtime - 5x smaller than Windows)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copiar los archivos publicados desde la etapa de publish
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "APISenad.dll"]
