using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Athar.Api;

public sealed class DatabaseExceptionMiddleware(
    RequestDelegate next,
    ILogger<DatabaseExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(
                exception,
                "Optimistic concurrency conflict for {Path}.",
                context.Request.Path);

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
            logger.LogInformation(
                exception,
                "Unique constraint conflict for {Path}.",
                context.Request.Path);

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
