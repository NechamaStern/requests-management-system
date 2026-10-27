# הפעלת מסד הנתונים

## בחירת טכנולוגיה
SQL Server (מקומי). נבדק על LocalDB, וגם מתאים ל-SQL Server Express / Developer.

## דרישות
- SQL Server מותקן ופועל (LocalDB, Express או Developer).
- כלי `sqlcmd` (מותקן עם SQL Server Tools / SSMS), או SSMS להרצה ידנית של הסקריפטים.

## הרצה

הסקריפטים נמצאים בתיקייה `database/` וצריך להריץ אותם **לפי הסדר**.
כולם idempotent: אפשר להריץ אותם שוב ושוב בלי לשבור דבר או ליצור כפילויות.

| # | קובץ | מה עושה |
|---|------|---------|
| 1 | `create-database.sql` | יוצר את המסד `RequestsManagement` |
| 2 | `02-create-tables.sql` | יוצר את הטבלאות, אילוצים ומפתחות זרים |
| 3 | `03-create-indexes.sql` | יוצר את האינדקסים |
| 4 | `04-seed-data.sql` | 7 משתמשים + השלמה ל-100,000 פניות |

`aggregations-examples.sql` הוא קובץ עזר עם שאילתות הסיכום (לא חלק מההקמה).

### PowerShell (sqlcmd)

החליפו את `$server` בשם המופע שלכם:
`(localdb)\MSSQLLocalDB` | `.\SQLEXPRESS` | `localhost`

```powershell
$server = '(localdb)\MSSQLLocalDB'
cd database
foreach ($f in 'create-database.sql','02-create-tables.sql','03-create-indexes.sql','04-seed-data.sql') {
    sqlcmd -S $server -E -C -b -i $f
}
```

`-E` = Windows Authentication, `-C` = אמון בתעודת השרת, `-b` = עצירה בשגיאה.

### SSMS
התחברו לשרת, פתחו כל קובץ לפי הסדר והריצו (F5).

## אימות
```powershell
sqlcmd -S $server -E -C -d RequestsManagement -Q "SELECT (SELECT COUNT(*) FROM Users) AS Users, (SELECT COUNT(*) FROM Requests) AS Requests"
```
התוצאה הצפויה: `Users = 7`, `Requests = 100000`.

## Connection String
```
Server=(localdb)\MSSQLLocalDB;Database=RequestsManagement;Trusted_Connection=True;TrustServerCertificate=True
```
יש להתאים את `Server` למופע המקומי (ראו `ConnectionStrings:DefaultConnection` ב-User Secrets של ה-API).

## איפוס
```sql
USE master;
ALTER DATABASE RequestsManagement SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE RequestsManagement;
```
ואז להריץ שוב את ארבעת הסקריפטים.
