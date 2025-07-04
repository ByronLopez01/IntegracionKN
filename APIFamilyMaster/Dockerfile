# Multi-stage build with Alpine Linux - reduces final image size by ~80%
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copiar y restaurar solo el archivo .csproj primero (mejor cache)
COPY ["APIFamilyMaster.csproj", "./"]
RUN dotnet restore "APIFamilyMaster.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar el proyecto
RUN dotnet build "APIFamilyMaster.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "APIFamilyMaster.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained false

# Stage 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copiar archivos publicados desde la etapa publish
COPY --from=publish /app/publish .

# Copiar archivo ExternalProperties.json desde la etapa build (ajusta ruta si es necesario)
COPY --from=build /src/externalproperties/ExternalProperties.json ./externalproperties/

# Limpieza agresiva para minimizar tamaño de la imagen
RUN find . -name "*.pdb" -delete && \
    find . -name "*.xml" -delete && \
    rm -rf /usr/share/dotnet && \
    apk del libc6-compat && \
    rm -rf /tmp/* /var/tmp/*

EXPOSE 8080
ENTRYPOINT ["dotnet", "APIFamilyMaster.dll"]
