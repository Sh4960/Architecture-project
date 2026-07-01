FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy project file and restore as distinct layer
COPY ./WebApiProject.csproj ./
RUN dotnet restore "./WebApiProject.csproj"

# copy everything and publish
COPY . ./
RUN dotnet publish "./WebApiProject.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "WebApiProject.dll"]
