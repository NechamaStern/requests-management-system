using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using RequestsManagement.Bll.Exceptions;
using RequestsManagement.Bll.Mapping.Dtos;
using RequestsManagement.Bll.Services;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Tests;

// The summary is cached; these tests check that it is served from the cache and that it never goes stale
// after an update that changes the counts.
public sealed class SummaryCacheTests
{
    private readonly FakeRequestsRepository _repository = new();
    private readonly RequestsService _service;

    public SummaryCacheTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(RequestsService).Assembly), NullLoggerFactory.Instance).CreateMapper();
        _service = new RequestsService(
            _repository, new MemoryCache(new MemoryCacheOptions()), mapper, NullLogger<RequestsService>.Instance);
    }

    private static UpdateRequestDto Update(RequestStatus status, RequestPriority priority, byte[] rowVersion) => new()
    {
        Status = status,
        Priority = priority,
        RowVersion = rowVersion,
        ChangedByUserId = FakeRequestsRepository.ValidUserId,
    };

    [Fact]
    public async Task GetSummary_CalledTwice_QueriesTheDatabaseOnce()
    {
        _repository.Add(1, RequestStatus.New);

        await _service.GetSummaryAsync(CancellationToken.None);
        await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(1, _repository.SummaryQueries);
    }

    [Fact]
    public async Task Update_StatusChanged_SummaryReflectsTheChange()
    {
        var request = _repository.Add(1, RequestStatus.New);
        var before = await _service.GetSummaryAsync(CancellationToken.None);
        Assert.Equal(1, before.ByStatus.Single(c => c.Key == nameof(RequestStatus.New)).Count);

        await _service.UpdateAsync(
            1, Update(RequestStatus.InProgress, request.Priority, request.RowVersion), CancellationToken.None);
        var after = await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(2, _repository.SummaryQueries);
        Assert.DoesNotContain(after.ByStatus, c => c.Key == nameof(RequestStatus.New));
        Assert.Equal(1, after.ByStatus.Single(c => c.Key == nameof(RequestStatus.InProgress)).Count);
    }

    [Fact]
    public async Task Update_OnlyAssigneeChanged_KeepsTheCachedSummary()
    {
        var request = _repository.Add(1, RequestStatus.New);
        await _service.GetSummaryAsync(CancellationToken.None);

        var dto = new UpdateRequestDto
        {
            Status = request.Status,
            Priority = request.Priority,
            AssignedToUserId = FakeRequestsRepository.ValidUserId,
            RowVersion = request.RowVersion,
            ChangedByUserId = FakeRequestsRepository.ValidUserId,
        };
        await _service.UpdateAsync(1, dto, CancellationToken.None);
        await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(1, _repository.SummaryQueries);
    }

    [Fact]
    public async Task Update_ConcurrencyConflict_KeepsTheCachedSummary()
    {
        var request = _repository.Add(1, RequestStatus.New);
        await _service.GetSummaryAsync(CancellationToken.None);
        _repository.RejectNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(
            1, Update(RequestStatus.InProgress, request.Priority, request.RowVersion), CancellationToken.None));
        await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(1, _repository.SummaryQueries);
    }

    [Fact]
    public async Task BulkUpdate_AtLeastOneSucceeded_InvalidatesTheSummary()
    {
        var first = _repository.Add(1, RequestStatus.New);
        _repository.Add(2, RequestStatus.New);
        await _service.GetSummaryAsync(CancellationToken.None);

        var result = await _service.BulkUpdateStatusAsync(new BulkStatusUpdateDto
        {
            Status = RequestStatus.InProgress,
            ChangedByUserId = FakeRequestsRepository.ValidUserId,
            Items =
            [
                new BulkStatusUpdateItemDto { Id = first.Id, RowVersion = first.RowVersion },
                new BulkStatusUpdateItemDto { Id = 2, RowVersion = [99] },
            ],
        }, CancellationToken.None);
        await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(2, _repository.SummaryQueries);
    }

    [Fact]
    public async Task BulkUpdate_AllFailed_KeepsTheCachedSummary()
    {
        _repository.Add(1, RequestStatus.New);
        await _service.GetSummaryAsync(CancellationToken.None);

        var result = await _service.BulkUpdateStatusAsync(new BulkStatusUpdateDto
        {
            Status = RequestStatus.InProgress,
            ChangedByUserId = FakeRequestsRepository.ValidUserId,
            Items = [new BulkStatusUpdateItemDto { Id = 1, RowVersion = [99] }],
        }, CancellationToken.None);
        await _service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(0, result.SucceededCount);
        Assert.Equal(1, _repository.SummaryQueries);
    }
}
