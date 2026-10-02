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
                "INVALID_GOOGLE_TOKEN",
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
            catch (ConcurrencyException)
            {
                await HandleConflictAsync(context);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException exception)
                when (exception.InnerException is Microsoft.Data.SqlClient.SqlException
                    { Number: 2601 or 2627 })
            {
                await HandleConflictAsync(context);
            }
            catch (Exception exception)
            {
                await HandleUnexpectedExceptionAsync(context, exception);
            }
        }

        private static async Task HandleConflictAsync(HttpContext context)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "Os dados foram alterados por outro pedido. Atualiza e tenta novamente.",
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
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

            // Missing/expired credentials are expected during session restore
            // (for example, when a user opens the app without a refresh cookie).
            // Keep the response as 401, but avoid logging a noisy stack trace as
            // if this were an application failure.
            if (statusCode == StatusCodes.Status401Unauthorized)
            {
                _logger.LogInformation(
                    "Authentication rejected for {Method} {Path}: {Code}",
                    context.Request.Method,
                    context.Request.Path,
                    exception.Code);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Authorization exception while processing {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
            }

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
