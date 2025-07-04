# Multi-stage build with Alpine Linux for minimal footprint

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy and restore project files first (better layer caching)
COPY ["APIOrderConfirmation.csproj", "./"]
RUN dotnet restore "APIOrderConfirmation.csproj"

# Copy everything else
COPY . .

# Build the project
RUN dotnet build "APIOrderConfirmation.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "APIOrderConfirmation.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained false
#    --runtime alpine-x64

# Stage 3: Final image (Alpine runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copy published files from publish stage
COPY --from=publish /app/publish .

# Expose ports and entrypoint
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "APIOrderConfirmation.dll"]
