namespace MyFSchool.Application.Identity;

public interface IAuthService
{
    Task<OperationResult<AuthSession>> SignInAsync(SignInCommand command, CancellationToken cancellationToken);

    Task<OperationResult<AuthSession>> RefreshAsync(RefreshSessionCommand command, CancellationToken cancellationToken);

    Task LogoutAsync(LogoutCommand command, CancellationToken cancellationToken);

    Task<SessionContext?> GetSessionAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IAccountAdministrationService
{
    Task<OperationResult<ProvisionedUser>> ProvisionAsync(
        ProvisionUserCommand command,
        CancellationToken cancellationToken);
}

public interface IAccessTokenIssuer
{
    AccessToken Issue(AccessTokenDescriptor descriptor);
}

public interface IIdentityBootstrapper
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
