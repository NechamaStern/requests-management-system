# בדיקות אוטומטיות

הבדיקות נמצאות ב-`server/tests/RequestsManagement.Tests` (xUnit 2.9.3, `Microsoft.AspNetCore.Mvc.Testing` 10.0.5).
בסך הכול **24 בדיקות** (כולל מקרי Theory): 18 בדיקות Integration ו-6 בדיקות Unit.

## הרצה

```powershell
cd server
dotnet test tests\RequestsManagement.Tests
```

דרישות: SQL Server פועל ב-`localhost` עם Windows Authentication. אין צורך להריץ את ה-API או את סקריפטי ה-DB (הבדיקות יוצרות מסד נפרד).
למופע אחר (למשל `.\SQLEXPRESS`): להגדיר משתנה סביבה `TESTS_CONNECTION_STRING`.

## בדיקות Integration (API + SQL Server אמיתי)

`ApiFactory` (מבוסס `WebApplicationFactory<Program>`) מריץ את ה-API האמיתי בזיכרון, עם ה-Middleware, ה-DI וה-EF האמיתיים,
מול מסד **נפרד** `RequestsManagement_Tests`:

- המסד נוצר בתחילת הריצה (`EnsureCreated`) ונמחק בסופה. מסד הפיתוח `RequestsManagement` לא נוגע.
- לפני כל בדיקה `ResetAsync` מנקה את הטבלאות ואת ה-Cache, ומכניס משתמש בדיקה. הבדיקות אינן תלויות זו בזו.
- הבדיקות רצות ברצף (Collection אחד משותף), כי הן חולקות מסד.

**למה SQL Server אמיתי ולא In-Memory / SQLite:** ה-409 נאכף ע"י `ROWVERSION` של SQL Server בתוך תנאי ה-`UPDATE`.
ספק EF אחר לא מתנהג כך, ולכן רק מסד אמיתי מוכיח שאין Lost Update.

| קובץ | בדיקה | דרישה |
|---|---|---|
| `ApiSmokeTests` | ה-API, ה-DAL וה-DB עובדים יחד | תשתית |
| `RequestsQueryTests` | סינון משולב + מיון + דפדוף בשרת, `totalCount`/`totalPages` | 1, 8 (תרחיש תקין) |
| `RequestsQueryTests` | חיפוש טקסט ב-Title ו-OrganizationName | 1 |
| `RequestsQueryTests` | סינון לפי טווח תאריכים | 1 |
| `RequestsQueryTests` | Summary: ספירה לפי סטטוס ולפי עדיפות | 1 (Aggregations) |
| `StatusUpdateTests` | עדכון תקין: 200, RowVersion חדש, `UpdatedAt`, שורת היסטוריה | 2, 3, 8 (תרחיש תקין) |
| `ValidationTests` | `pageSize`/`page`/סטטוס/מיון/טווח תאריכים לא תקינים: 400 ProblemDetails (6 מקרים) | 1, 8 (Validation) |
| `ValidationTests` | מעבר אסור New→Completed: 422, ללא שינוי וללא היסטוריה | 2, 8 (Validation) |
| `ValidationTests` | ערך סטטוס לא קיים: 400 | 2 |
| `ValidationTests` | פנייה שלא קיימת: 404 | 2 |
| `ConcurrencyTests` | שני PUT מקבילים עם אותו RowVersion: 200 ו-409, שורת היסטוריה אחת | 2, 8 (Concurrency) |
| `ConcurrencyTests` | RowVersion ישן ברצף: 409, השינוי הראשון נשמר | 2, 8 (Concurrency) |
| `ConcurrencyTests` | Bulk מעורב: 200 עם הצלחה חלקית וסיבה לכל פריט (`Conflict`, `NotFound`) | 4 |

## בדיקות Unit (שירות עם Repository מזויף)

`SummaryCacheTests` (6 בדיקות): ה-Cache של ה-Summary, כולל Invalidation אחרי עדכון, אחרי Bulk מוצלח,
והעדר Invalidation כשהעדכון נכשל (דרישה 7).

## מגבלות

- הבדיקות יוצרות את הסכמה מהמודל של EF (`EnsureCreated`), ולא מסקריפטי `database/*.sql`. לכן האינדקסים שבסקריפטים לא נבדקים כאן.
  הם נבדקו בנפרד ב-[performance.md](performance.md).
- אין בדיקות ביצועים על 100K רשומות בתוך חבילת הבדיקות (נמדדו ידנית, ראו performance.md).
- ה-Angular כולל רק בדיקת scaffold (`app.spec.ts`). הדרישה מתמקדת בבדיקות צד שרת.
- הבדיקות דורשות SQL Server, ולכן רצות ב-Windows (או מול SQL Server אחר דרך `TESTS_CONNECTION_STRING`).
