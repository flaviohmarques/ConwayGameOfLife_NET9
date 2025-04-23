using System.Net;
using Microsoft.AspNetCore.Diagnostics;
namespace ConwayGameOfLife_NET9.Common;
public sealed class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        logger.LogError(exception, "An error occurred while processing the request. Path: {Path}", httpContext.Request.Path);
        httpContext.Response.ContentType = "application/json";
        var statusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError
        };
        httpContext.Response.StatusCode = statusCode;
        var errorResponse = new
        {
            error = "An error occurred",
            message = statusCode == (int)HttpStatusCode.InternalServerError ? "Internal Server Error" : exception.Message,
            details = statusCode == (int)HttpStatusCode.InternalServerError ? null : exception.StackTrace
        };
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }
}