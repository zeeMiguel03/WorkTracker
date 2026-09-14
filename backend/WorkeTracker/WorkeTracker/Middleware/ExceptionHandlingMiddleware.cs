using Application.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private static readonly HashSet<string> AuthenticationErrorCodes =
            new(StringComparer.Ordinal)
            {
                "USER_NOT_AUTHENTICATED",
                "INVALID_USER_ID",
                "INVALID_PASSWORD",
                "INVALID_CREDENTIALS",
                "REFRESH_TOKEN_REQUIRED",
                "INVALID_REFRESH_TOKEN",
                "REFRESH_TOKEN_EXPIRED"
            };

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException exception)
            {
                await HandleDomainExceptionAsync(context, exception);
            }
            catch (UnauthorizedException exception)
            {
                await HandleUnauthorizedExceptionAsync(context, exception);
            }
            catch (Exception exception)
            {
                await HandleUnexpectedExceptionAsync(context, exception);
            }
        }

        private async Task HandleDomainExceptionAsync(HttpContext context, DomainException exception)
        {
            _logger.LogWarning(
                exception,
                "Domain exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message,
                Instance = context.Request.Path
            };

            problem.Extensions["code"] = exception.Code;

            if (exception.Params is not null)
            {
                problem.Extensions["params"] = exception.Params;
            }

            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problem);
        }

        private async Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedException exception)
        {
            var statusCode = AuthenticationErrorCodes.Contains(exception.Code)
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status403Forbidden;

            _logger.LogWarning(
                exception,
                "Authorization exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden",
                Detail = exception.Message,
                Instance = context.Request.Path
            };

            problem.Extensions["code"] = exception.Code;

            if (exception.Params is not null)
            {
                problem.Extensions["params"] = exception.Params;
            }

            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problem);
        }

        private async Task HandleUnexpectedExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred.",
                Instance = context.Request.Path
            };

            problem.Extensions["code"] = "UNEXPECTED_ERROR";
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
