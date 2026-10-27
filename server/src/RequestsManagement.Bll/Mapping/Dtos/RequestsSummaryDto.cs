namespace RequestsManagement.Bll.Mapping.Dtos;

public sealed record CountItemDto(string Key, int Count);

public sealed record RequestsSummaryDto(
    int Total,
    IReadOnlyList<CountItemDto> ByStatus,
    IReadOnlyList<CountItemDto> ByPriority);
