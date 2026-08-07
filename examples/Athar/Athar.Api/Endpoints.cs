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
using Microsoft.Extensions.Options;

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
                        status = "ready"
                    })
                    : Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "DependencyUnavailable",
                        detail: "تعذر إكمال فحص الجاهزية.");
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

        auth.MapPost("/login/2fa", TwoFactorLoginAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("auth")
            .WithName("LoginAtharUserTwoFactor")
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

        auth.MapPost("/email/request-confirmation", RequestEmailConfirmationAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("auth")
            .WithName("RequestAtharEmailConfirmation")
            .Produces<ApiMessageResponse>();

        auth.MapPost("/email/confirm", ConfirmEmailAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("auth")
            .WithName("ConfirmAtharEmail")
            .Produces<ApiMessageResponse>();

        auth.MapPost("/password/forgot", ForgotPasswordAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("auth")
            .WithName("RequestAtharPasswordReset")
            .Produces<ApiMessageResponse>();

        auth.MapPost("/password/reset", ResetPasswordAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireRateLimiting("auth")
            .WithName("ResetAtharPassword")
            .Produces<ApiMessageResponse>();

        auth.MapPost("/password/change", ChangePasswordAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithName("ChangeAtharPassword")
            .Produces<ApiMessageResponse>();

        auth.MapGet("/mfa/status", GetMfaStatusAsync)
            .RequireAuthorization()
            .WithName("GetAtharMfaStatus")
            .Produces<MfaStatusResponse>();

        auth.MapPost("/mfa/setup", SetupMfaAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithName("SetupAtharMfa")
            .Produces<MfaSetupResponse>();

        auth.MapPost("/mfa/enable", EnableMfaAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithName("EnableAtharMfa")
            .Produces<MfaEnableResponse>();

        auth.MapPost("/mfa/disable", DisableMfaAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithName("DisableAtharMfa")
            .Produces<ApiMessageResponse>();

        auth.MapPost("/mfa/recovery-codes", RegenerateRecoveryCodesAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithName("RegenerateAtharMfaRecoveryCodes")
            .Produces<MfaEnableResponse>();
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
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<AtharUser> userManager,
        SignInManager<AtharUser> signInManager,
        IAccountNotificationSender notificationSender,
        IOptions<AccountSecurityOptions> securityOptions,
        CancellationToken cancellationToken)
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

        if (securityOptions.Value.RequireConfirmedEmail)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var delivered = await notificationSender.SendEmailConfirmationAsync(
                user.Email!,
                token,
                cancellationToken);

            if (!delivered)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "AccountNotificationUnavailable",
                    detail: "تعذر إرسال رسالة تأكيد الحساب. حاول لاحقًا.");
            }

            return Results.Ok(await ToCurrentUserAsync(
                user,
                userManager,
                isAuthenticated: false));
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

        if (result.RequiresTwoFactor)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "TwoFactorRequired",
                detail: "أدخل رمز المصادقة الثنائية أو أحد رموز الاسترداد لإكمال تسجيل الدخول.");
        }

        if (!result.Succeeded)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: result.IsLockedOut
                    ? "AccountLocked"
                    : result.IsNotAllowed
                        ? "AccountNotAllowed"
                        : "InvalidCredentials",
                detail: result.IsLockedOut
                    ? "تم قفل الحساب مؤقتًا بسبب محاولات متكررة."
                    : result.IsNotAllowed
                        ? "الحساب غير جاهز لتسجيل الدخول. تحقق من البريد الإلكتروني ومتطلبات الأمان."
                        : "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim())
            ?? throw new InvalidOperationException(
                "Authenticated user was not found.");

        return Results.Ok(await ToCurrentUserAsync(user, userManager));
    }

    private static async Task<IResult> TwoFactorLoginAsync(
        TwoFactorLoginRequest request,
        UserManager<AtharUser> userManager,
        SignInManager<AtharUser> signInManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "TwoFactorSessionMissing",
                detail: "أعد تسجيل الدخول بكلمة المرور قبل إدخال رمز المصادقة الثنائية.");
        }

        var normalizedCode = request.Code
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

        SignInResult result;
        if (normalizedCode.Length == 6
            && normalizedCode.All(char.IsDigit))
        {
            result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                normalizedCode,
                request.RememberMe,
                request.RememberMachine);
        }
        else
        {
            result = await signInManager.TwoFactorRecoveryCodeSignInAsync(
                normalizedCode);
        }

        if (!result.Succeeded)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: result.IsLockedOut ? "AccountLocked" : "InvalidTwoFactorCode",
                detail: result.IsLockedOut
                    ? "تم قفل الحساب مؤقتًا بسبب محاولات متكررة."
                    : "رمز المصادقة الثنائية أو الاسترداد غير صحيح.");
        }

        return Results.Ok(await ToCurrentUserAsync(user, userManager));
    }

    private static async Task<IResult> RequestEmailConfirmationAsync(
        EmailAddressRequest request,
        UserManager<AtharUser> userManager,
        IAccountNotificationSender notificationSender,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && !await userManager.IsEmailConfirmedAsync(user))
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await notificationSender.SendEmailConfirmationAsync(
                user.Email!,
                token,
                cancellationToken);
        }

        return Results.Ok(GenericAccountNotificationResponse());
    }

    private static async Task<IResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        UserManager<AtharUser> userManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return Results.ValidationProblem(GenericInvalidTokenProblem());

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded
            ? Results.Ok(new ApiMessageResponse("تم تأكيد البريد الإلكتروني بنجاح."))
            : IdentityValidationProblem(result);
    }

    private static async Task<IResult> ForgotPasswordAsync(
        EmailAddressRequest request,
        UserManager<AtharUser> userManager,
        IAccountNotificationSender notificationSender,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && await userManager.IsEmailConfirmedAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await notificationSender.SendPasswordResetAsync(
                user.Email!,
                token,
                cancellationToken);
        }

        return Results.Ok(GenericAccountNotificationResponse());
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        UserManager<AtharUser> userManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return Results.ValidationProblem(GenericInvalidTokenProblem());

        var result = await userManager.ResetPasswordAsync(
            user,
            request.Token,
            request.NewPassword);

        return result.Succeeded
            ? Results.Ok(new ApiMessageResponse(
                "تم تحديث كلمة المرور. سجّل الدخول من جديد."))
            : IdentityValidationProblem(result);
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        UserManager<AtharUser> userManager,
        SignInManager<AtharUser> signInManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var result = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
            return IdentityValidationProblem(result);

        await signInManager.RefreshSignInAsync(user);
        return Results.Ok(new ApiMessageResponse(
            "تم تغيير كلمة المرور بنجاح."));
    }

    private static async Task<IResult> GetMfaStatusAsync(
        ClaimsPrincipal principal,
        UserManager<AtharUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        return Results.Ok(new MfaStatusResponse(
            await userManager.GetTwoFactorEnabledAsync(user),
            await userManager.IsEmailConfirmedAsync(user),
            await userManager.CountRecoveryCodesAsync(user)));
    }

    private static async Task<IResult> SetupMfaAsync(
        MfaSetupRequest request,
        ClaimsPrincipal principal,
        UserManager<AtharUser> userManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "ReauthenticationRequired",
                detail: "كلمة المرور الحالية غير صحيحة.");
        }

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            var reset = await userManager.ResetAuthenticatorKeyAsync(user);
            if (!reset.Succeeded)
                return IdentityValidationProblem(reset);

            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "MfaKeyUnavailable",
                detail: "تعذر إنشاء مفتاح المصادقة الثنائية.");
        }

        var email = user.Email ?? user.UserName ?? user.Id.ToString();
        var uri = $"otpauth://totp/Athar:{Uri.EscapeDataString(email)}?secret={Uri.EscapeDataString(key)}&issuer=Athar&digits=6";

        return Results.Ok(new MfaSetupResponse(key, uri));
    }

    private static async Task<IResult> EnableMfaAsync(
        MfaCodeRequest request,
        ClaimsPrincipal principal,
        UserManager<AtharUser> userManager,
        SignInManager<AtharUser> signInManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var code = request.Code
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var valid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            code);

        if (!valid)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "InvalidTwoFactorCode",
                detail: "رمز تطبيق المصادقة غير صحيح.");
        }

        var enabled = await userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enabled.Succeeded)
            return IdentityValidationProblem(enabled);

        var recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                user,
                10))
            ?.ToArray()
            ?? [];

        await userManager.UpdateSecurityStampAsync(user);
        await signInManager.SignOutAsync();

        return Results.Ok(new MfaEnableResponse(recoveryCodes));
    }

    private static async Task<IResult> DisableMfaAsync(
        MfaDisableRequest request,
        ClaimsPrincipal principal,
        UserManager<AtharUser> userManager,
        SignInManager<AtharUser> signInManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "ReauthenticationRequired",
                detail: "كلمة المرور الحالية غير صحيحة.");
        }

        var disabled = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disabled.Succeeded)
            return IdentityValidationProblem(disabled);

        await userManager.ResetAuthenticatorKeyAsync(user);
        await userManager.UpdateSecurityStampAsync(user);
        await signInManager.SignOutAsync();

        return Results.Ok(new ApiMessageResponse(
            "تم تعطيل المصادقة الثنائية وتسجيل الخروج من الجلسة الحالية."));
    }

    private static async Task<IResult> RegenerateRecoveryCodesAsync(
        MfaSetupRequest request,
        ClaimsPrincipal principal,
        UserManager<AtharUser> userManager)
    {
        var validation = Validate(request);
        if (validation is not null)
            return Results.ValidationProblem(validation);

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "TwoFactorNotEnabled",
                detail: "فعّل المصادقة الثنائية قبل إنشاء رموز استرداد جديدة.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "ReauthenticationRequired",
                detail: "كلمة المرور الحالية غير صحيحة.");
        }

        var recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                user,
                10))
            ?.ToArray()
            ?? [];

        return Results.Ok(new MfaEnableResponse(recoveryCodes));
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
        UserManager<AtharUser> userManager,
        bool isAuthenticated = true) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            (await userManager.GetRolesAsync(user)).ToArray(),
            isAuthenticated,
            await userManager.IsEmailConfirmedAsync(user),
            await userManager.GetTwoFactorEnabledAsync(user));

    private static ApiMessageResponse GenericAccountNotificationResponse() =>
        new("إذا كان الحساب موجودًا ومؤهلًا للعملية، فسيتم إرسال تعليمات إلى البريد الإلكتروني المسجل.");

    private static Dictionary<string, string[]> GenericInvalidTokenProblem() =>
        new()
        {
            ["Token"] = ["رمز التحقق غير صالح أو انتهت صلاحيته."]
        };

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
