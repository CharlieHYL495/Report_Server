# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Start with base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install System.Drawing dependencies
RUN apt-get update \
    && apt-get install -y \
        libc6-dev \
        libgdiplus \
        libx11-dev \
    && rm -rf /var/lib/apt/lists/*

# Set environment variable for System.Drawing
ENV DOTNET_SYSTEM_DRAWING_ENABLE_UNIX_SUPPORT=1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Report.Server.csproj", "./"]
RUN dotnet restore "Report.Server.csproj" \
    --source https://api.nuget.org/v3/index.json \
    --source https://get.revopos.io/nuget/revopos/
COPY . .
RUN dotnet build "Report.Server.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Report.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Report.Server.dll"]