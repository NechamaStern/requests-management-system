using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RequestsManagement.Bll.Exceptions;

namespace RequestsManagement.Api.Middleware;

// Turns unhandled exceptions into a ProblemDetails response with a user-facing message.
// Details are logged server-side only and never returned to the client.
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        if (exception is NotFoundException or ConflictException or BusinessValidationException)
        {
            return await WriteExpectedFailureAsync(httpContext, exception);
        }

        _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "אירעה שגיאה בשרת. נסו שוב מאוחר יותר.",
            },
        });
    }

    // Business failures are expected: logged as warnings and returned with the matching 4xx status.
    private async ValueTask<bool> WriteExpectedFailureAsync(HttpContext httpContext, Exception exception)
    {
        _logger.LogWarning("Request rejected on {Method} {Path}: {Reason}",
            httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        ProblemDetails problem = exception switch
        {
            NotFoundException => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not Found" },
            ConflictException => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflict" },
            BusinessValidationException v => new ValidationProblemDetails(
                new Dictionary<string, string[]> { [v.Field] = [v.Message] })
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation failed",
            },
            _ => throw new InvalidOperationException(),
        };
        problem.Detail = exception.Message;

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }
}
