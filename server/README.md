# Server

.NET 10 Web API בשלוש שכבות: `Api` (Controllers) → `Bll` (שירותים, Mapping עם AutoMapper, Cache) → `Dal` (EF Core + Repositories). התקשורת בין השכבות דרך Dependency Injection.

### Endpoints

| Method | Route | תיאור |
|---|---|---|
| GET | `/api/requests` | חיפוש, סינון משולב, מיון ודפדוף בצד השרת. ראו [docs/requests-list-page.md](../docs/requests-list-page.md) |
| GET | `/api/requests/{id}` | פנייה בודדת. 200 / 404 |
| GET | `/api/requests/{id}/history` | היסטוריית שינויי סטטוס, החדש ראשון. 200 / 404 |
| PUT | `/api/requests/{id}` | עדכון סטטוס, עדיפות ומטפל (`status`, `priority`, `assignedToUserId`, `rowVersion`, `changedByUserId`). 200 / 400 / 404 / 409 (RowVersion ישן) / 422 (מעבר סטטוס לא מותר או משתמש לא קיים) |
| POST | `/api/requests/bulk-status` | עדכון סטטוס של עד 100 פניות, Partial Success. ראו [docs/bulk-status-update.md](../docs/bulk-status-update.md) |
| GET | `/api/requests/summary` | פילוח פניות לפי סטטוס ועדיפות (במטמון). ראו [docs/summary-aggregations.md](../docs/summary-aggregations.md) |
| GET | `/api/users` | רשימת משתמשים פעילים לבחירת מטפל |

### בדיקות

```powershell
dotnet test .\tests\RequestsManagement.Tests
```

פירוט ב-[docs/testing.md](../docs/testing.md).

```powershell
dotnet run --project .\src\RequestsManagement.Api --launch-profile http
```

כתובת: `http://localhost:5231`.

ה-Connection String נמצא ב-`appsettings.Development.json` (ברירת מחדל: `localhost`). להתאמה למופע אחר, ראו [`docs/database-setup.md`](../docs/database-setup.md).
