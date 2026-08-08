using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Madar.Api;

public sealed class DatabaseExceptionMiddleware(
    RequestDelegate next,
    ILogger<DatabaseExceptionMiddleware> logger)
{
    private static readonly Action<ILogger, string, Exception?> ConcurrencyConflict =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3001, nameof(ConcurrencyConflict)),
            "Madar optimistic concurrency conflict for {Path}.");

    private static readonly Action<ILogger, string, Exception?> UniqueConstraintConflict =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(3002, nameof(UniqueConstraintConflict)),
            "Madar unique constraint conflict for {Path}.");

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            ConcurrencyConflict(
                logger,
                context.Request.Path.Value ?? "/",
                exception);

            await Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "ConcurrencyConflict",
                detail: "تم تعديل الحالة من جلسة أخرى. حدّث البيانات ثم حاول مجددًا.",
                extensions: new Dictionary<string, object?>
                {
                    ["correlationId"] = context.TraceIdentifier
                }).ExecuteAsync(context);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException
            {
                Number: 2601 or 2627
            })
        {
            UniqueConstraintConflict(
                logger,
                context.Request.Path.Value ?? "/",
                exception);

            await Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "DuplicateRequest",
                detail: "تعارض الطلب مع قيمة فريدة موجودة مسبقًا.",
                extensions: new Dictionary<string, object?>
                {
                    ["correlationId"] = context.TraceIdentifier
                }).ExecuteAsync(context);
        }
    }
}
