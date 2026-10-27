using Microsoft.AspNetCore.Mvc;
using RequestsManagement.Bll.Mapping.Dtos;
using RequestsManagement.Bll.Services;

namespace RequestsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RequestsController : ControllerBase
{
    private readonly IRequestsService _requestsService;

    public RequestsController(IRequestsService requestsService)
    {
        _requestsService = requestsService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<RequestsSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return await _requestsService.GetSummaryAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestListItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return await _requestsService.GetAsync(id, cancellationToken);
    }

    [HttpGet("{id:int}/history")]
    [ProducesResponseType<IReadOnlyList<StatusHistoryItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StatusHistoryItemDto>>> GetHistory(
        int id, CancellationToken cancellationToken)
    {
        return Ok(await _requestsService.GetHistoryAsync(id, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<RequestListItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RequestListItemDto>> Update(
        int id, [FromBody] UpdateRequestDto dto, CancellationToken cancellationToken)
    {
        return await _requestsService.UpdateAsync(id, dto, cancellationToken);
    }

    [HttpPost("bulk-status")]
    [ProducesResponseType<BulkStatusResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BulkStatusResultDto>> BulkUpdateStatus(
        [FromBody] BulkStatusUpdateDto dto, CancellationToken cancellationToken)
    {
        return await _requestsService.BulkUpdateStatusAsync(dto, cancellationToken);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<RequestListItemDto>>> Search(
        [FromQuery] RequestsQueryDto query, CancellationToken cancellationToken)
    {
        return await _requestsService.SearchAsync(query, cancellationToken);
    }
}
