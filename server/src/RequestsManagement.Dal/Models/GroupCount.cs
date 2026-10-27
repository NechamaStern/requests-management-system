namespace RequestsManagement.Dal.Models;

public sealed record GroupCount<TKey>(TKey Key, int Count);
