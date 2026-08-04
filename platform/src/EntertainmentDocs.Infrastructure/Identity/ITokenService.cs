namespace EntertainmentDocs.Infrastructure.Identity;

public interface ITokenService
{
    string CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles);
}
