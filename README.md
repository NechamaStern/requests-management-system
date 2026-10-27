# מערכת ניהול פניות

מערכת לניהול פניות מארגונים ומעסיקים (Angular + ASP.NET Core + SQL Server). ממומשים: Dashboard עם פילוח לפי סטטוס ועדיפות, עמוד פניות (חיפוש, סינון, מיון ועימוד בצד השרת), עדכון סטטוס עם Optimistic Concurrency, היסטוריית שינויים ועדכון סטטוס מרוכז, מעל מסד נתונים עם 100,000 פניות.

## מבנה הפרויקט

```text
client/                         אפליקציית Angular
server/                         פתרון .NET ו-ASP.NET Core Web API
  src/RequestsManagement.Api/    Controllers
  src/RequestsManagement.Bll/    שירותים, Mapping (DTO + Profiles), Cache
  src/RequestsManagement.Dal/    EF Core, Entities, Repositories
database/                       סקריפטים ל-SQL Server (טבלאות, אינדקסים, Seed)
docs/                           תיעוד
```

## תיעוד

- [פילוח פניות ל-Dashboard (זרימה, דרישות, Cache)](docs/summary-aggregations.md)
- [עמוד פניות (חיפוש, סינון, מיון, עימוד והחלטות)](docs/requests-list-page.md)
- [עמוד פרטי פניה (עדכון, 409, היסטוריה)](docs/request-details-page.md)
- [לקוח Angular: מיפוי דרישות והפרדת אחריות](docs/angular-client.md)
- [עדכון סטטוס, Optimistic Concurrency והיסטוריית שינויים (Audit)](docs/status-update-concurrency.md)
- [עדכון סטטוס מרוכז (Bulk, Partial Success ונימוק)](docs/bulk-status-update.md)
- [Logging וטיפול בשגיאות גלובלי](docs/logging-and-error-handling.md)
- [Cache (בחירה, Expiration, Invalidation, מספר שרתים)](docs/cache.md)
- [ביצועים על 100K פניות (Execution Plan, אינדקסים, Bottleneck)](docs/performance.md)
- [בדיקות אוטומטיות (Integration + Unit, מיפוי לדרישות, הרצה)](docs/testing.md)
- [הפעלת מסד הנתונים](docs/database-setup.md)
- [סכמה ואינדקסים](docs/database-schema.md)

## דרישות מקדימות

- .NET SDK 10.
- Node.js 22.12 ומעלה בענף 22, או 24 ומעלה; npm 8 ומעלה.
- SQL Server מקומי פועל. להתקנת Express לדוגמה אפשר להשתמש בשם מופע `.\SQLEXPRESS`.
- `sqlcmd` נדרש רק ליצירת מסד הנתונים באמצעות הסקריפט.

## טכנולוגיות וגרסאות

| רכיב | גרסה |
|------|------|
| .NET / ASP.NET Core | 10.0 (`net10.0`) |
| Entity Framework Core (SQL Server) | 10.0 |
| AutoMapper | 16.2 |
| Angular / TypeScript | 21.2 / 5.9 |
| Node.js | 22.13 (נבדק) |
| SQL Server | 2019 ומעלה (כולל Express) |
| מסד נתונים | SQL Server, `RequestsManagement` |

## הרצה מקומית ב-Windows PowerShell

1. שכפל את המאגר והתקן את חבילות Angular:

   ```powershell
   git clone <כתובת-המאגר>
   cd <תיקיית-המאגר>
   Push-Location .\client
   npm ci
   Pop-Location
   ```

2. צור והקם את מסד הנתונים (טבלאות, אינדקסים ו-100,000 פניות). הסקריפטים ניתנים להרצה חוזרת. שנה את שם השרת/המופע לפי ההתקנה המקומית (פירוט ב-[docs/database-setup.md](docs/database-setup.md)):

   ```powershell
   cd .\database
   foreach ($f in 'create-database.sql','02-create-tables.sql','03-create-indexes.sql','04-seed-data.sql') {
       sqlcmd -S localhost -E -C -b -i $f
   }
   cd ..
   ```

   סקריפט ה-Seed (`04-seed-data.sql`) יוצר 100,000 פניות, ובהרצה חוזרת אינו מכפיל נתונים. כדי לוודא: `sqlcmd -S localhost -E -C -d RequestsManagement -Q "SELECT COUNT(*) FROM Requests"`.

3. הפעל את ה-API בחלון PowerShell ראשון:

   ```powershell
   dotnet run --project .\server\src\RequestsManagement.Api --launch-profile http
   ```

4. הפעל את Angular בחלון PowerShell שני:

   ```powershell
   Push-Location .\client
   npm start
   Pop-Location
   ```

   האפליקציה תהיה זמינה בכתובת `http://localhost:4200`. בקשות `/api` מ-Angular מועברות ל-API באמצעות proxy לפיתוח. בדיקה ישירה של ה-API: `http://localhost:5231/api/requests/summary`.

## הרצת הבדיקות

```powershell
dotnet test .\server\tests\RequestsManagement.Tests
```

פירוט בבדיקות ובמיפוי לדרישות: [docs/testing.md](docs/testing.md).

## הגדרת חיבור ל-SQL Server

הגדרות הפיתוח כוללות מחרוזת חיבור לדוגמה המשתמשת באימות Windows ובשרת `localhost`. אם משתמשים במופע או בפרטי התחברות אחרים, אפשר לעדכן מקומית באמצעות User Secrets, בלי להכניס סיסמה לקובץ שבמאגר:

```powershell
dotnet user-secrets init --project .\server\src\RequestsManagement.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=RequestsManagement;Trusted_Connection=True;TrustServerCertificate=True;" --project .\server\src\RequestsManagement.Api
```

ה-API פותח חיבור ל-SQL Server דרך `ConnectionStrings:DefaultConnection`; יש להקים קודם את מסד הנתונים (שלב 2).

## העלאה ל-Git

קובצי `.gitignore` מונעים הוספת תוצרי build, `node_modules` וקובצי סודות מקומיים. לאחר יצירת מאגר מרוחק:

```powershell
git init
git add .
git commit -m "Initial application scaffold"
git branch -M main
git remote add origin <כתובת-המאגר>
git push -u origin main
```
