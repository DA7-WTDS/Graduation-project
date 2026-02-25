using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Project.Api.Middlewares;

internal sealed class DatabaseExceptionHandler(
    ILogger<DatabaseExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException dbEx || dbEx.InnerException is not PostgresException pgEx)
        {
            return false;
        }

        // Log comprehensive error details
        logger.LogError(pgEx, 
            "Database exception occurred.\n" +
            "PostgreSQL Error Code: {SqlState}\n" +
            "PostgreSQL Message: {Message}\n" +
            "DbUpdateException Message: {DbMessage}\n" +
            "Affected Entities: {Entities}\n" +
            "Full Exception: {FullException}",
            pgEx.SqlState,
            pgEx.Message,
            dbEx.Message,
            string.Join(", ", dbEx.Entries.Select(e => $"{e.Entity.GetType().Name} (State: {e.State})")),
            exception.ToString());

        int status = pgEx.SqlState switch
        {
            PostgresErrorCodes.ForeignKeyViolation => StatusCodes.Status400BadRequest,
            PostgresErrorCodes.UniqueViolation => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = status;
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Detail = "A database constraint was violated."
            }
        };

        return await problemDetailsService.TryWriteAsync(context);
    }
}


