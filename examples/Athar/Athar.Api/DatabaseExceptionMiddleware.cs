using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Athar.Api;

public sealed class DatabaseExceptionMiddleware(
    RequestDelegate next,
    ILogger<DatabaseExceptionMiddleware> logger)
{
    private static readonly Action<ILogger, string, Exception?> ConcurrencyConflict =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2001, nameof(ConcurrencyConflict)),
            "Optimistic concurrency conflict for {Path}.");

    private static readonly Action<ILogger, string, Exception?> UniqueConstraintConflict =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2002, nameof(UniqueConstraintConflict)),
            "Unique constraint conflict for {Path}.");

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
                detail: "تم تعديل السجل من جلسة أخرى. حدّث البيانات ثم حاول مجددًا.",
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
                detail: "تم تنفيذ طلب مطابق مسبقًا أو توجد قيمة فريدة مستخدمة.",
                extensions: new Dictionary<string, object?>
                {
                    ["correlationId"] = context.TraceIdentifier
                }).ExecuteAsync(context);
        }
    }
}
