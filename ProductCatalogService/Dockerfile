FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProductCatalogService/ProductCatalogService.csproj", "ProductCatalogService/"]
# RUN --network=host dotnet restore "ProductCatalogService/ProductCatalogService.csproj" # unsupported on Docker Desktop Windows build
RUN dotnet restore "ProductCatalogService/ProductCatalogService.csproj"
COPY . .
WORKDIR "/src/ProductCatalogService"
RUN dotnet build "ProductCatalogService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductCatalogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "ProductCatalogService.dll"]
