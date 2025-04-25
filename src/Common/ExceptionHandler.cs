using System.Net;
using Microsoft.AspNetCore.Diagnostics;
namespace ConwayGameOfLife_NET9.Common;
public sealed class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle an exception that occurred during HTTP request processing.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that was thrown during request processing.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> containing a boolean that indicates whether the exception was successfully handled.
    /// Returns true if the exception was handled; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method logs the exception details, sets appropriate response status codes based on the exception type,
    /// and returns a JSON error response to the client. The response includes error information with varying levels
    /// of detail depending on the type of exception.
    /// 
    /// Status codes are mapped as follows:
    /// - ArgumentException: 400 (Bad Request)
    /// - UnauthorizedAccessException: 401 (Unauthorized)
    /// - All other exceptions: 500 (Internal Server Error)
    /// 
    /// For security reasons, stack traces are only included for non-500 responses.
    /// </remarks>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Check if operation has been cancelled
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        // Log the exception with request path for diagnostic purposes
        logger.LogError(exception, "An error occurred while processing the request. Path: {Path}", httpContext.Request.Path);

        // Set response content type to JSON
        httpContext.Response.ContentType = "application/json";

        // Determine appropriate HTTP status code based on exception type
        var statusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,           // 400
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized, // 401
            _ => (int)HttpStatusCode.InternalServerError                   // 500
        };

        // Set the response status code
        httpContext.Response.StatusCode = statusCode;

        // Create error response object with appropriate level of detail
        // For 500 errors, provide generic message and hide stack trace for security
        var errorResponse = new
        {
            error = "An error occurred",
            message = statusCode == (int)HttpStatusCode.InternalServerError ? "Internal Server Error" : exception.Message,
            details = statusCode == (int)HttpStatusCode.InternalServerError ? null : exception.StackTrace
        };

        // Write the JSON response to the output stream
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        // Indicate that the exception was handled
        return true;
    }
}