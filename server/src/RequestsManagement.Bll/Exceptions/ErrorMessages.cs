using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Bll.Exceptions;

// User-facing error messages shared by the services.
internal static class ErrorMessages
{
    public const string StaleRequest = "הפניה עודכנה על ידי משתמש אחר. רענן את הנתונים ונסה שוב.";

    public const string InvalidActingUser = "המשתמש המבצע אינו קיים או אינו פעיל.";

    public const string InvalidAssignee = "המטפל שנבחר אינו קיים או אינו פעיל.";

    public static string RequestNotFound(int id) => $"פניה {id} לא נמצאה.";

    public static string TransitionNotAllowed(RequestStatus from, RequestStatus to) =>
        $"מעבר הסטטוס מ-{from} ל-{to} אינו מותר.";
}
