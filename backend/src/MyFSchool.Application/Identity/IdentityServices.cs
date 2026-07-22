namespace MyFSchool.Application.Identity;

public interface IAuthService
{
    Task<OperationResult<AuthSession>> SignInAsync(SignInCommand command, CancellationToken cancellationToken);

    Task<OperationResult<AuthSession>> RefreshAsync(RefreshSessionCommand command, CancellationToken cancellationToken);

    Task LogoutAsync(LogoutCommand command, CancellationToken cancellationToken);

    Task<OperationResult<bool>> ChangeTemporaryPasswordAsync(
        ChangeTemporaryPasswordCommand command,
        CancellationToken cancellationToken);

    Task<SessionContext?> GetSessionAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IAccountAdministrationService
{
    Task<OperationResult<ProvisionedUser>> ProvisionAsync(
        ProvisionUserCommand command,
        CancellationToken cancellationToken);

    Task<PasswordHelpPage> GetPasswordHelpRequestsAsync(
        PasswordHelpQuery query,
        CancellationToken cancellationToken);

    Task<OperationResult<IssuedTemporaryPassword>> IssueTemporaryPasswordAsync(
        IssueTemporaryPasswordCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<bool>> RejectPasswordHelpRequestAsync(
        RejectPasswordHelpCommand command,
        CancellationToken cancellationToken);
}

public interface IPasswordHelpService
{
    Task SubmitAsync(string emailOrUserName, string correlationId, CancellationToken cancellationToken);
}

public interface IAccessTokenIssuer
{
    AccessToken Issue(AccessTokenDescriptor descriptor);
}

public interface IIdentityBootstrapper
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
