using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Domain.Identity;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.Identity;

public sealed class RelationshipAdministrationService(
    UserManager<AppUser> userManager,
    MyFSchoolDbContext dbContext,
    TimeProvider timeProvider) : IIdentityRelationshipAdministrationService
{
    public Task<OperationResult<IdentityProfileResult>> CreateTeacherProfileAsync(
        CreateIdentityProfileCommand command,
        CancellationToken cancellationToken) =>
        CreateProfileAsync<TeacherProfile>(command, SchoolRoles.Teacher, cancellationToken);

    public Task<OperationResult<IdentityProfileResult>> CreateStudentProfileAsync(
        CreateIdentityProfileCommand command,
        CancellationToken cancellationToken) =>
        CreateProfileAsync<StudentProfile>(command, SchoolRoles.Student, cancellationToken);

    public Task<OperationResult<IdentityProfileResult>> CreateParentProfileAsync(
        CreateIdentityProfileCommand command,
        CancellationToken cancellationToken) =>
        CreateProfileAsync<ParentProfile>(command, SchoolRoles.Parent, cancellationToken);

    public async Task<OperationResult<ParentStudentLinkResult>> CreateParentStudentLinkAsync(
        CreateParentStudentLinkCommand command,
        CancellationToken cancellationToken)
    {
        var profilesAreActive = await dbContext.ParentProfiles
            .Where(profile => profile.Id == command.ParentProfileId && profile.IsActive)
            .Join(dbContext.Users.Where(user => user.IsActive), profile => profile.UserId, user => user.Id, (_, _) => true)
            .AnyAsync(cancellationToken) &&
            await dbContext.StudentProfiles
                .Where(profile => profile.Id == command.StudentProfileId && profile.IsActive)
                .Join(dbContext.Users.Where(user => user.IsActive), profile => profile.UserId, user => user.Id, (_, _) => true)
                .AnyAsync(cancellationToken);
        if (!profilesAreActive)
        {
            return OperationResult<ParentStudentLinkResult>.Failure("profileNotFound");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var link = new ParentStudentLink
        {
            Id = Guid.NewGuid(),
            ParentProfileId = command.ParentProfileId,
            StudentProfileId = command.StudentProfileId,
            Relationship = command.Relationship,
            IsPrimaryContact = command.IsPrimaryContact,
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
        dbContext.ParentStudentLinks.Add(link);
        dbContext.SecurityAuditEvents.Add(CreateAudit(
            "parentStudentLinkCreated",
            await GetStudentUserIdAsync(command.StudentProfileId, cancellationToken),
            command.ActorUserId,
            command.CorrelationId,
            nowUtc));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            return OperationResult<ParentStudentLinkResult>.Failure("relationshipAlreadyExists");
        }

        return OperationResult<ParentStudentLinkResult>.Success(ToResult(link));
    }

    public async Task<OperationResult<ParentStudentLinkResult>> UpdateParentStudentLinkAsync(
        UpdateParentStudentLinkCommand command,
        CancellationToken cancellationToken)
    {
        var link = await dbContext.ParentStudentLinks
            .SingleOrDefaultAsync(item => item.Id == command.LinkId, cancellationToken);
        if (link is null)
        {
            return OperationResult<ParentStudentLinkResult>.Failure("relationshipNotFound");
        }
        if (!RowVersionMatches(command.RowVersion, link.RowVersion))
        {
            return OperationResult<ParentStudentLinkResult>.Failure("concurrencyConflict");
        }

        link.Relationship = command.Relationship;
        link.IsPrimaryContact = command.IsPrimaryContact;
        link.IsActive = command.IsActive;
        var nowUtc = timeProvider.GetUtcNow();
        dbContext.SecurityAuditEvents.Add(CreateAudit(
            "parentStudentLinkUpdated",
            await GetStudentUserIdAsync(link.StudentProfileId, cancellationToken),
            command.ActorUserId,
            command.CorrelationId,
            nowUtc));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ParentStudentLinkResult>.Failure("concurrencyConflict");
        }

        return OperationResult<ParentStudentLinkResult>.Success(ToResult(link));
    }

    private async Task<OperationResult<IdentityProfileResult>> CreateProfileAsync<TProfile>(
        CreateIdentityProfileCommand command,
        string requiredRole,
        CancellationToken cancellationToken)
        where TProfile : class
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return OperationResult<IdentityProfileResult>.Failure("accountNotFound");
        }
        if (!await userManager.IsInRoleAsync(user, requiredRole))
        {
            return OperationResult<IdentityProfileResult>.Failure("profileRoleMismatch");
        }

        var id = Guid.NewGuid();
        var code = command.Code.Trim();
        object profile = requiredRole switch
        {
            SchoolRoles.Teacher => new TeacherProfile { Id = id, UserId = user.Id, EmployeeCode = code },
            SchoolRoles.Student => new StudentProfile { Id = id, UserId = user.Id, StudentCode = code },
            SchoolRoles.Parent => new ParentProfile { Id = id, UserId = user.Id, ParentCode = code },
            _ => throw new ArgumentOutOfRangeException(nameof(requiredRole))
        };
        dbContext.Add(profile);
        var nowUtc = timeProvider.GetUtcNow();
        dbContext.SecurityAuditEvents.Add(CreateAudit(
            "identityProfileCreated",
            user.Id,
            command.ActorUserId,
            command.CorrelationId,
            nowUtc));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            return OperationResult<IdentityProfileResult>.Failure("profileAlreadyExists");
        }

        return OperationResult<IdentityProfileResult>.Success(new IdentityProfileResult(id, user.Id, code, true));
    }

    private Task<Guid> GetStudentUserIdAsync(Guid studentProfileId, CancellationToken cancellationToken) =>
        dbContext.StudentProfiles
            .Where(profile => profile.Id == studentProfileId)
            .Select(profile => profile.UserId)
            .SingleAsync(cancellationToken);

    private static SecurityAuditEvent CreateAudit(
        string eventType,
        Guid subjectUserId,
        Guid actorUserId,
        string correlationId,
        DateTimeOffset occurredAtUtc) => new()
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            SubjectUserId = subjectUserId,
            ActorUserId = actorUserId,
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAtUtc
        };

    private static ParentStudentLinkResult ToResult(ParentStudentLink link) => new(
        link.Id,
        link.ParentProfileId,
        link.StudentProfileId,
        link.Relationship,
        link.IsPrimaryContact,
        link.IsActive,
        link.CreatedAtUtc,
        Convert.ToBase64String(link.RowVersion));

    private static bool RowVersionMatches(byte[] supplied, byte[] persisted) =>
        supplied.Length == persisted.Length && CryptographicOperations.FixedTimeEquals(supplied, persisted);

    private static bool IsUniqueConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
