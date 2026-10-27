# לקוח Angular: מיפוי דרישות

| דרישה | מימוש | קובץ |
|---|---|---|
| חיפוש, סינון, מיון ועימוד בצד השרת | כל שינוי מעדכן את ה-URL, ומשם נשלחת בקשה ל-`GET /api/requests`. הלקוח לא מסנן, ממיין או מחלק לעמודים | `requests-list.ts`, `requests.service.ts` |
| Debounce לחיפוש | `debounceTime(400)` + `distinctUntilChanged()` על שדה החיפוש החופשי. שדות הסינון המתקדמים נשלחים רק בלחיצה על "חפש" | `requests-filters.ts` |
| ביטול והחלפת בקשות קודמות | `switchMap` על `queryParams`: בקשה חדשה מבטלת את ההמתנה לקודמת (ב-HTTP היא מבוטלת בפועל). `catchError` בתוך ה-`switchMap`, כך ששגיאה אחת לא סוגרת את ה-stream | `requests-list.ts` |
| Loading / Empty / Error | טעינה ראשונה ("טוען נתונים..."), טעינות הבאות מעומעמות עם `aria-busy`, ריק ("לא נמצאו פניות") ושגיאה עם "נסה שוב" כשהיא ניתנת לניסיון חוזר. אותם מצבים בדשבורד, בהיסטוריה ובמשתמשים | `requests-list.html`, `dashboard.ts`, `request-history.ts` |
| עדכון סטטוס וטיפול ב-409 | דרואר פרטים עם `PUT` ו-`rowVersion`. ב-409 מוצגת הודעה וכפתור "טען נתונים עדכניים". ב-422 וב-404 מוצגת הודעת השרת | `request-details.ts` |
| Aggregations | Dashboard: פילוח לפי סטטוס ולפי עדיפות מ-`GET /api/requests/summary` | `dashboard.ts`, `breakdown-card` |
| היסטוריית שינויים | `GET /api/requests/{id}/history`, נטענת מחדש בכל שינוי `rowVersion` | `request-history.ts` |
| עדכון מרוכז | בחירה בעמוד, סרגל פעולות ופאנל תוצאות | `bulk-actions-bar`, `bulk-result` |

## הפרדה בין Components ל-Services

- **Components** מנהלים מצב תצוגה (signals) ולא מכירים HTTP גולמי. קומפוננטות תצוגה (`request-card`, `requests-sort`, `pagination`, `requests-filters`) מקבלות `input` ומחזירות `output`.
- **Services:** `services/api` (`requests.service.ts`, `users.service.ts`) הם היחידים שקוראים ל-`HttpClient`. `services/interceptors` ממיר שגיאות HTTP ל-`ApiError` במקום אחד. `services/utils` מחזיק פונקציות טהורות (המרת URL לפילטרים ושגיאות לטקסט).
- **Models** מכילים טיפוסים בלבד.

פירוט נוסף: [requests-list-page.md](requests-list-page.md), [request-details-page.md](request-details-page.md), [bulk-status-update.md](bulk-status-update.md).

## מגבלות

- `changedByUserId` קבוע בלקוח (`CURRENT_USER_ID`) כי אין אימות משתמשים.
- אין בדיקות אוטומטיות משמעותיות ללקוח (קיים רק `app.spec.ts` בסיסי).
