# שלב 1: הבאת סביבת הריצה של .NET 8 לצורך הרצת האפליקציה בהמשך
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# שלב 2: הבאת ה-SDK של .NET 8 לצורך קומפילציה ובנייה (Build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# העתקת קובץ הפרויקט של ה-Gateway לצורך שחזור חבילות (Restore)
COPY ["ApiGateway/ApiGateway.csproj", "ApiGateway/"]
RUN dotnet restore "ApiGateway/ApiGateway.csproj"

# העתקת כל שאר קבצי המקור של התיקייה
COPY . .
WORKDIR "/src/ApiGateway"

# ביצוע הפעולות לבניית הפרויקט במצב Release
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

# שלב 3: פרסום האפליקציה (Publish) לקבצים מוכנים להרצה
FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# שלב 4: יצירת הקונטיינר הסופי שמכיל רק את קבצי הריצה לטובת משקל קל ויעילות
FROM base AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]