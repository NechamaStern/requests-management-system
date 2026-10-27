# Logging וטיפול בשגיאות גלובלי

## שרת (`ILogger`, structured logging)

| נקודה | רמה | מה נרשם |
|---|---|---|
| `GlobalExceptionHandler` – חריגה לא צפויה | Error | חריגה עם stack, Method, Path |
| `GlobalExceptionHandler` – כשל עסקי (404, 409, 422) | Warning | Method, Path, סיבה |
| `RequestsService` – קונפליקט מקביליות (עדכון בודד ומרוכז) | Warning | `RequestId` |
| `RequestsService` – עדכון מוצלח | Information | `RequestId`, `UserId`, סטטוס קודם וחדש |
| `RequestsService` – עדכון מרוכז | Information | סטטוס, `UserId`, מספר הצלחות וכשלונות |

- ביטול בקשה על ידי הלקוח אינו נרשם כשגיאה (מוחזר 499).
- רמות מוגדרות ב-`appsettings.json`: `Information`, ו-`Microsoft.AspNetCore` ב-`Warning`.

## למניעת חשיפת מידע רגיש

- נרשמים רק מזהים ו-enum. לא נרשמים גוף בקשה, כותרות, כותרת פנייה או שם ארגון.
- ה-stack והפרטים הטכניים נשארים בלוג בלבד. הלקוח מקבל ProblemDetails עם הודעה כללית בעברית.
- `EnableSensitiveDataLogging` של EF אינו מופעל.
- הודעות לוג הן תבניות עם פרמטרים (`{RequestId}`), לא שרשור מחרוזות.

## קלינט

`errorInterceptor` ממיר כל שגיאת HTTP ל-`ApiError` ורושם ל-console רק method, כתובת וסטטוס. הכתובת כוללת פרמטרי חיפוש, והם נחשפים רק בדפדפן של המשתמש עצמו.

## מגבלות

- אין Logging בשכבת ה-Repository (שגיאות עולות ל-Handler).
- אין Correlation ID שמקשר בין בקשה ללוגים שלה.
- הלוגים נכתבים לקונסול בלבד, ללא איסוף מרכזי.
