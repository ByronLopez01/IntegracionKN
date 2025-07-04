# Multi-stage build with Alpine Linux for minimal footprint

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy and restore project files first (for better layer caching)
COPY ["APIOrderUpdate.csproj", "./"]
RUN dotnet restore "APIOrderUpdate.csproj"
# --runtime alpine-x64 is omitted because it's not recognized by default

# Copy everything else
COPY . .

# Build the project
RUN dotnet build "APIOrderUpdate.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "APIOrderUpdate.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained false
# --runtime alpine-x64 is omitted

# Stage 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copy published files from publish stage
COPY --from=publish /app/publish .

# Copy ExternalProperties.json from build stage
COPY --from=build /src/externalproperties/ExternalProperties.json ./externalproperties/

EXPOSE 8080
ENTRYPOINT ["dotnet", "APIOrderUpdate.dll"]
