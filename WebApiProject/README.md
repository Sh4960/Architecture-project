**Final Project — Phase 1 — Monolith Baseline (סיכום ופעלים שנעשו)**

תקציר הפרויקט
----------------
פרויקט זה הוא מונוליט ASP.NET Core 8 שמממש מערכת של ניהול "מתנות/מוצרים" עם יכולות חנויות בסיסיות. המטרה של Phase 1 היתה לקחת את ה־WebAPI הקיים ולהכינו להשוואה מול ארכיטקטורת מיקרו‑שירותים בשלבים הבאים. עשינו את הפעולות הבאות:

- בנינו/התאמנו Web API עם קונטרולרים עבור Authentication, Users, Donors, Gifts (מפות ל־Products), Shopping (מפות ל‑Orders) ועוד.
- שמרנו הפרדה לוגית באמצעות שכבות: DAL (repos + EF Core), BLL (שירותי לוגיקה עסקית), Controllers (API surface).
- הוספנו ישות `Inventory` וטיפול ברזרב מלאי (Controller + DbSet + פעולה ל־reserve).
- העברנו את מחרוזת החיבור לחיבור קונפיגורציה/משתני סביבה והוספנו `appsettings.Development.json` לדוגמה.
- הוספנו containerization: `Dockerfile` ו־`docker-compose.yml` שמריצים `webapi` ו‑SQL Server יחד, כולל הרצת מיגרציות אוטומטית בזמן ה‑startup.
- הוספנו בדיקות יחידה בסיסיות (xUnit) לכיסוי התרחיש של אישור הזמנה והורדת מלאי.

מה קיבלנו ב־Phase 1
----------------------
- Monolithic ASP.NET Core 8 WebAPI, EF Core migrations, Swagger.
- יכולת להריץ את המערכת בקלות באמצעות `docker compose up --build` (כולל DB בתיבת Docker).
- Basic E2E flow: יצירת מוצר, יצירת הזמנה (טיוטה), אישור הזמנה שמוריד מלאי באמצעות טרנזקציה אטומית.

ארכיטקטורה ודיאגרמה
---------------------
להלן דיאגרמת Mermaid המציגה את הזרימה הבסיסית:

```mermaid
flowchart LR
  subgraph API
    A[Angular / Client] -->|HTTP| B[API Gateway / BFF (future)]
    B --> C[WebApi (Controllers)]
  end

  subgraph App
    C --> D[BLL Services]
    D --> E[DAL / Repositories]
    E --> F[(SQL Server)]
    D --> G[Inventory Controller]
  end

  style F fill:#f9f,stroke:#333,stroke-width:1px

  %% Inventory reserve flow
  C -->|POST /api/shopping/{id}/confirm| H[ConfirmShopping]
  H -->|transaction| E
  E -->|UPDATE Inventory SET Quantity = Quantity - n| F
  H -->|send notification| I[Email Service]

  click F href "https://hub.docker.com/_/microsoft-mssql-server" "SQL Server container"
```

הוראות הרצה באמצעות Docker Compose
-------------------------------------
1. ודא ש‑Docker Desktop או Docker Engine פועלים ב‑Windows (WSL2 או Docker Desktop).
2. פתח טרמינל בנתיב של הפרויקט שבו נמצא ה־`Dockerfile` ו־`docker-compose.yml`:

```powershell
cd WebApiProject

docker compose up --build
```

3. Compose יבנה את התמונה של ה־API, ירוץ את מיגרציות EF Core על ה‑DB וישמיע את ה‑API על `http://localhost:5000` (מיפוי פורטים בתצורת Compose).
4. עצירת הסביבה וניקוי:

```powershell
docker compose down
```

הערות חשובות
- ברירת המחדל של `docker-compose.yml` משתמשת ב‑SA password `Your_strong!Passw0rd`. לשימוש מבחן/פרודקשן החלף זאת בערך מאובטח (Secrets manager / env vars).
- ה־webapi להרצה ב־Development environment כבר מריץ Swagger.

בדיקות צ'קפוינט — הוראות עבור המרצה
-------------------------------------
להלן צעדי בדיקה מדויקים שניתן לבצע ב־Swagger או ב־Postman:

1) פתיחת Swagger UI
   - פתח דפדפן וגש לכתובת: http://localhost:5000/swagger

