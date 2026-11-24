using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenDCP.Core.API.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions globally across the API.
    /// Catches unhandled exceptions, maps them to appropriate HTTP status codes,
    /// and returns standardized error responses to the client.
    /// </summary>
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate in the pipeline.</param>
        /// <param name="logger">The logger instance for recording exceptions.</param>
        /// <param name="configuration">The application configuration for accessing debug settings.</param>
        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Invokes the middleware to process the HTTP request.
        /// Catches any exceptions that occur during request processing and delegates
        /// them to <see cref="HandleExceptionAsync"/> for standardized error handling.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles exceptions by mapping them to appropriate HTTP status codes and
        /// formatting error responses according to the exception type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Exception to status code mapping:
        /// <list type="bullet">
        /// <item><see cref="DCPUnauthorizedException"/> → 401 Unauthorized</item>
        /// <item><see cref="DCPBadRequestException"/> or <see cref="DCPRawBadRequestException"/> → 400 Bad Request</item>
        /// <item><see cref="DCPDataSourceException"/> or <see cref="DCPCalculationException"/> → 422 Unprocessable Entity</item>
        /// <item><see cref="DCPForbiddenException"/> → 403 Forbidden</item>
        /// <item><see cref="DCPNotFoundException"/> → 404 Not Found</item>
        /// <item>All other exceptions → 500 Internal Server Error</item>
        /// </list>
        /// </para>
        /// <para>
        /// Special handling is provided for <see cref="DCPRawBadRequestException"/> to maintain
        /// backwards compatibility with legacy error response format.
        /// </para>
        /// <para>
        /// When <c>Debug:SanitizeError</c> is enabled (default: true), non-DCP exceptions
        /// return a generic error message. Stack traces are only included when sanitization
        /// is explicitly disabled.
        /// </para>
        /// </remarks>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="ex">The exception that was caught.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var statusCode = ex switch
            {
                DCPUnauthorizedException => StatusCodes.Status401Unauthorized,
                DCPBadRequestException => StatusCodes.Status400BadRequest,
                DCPRawBadRequestException => StatusCodes.Status400BadRequest,
                DCPDataSourceException => StatusCodes.Status422UnprocessableEntity,
                DCPCalculationException => StatusCodes.Status422UnprocessableEntity,
                DCPForbiddenException => StatusCodes.Status403Forbidden,
                DCPNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null // keeps PascalCase
            };

            if (ex is DCPRawBadRequestException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                // 🔥 Normalize legacy format
                var normalizedMessage = ex.Message
                    .Replace("\\u0027", "'")
                    .Replace("\\u0022", "\"");

                var oldError = new JsonObject
                {
                    ["Message"] = normalizedMessage
                };

                options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                await context.Response.WriteAsJsonAsync(oldError, options);
                return;
            }

            var error = new ErrorResponse
            {
                Message = (_configuration.GetValue<bool>("Debug:SanitizeError", true) && !(ex is DCPBaseException))
                    ? "An unexpected error occurred"
                    : ex.Message,
            };

            if (!_configuration.GetValue<bool>("Debug:SanitizeError", true))
            {
                error.StackTrace = ex?.StackTrace;
            }

            _logger.LogError(ex, "Unhandled exception. ErrorId: {ErrorId}", error.ErrorId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(error, options);
        }
    }
}