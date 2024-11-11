
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src


COPY NuGet.Config ./NuGet.Config


COPY ["Report.Server.csproj", "./"]
RUN dotnet restore "Report.Server.csproj" --configfile ./NuGet.Config


COPY . .
RUN dotnet build "Report.Server.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "Report.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
EXPOSE 443


ENTRYPOINT ["dotnet", "Report.Server.dll"]