2) יצירת מוצר (Product / Gift)
   - Endpoint: `POST /api/gift`
   - JSON לדוגמה:
     ```json
     {
       "name": "Sample Product",
       "category": "General",
       "cardPrice": 100,
       "donorId": 1
     }
     ```
   - אם אין `Donor` במערכת, יש ליצור קודם `POST /api/donor` עם פרטי דונור.

3) בדיקת מלאי עבור המוצר
   - Endpoint: `POST /api/inventory` (Manager) — צור רשומת מלאי עבור ה‑GiftId שנוצר.
   - JSON לדוגמה:
     ```json
     { "giftId": 1, "quantity": 10 }
     ```

4) יצירת הזמנה (Shopping)
   - Endpoint: `POST /api/shopping`
   - JSON לדוגמה:
     ```json
     { "userId": 1, "giftId": 1, "quantity": 2 }
     ```
   - ההזמנה נוצרה כטיוטה (`IsDraft = true`).

5) אישור הזמנה והשבתת מלאי
   - Endpoint: `POST /api/shopping/{id}/confirm`
   - לאחר קריאה זו ה־API יבצע בתוך טרנזקציה: ירדת מלאי (`Inventories.Quantity -= quantity`) ואז ישנה את `IsDraft` ל־`false`.

6) אימות המלאי
   - Endpoint: `GET /api/inventory/{giftId}` — ודא שהכמות ירדה בהתאם.

בדיקה מהירה באמצעות curl (דוגמאות):
```powershell
# יצירת דונור
curl -X POST http://localhost:5000/api/donor -H "Content-Type: application/json" -d "{ \"name\": \"D1\", \"email\": \"d1@example.com\", \"phone\": \"0500000001\" }"

# יצירת מוצר
curl -X POST http://localhost:5000/api/gift -H "Content-Type: application/json" -d "{ \"name\": \"P1\", \"category\": \"General\", \"cardPrice\": 50, \"donorId\": 1 }"

# יצירת מלאי
curl -X POST http://localhost:5000/api/inventory -H "Content-Type: application/json" -d "{ \"giftId\": 1, \"quantity\": 5 }"

# יצירת הזמנה
curl -X POST http://localhost:5000/api/shopping -H "Content-Type: application/json" -d "{ \"userId\": 1, \"giftId\": 1, \"quantity\": 2 }"

# אישור הזמנה (replace {id})
curl -X POST http://localhost:5000/api/shopping/1/confirm

# בדיקת מלאי
curl http://localhost:5000/api/inventory/1
```

מה נבדוק כ־Checkpoint
-----------------------
1. `docker compose up --build` מריץ בהצלחה ו־webapi מאזין על `http://localhost:5000`.
2. Swagger UI נטען וניתן לקרוא את `swagger.json`.
3. ניתן ליצור Donor, Product (Gift), ולהוסיף Inventory.
4. יצירת הזמנה ואישורה מורידים מלאי — בדוק באמצעות `GET /api/inventory/{giftId}`.

המלצות לשלב הבא (Phase 2)
----------------------------
- הפרדת השירותים למיקרו‑שירותים: Orders, Products, Inventory, Notifications.
- הוספת message broker (RabbitMQ/Kafka) ל־reservation async ו־saga ל־coordinator של ה־order lifecycle.
- הוספת caching עבור קריאות מוצר (Redis) ו־API gateway/BFF (YARP/Ocelot).
- הוספת observability: OpenTelemetry + Jaeger/Prometheus/Grafana.

קבצים חשובים בפרויקט
----------------------
- `Program.cs` — תצורת DI, Swagger, DB ו‑middleware.
- `DAL/AppDbContext.cs` — הגדרת DbSets והמיפויים.
- `Controllers/` — נקודת הכניסה ל־API.
- `Dockerfile`, `docker-compose.yml` — containerization וקומפוז.
- `WebApiProject.Tests/` — בדיקות יחידה בסיסיות.

אם תרצה, אוסיף גם:
- קובץ `diagram.svg` או `diagram.png` שנבנה אוטומטית מתוך ה‑Mermaid.
- GitHub Actions workflow לביצוע `dotnet build`, `dotnet test` ובניית Docker image.

---
קראתי את ההוראות של המרצה וסידרתי את Phase 1 כך שתוכל להדגים את השינויים והאפיונים המבוקשים. אם תרצה שאייצא את דיאגרמת ה‑Mermaid כקובץ תמונה אייצר `diagram.svg` ואוסיף אותו לריפו.
