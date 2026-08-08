using FoundationKit.Authorization;

namespace Madar.Application.Security;

public interface IMadarCurrentUser :
    FoundationKit.Application.Abstractions.ICurrentUser,
    IAuthorizationSubject
{
}
