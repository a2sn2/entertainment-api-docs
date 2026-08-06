using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Athar.Application;
using Athar.Contracts;
using Athar.Infrastructure;
using FoundationKit.Application.Pagination;
using FoundationKit.WebApi.Results;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Athar.Api;

public static class AtharEndpoints
{
    public static IEndpointRouteBuilder MapAtharEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () =>
                Results.Ok(new
                {
                    status = "healthy",
                    service = "athar-api"
                }))
            .WithTags("الصحة")
            .WithName("AtharLive");

        endpoints.MapGet("/health/ready", async (
                AtharDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var connected = await dbContext.Database
                    .CanConnectAsync(cancellationToken);

                return connected
                    ? Results.Ok(new
                    {
                        status = "ready",
                        database = "sql-server"
                    })
                    : Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "DatabaseUnavailable",
                        detail: "تعذر الاتصال بقاعدة البيانات.");
            })
            .WithTags("الصحة")
            .WithName("AtharReady");

        endpoints.MapGet(
                $"/{AtharRoutes.SecurityToken}",
                (IAntiforgery antiforgery, HttpContext context) =>
                {
                    var tokens = antiforgery.GetAndStoreTokens(context);
                    return Results.Ok(new AntiforgeryTokenResponse(
                        tokens.RequestToken
                        ?? throw new InvalidOperationException(
                            "Antiforgery request token was not generated.")));
                })
            .WithTags("الأمان")
            .WithName("GetAtharAntiforgeryToken");

        MapAuthentication(endpoints);
        MapInitiatives(endpoints);
        return endpoints;
    }

    private static void MapAuthentication(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints
            .MapGroup($"/{AtharRoutes.ApiRoot}/auth")
            .WithTags("الحساب");

        auth.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("auth")
            .WithName("RegisterAtharUser")
            .Produces<CurrentUserResponse>()
            .ProducesValidationProblem();

        auth.MapPost("/login", LoginAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("auth")
            .WithName("LoginAtharUser")
            .Produces<CurrentUserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/logout", async (
                SignInManager<AtharUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Ok(new ApiMessageResponse(
                    "تم تسجيل الخروج بنجاح."));
            })
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .WithName("LogoutAtharUser");

        auth.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetAtharCurrentUser")
            .Produces<CurrentUserResponse>();
    }

    private static void MapInitiatives(IEndpointRouteBuilder endpoints)
    {
        var initiatives = endpoints
            .MapGroup($"/{AtharRoutes.ApiRoot}/initiatives")
            .RequireAuthorization("AtharUser")
            .WithTags("المبادرات");

        initiatives.MapPost("/", async (
                CreateInitiativeRequest request,
                IInitiativeManager manager,
                CancellationToken cancellationToken) =>
                (await manager.CreateAsync(request, cancellationToken))
                    .ToHttpResult(value => Results.Created(
                        $"/{AtharRoutes.Initiative(value.Id)}",
                        value)))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("CreateAtharInitiative")
            .Produces<InitiativeDetailsDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        initiatives.MapGet("/mine", async (
                int page,
                int pageSize,
                string? search,
                string? status,
                IInitiativeManager manager,
                CancellationToken cancellationToken) =>
                (await manager.GetMineAsync(
                    new InitiativeSearchRequest
                    {
                        Page = page <= 0 ? 1 : page,
                        PageSize = pageSize <= 0
                            ? PageRequest.DefaultPageSize
                            : pageSize,
                        Search = search,
                        Status = status
                    },
                    cancellationToken))
                .ToHttpResult(Results.Ok))
            .WithName("GetMyAtharInitiatives")
            .Produces<PagedResult<InitiativeSummaryDto>>();

        initiatives.MapGet("/{id:guid}", async (
                Guid id,
                IInitiativeManager manager,
                CancellationToken cancellationToken) =>
                (await manager.GetAsync(id, cancellationToken))
                    .ToHttpResult(Results.Ok))
            .WithName("GetAtharInitiative")
            .Produces<InitiativeDetailsDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var admin = endpoints
            .MapGroup($"/{AtharRoutes.ApiRoot}/admin")
            .RequireAuthorization("AtharAdministrator")
            .WithTags("الإدارة");

        admin.MapGet("/dashboard", async (
                IInitiativeManager manager,
                CancellationToken cancellationToken) =>
                (await manager.GetDashboardAsync(cancellationToken))
                    .ToHttpResult(Results.Ok))
            .WithName("GetAtharAdminDashboard")
            .Produces<AdminDashboardResponse>();

        admin.MapGet("/initiatives", async (
                int page,
                int pageSize,
                string? search,
                string? status,
                IInitiativeManager manager,
                CancellationToken cancellationToken) =>
                (await manager.GetAdminQueueAsync(
                    new InitiativeSearchRequest
                    {
                        Page = page <= 0 ? 1 : page,
                        PageSize = pageSize <= 0
                            ? PageRequest.DefaultPageSize
                            : pageSize,
                        Search = search,
                        Status = status
                    },
                    cancellationToken))
                .ToHttpResult(Results.Ok))
            .WithName("GetAtharAdminInitiatives")
            .Produces<PagedResult<InitiativeSummaryDto>>();

        admin.MapPost("/initiatives/{id:guid}/review", async (
                Guid id,
                ReviewInitiativeRequest request,
                IInitiativeManager manager,
                CancellationToken cancellationToken) =>
                (await manager.ReviewAsync(
                    id,
                    request,
                    cancellationToken))
                .ToHttpResult(Results.Ok))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("write")
            .WithName("ReviewAtharInitiative")
            .Produces<InitiativeDetailsDto>()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<AtharUser> userManager,
        SignInManager<AtharUser> signInManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        if (await userManager.FindByEmailAsync(request.Email.Trim()) is not null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Email)] =
                    ["البريد الإلكتروني مستخدم مسبقًا."]
            });
        }

        var user = new AtharUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            CreatedUtc = DateTimeOffset.UtcNow
        };

        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            return IdentityValidationProblem(create);

        var role = await userManager.AddToRoleAsync(user, AtharRoles.User);
        if (!role.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return IdentityValidationProblem(role);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Ok(await ToCurrentUserAsync(user, userManager));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<AtharUser> userManager,
        SignInManager<AtharUser> signInManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var result = await signInManager.PasswordSignInAsync(
            request.Email.Trim(),
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: result.IsLockedOut
                    ? "AccountLocked"
                    : "InvalidCredentials",
                detail: result.IsLockedOut
                    ? "تم قفل الحساب مؤقتًا بسبب محاولات متكررة."
                    : "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new InvalidOperationException(
                "Authenticated user was not found.");

        return Results.Ok(await ToCurrentUserAsync(user, userManager));
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<AtharUser> userManager)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(new CurrentUserResponse(
                null,
                null,
                null,
                [],
                false));
        }

        var user = await userManager.GetUserAsync(principal);
        return user is null
            ? Results.Ok(new CurrentUserResponse(
                null,
                null,
                null,
                [],
                false))
            : Results.Ok(await ToCurrentUserAsync(user, userManager));
    }

    private static async Task<CurrentUserResponse> ToCurrentUserAsync(
        AtharUser user,
        UserManager<AtharUser> userManager) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            (await userManager.GetRolesAsync(user)).ToArray(),
            true);

    private static Dictionary<string, string[]>? Validate<T>(T request)
    {
        var context = new ValidationContext(request!);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(
            request!,
            context,
            results,
            validateAllProperties: true))
        {
            return null;
        }

        return results
            .SelectMany(result =>
                (result.MemberNames.Any()
                    ? result.MemberNames
                    : [string.Empty])
                .Select(member => new
                {
                    Member = member,
                    Message = result.ErrorMessage
                        ?? "القيمة غير صالحة."
                }))
            .GroupBy(item => item.Member)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Message).ToArray());
    }

    private static IResult IdentityValidationProblem(
        IdentityResult result) =>
        Results.ValidationProblem(
            result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.Description)
                        .ToArray()));
}

public sealed class CurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var id)
                ? id
                : null;
        }
    }

    public bool IsInRole(string role) =>
        Principal?.IsInRole(role) == true;
}

public sealed class AntiforgeryEndpointFilter(
    IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
            return await next(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "InvalidAntiforgeryToken",
                detail: "انتهت صلاحية جلسة الحماية. أعد تحميل الصفحة وحاول مرة أخرى.");
        }
    }
}
