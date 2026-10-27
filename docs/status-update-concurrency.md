# עדכון סטטוס ו-Optimistic Concurrency

`PUT /api/requests/{id}` (`RequestsService.UpdateAsync`). הגוף: `status`, `priority`, `assignedToUserId`, `rowVersion` (הגרסה שהלקוח קרא לאחרונה), `changedByUserId`.

## קודי תגובה

| מצב | קוד | מקור |
|---|---|---|
| נשמר | 200 + הפנייה המעודכנת (כולל `RowVersion` חדש) | |
| פנייה לא קיימת | 404 | `NotFoundException` |
| סטטוס/עדיפות לא חוקיים, `rowVersion` חסר, `changedByUserId` לא חוקי | 400 | ולידציית DTO (`EnumDataType`, `Required`, `Range`) |
| מעבר סטטוס אסור, משתמש מבצע/מטפל לא קיים או לא פעיל | 422 + הודעה בעברית בשדה הרלוונטי | `BusinessValidationException` |
| `RowVersion` לא עדכני (עדכון מתחרה) | 409 | `ConflictException` |

כל החריגות הצפויות הופכות ל-ProblemDetails ב-`GlobalExceptionHandler`.

## מעברי סטטוס מותרים

מקור אמת יחיד: `StatusTransitions`.

| מ- | אל |
|---|---|
| New | InProgress |
| InProgress | Waiting, Completed |
| Waiting | InProgress, Completed |
| Completed | (סופי) |

שליחת אותו סטטוס (ללא שינוי) אינה מעבר ולכן תקינה; בעדכון מרוכז היא מדווחת כ-`AlreadyInStatus`.

## איך מובטח שרק עדכון אחד מצליח (ללא Lost Update)

1. `RowVersion` הוא עמודת `ROWVERSION` ב-SQL Server, והמסד מעלה אותו אוטומטית בכל `UPDATE`.
2. השרת קורא את הפנייה ובודק מהר את `RowVersion` של הלקוח (Fast path, ל-409 מיידי בלי עבודה מיותרת).
3. הבדיקה המחייבת היא במסד: `TryUpdateAsync` מציב את ה-`RowVersion` של הלקוח כ-`OriginalValue`, ו-EF Core מייצר `UPDATE ... WHERE Id = @id AND RowVersion = @original`.
4. אם בין הקריאה לשמירה עדכן מישהו אחר, אף שורה לא מושפעת, EF זורק `DbUpdateConcurrencyException`, והשרת מחזיר 409. ה-`ChangeTracker` מנוקה כדי שהישות הכושלת ושורת ההיסטוריה לא יישמרו בטעות.
5. כששני עדכונים רצים במקביל עם אותו `RowVersion`, ה-UPDATE הראשון מצליח ומשנה את הגרסה, והשני לא מתאים ל-WHERE ומקבל 409. הבדיקה בצד השרת/DB ולא ב-UI בלבד.

הלקוח מטפל ב-409 בהצגת הודעה וטעינה מחדש (ראו [request-details-page.md](request-details-page.md#טיפול-בשגיאות)).

## עקביות UpdatedAt, RowVersion והיסטוריה

- בכל שינוי (סטטוס, עדיפות או מטפל) `UpdatedAt = DateTime.UtcNow`, ו-`RowVersion` מתעדכן אוטומטית על ידי המסד באותו `UPDATE`.
- שינוי סטטוס כותב שורת היסטוריה (`ChangedAt` זהה ל-`UpdatedAt`) באותו `SaveChanges`, כלומר באותה טרנזקציה: אין שינוי בלי Audit ואין Audit בלי שינוי.
- אם לא השתנה דבר, לא מתבצעת כתיבה, ולכן `UpdatedAt` ו-`RowVersion` לא משתנים.
- אחרי שינוי סטטוס או עדיפות ה-Cache של הסיכום מתבטל (ראו [cache.md](cache.md)).
- התשובה נקראת מחדש מהמסד, ולכן הלקוח מקבל את ה-`RowVersion` האמיתי החדש.

## היסטוריית שינויים (Audit)

**אחסון:** טבלת `RequestStatusHistory` (`Id`, `RequestId`, `PreviousStatus`, `NewStatus`, `ChangedAt` ב-UTC, `ChangedByUserId`). `CHECK` על ערכי הסטטוס, ו-FK ל-`Requests` ול-`Users`. פירוט ב-[database-schema.md](database-schema.md).

**כתיבה:** שורה אחת לכל שינוי סטטוס, באותה טרנזקציה של העדכון (גם בעדכון מרוכז, שורה לכל פנייה שהצליחה). שינוי עדיפות או מטפל בלבד אינו נרשם, ועדכון שנכשל (409/422) לא משאיר שורה. נתוני ה-Seed הראשוניים אינם כוללים היסטוריה.

**Endpoint:** `GET /api/requests/{id}/history`

- 200: רשימה מהחדש לישן (`ChangedAt DESC`, `Id DESC` כ-tie-breaker), מוחזרת כ-DTO:
  `{ id, requestId, previousStatus, newStatus, changedAt, changedByUserId, changedBy }`. הסטטוסים כמחרוזות (`"New"`), ו-`changedBy` הוא שם התצוגה של המשתמש.
- פנייה קיימת ללא שינויים: 200 עם רשימה ריקה. פנייה שלא קיימת: 404. (בדיקת הקיום רצה רק כשהרשימה ריקה, כדי לחסוך שאילתה.)
- ביצועים: שאילתה אחת עם `AsNoTracking`, נתמכת באינדקס `IX_RequestStatusHistory_RequestId_ChangedAt` (seek לפי `RequestId`, בלי מיון נוסף). אין עימוד כי מספר השינויים לפנייה קטן (Waiting ו-InProgress מאפשרים מעבר חוזר, אבל בפועל מדובר בעשרות בודדות לכל היותר).
- הלקוח: ראו [request-details-page.md](request-details-page.md#הצגת-היסטוריה).

## מגבלה

`changedByUserId` נשלח מהלקוח כי אין אימות משתמשים במערכת.
