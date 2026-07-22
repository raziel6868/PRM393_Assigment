using MyFSchool.Domain.Identity;

namespace MyFSchool.Application.Identity;

public sealed record CreateIdentityProfileCommand(
    Guid UserId,
    string Code,
    Guid ActorUserId,
    string CorrelationId);

public sealed record IdentityProfileResult(
    Guid Id,
    Guid UserId,
    string Code,
    bool IsActive);

public sealed record CreateParentStudentLinkCommand(
    Guid ParentProfileId,
    Guid StudentProfileId,
    GuardianRelationship Relationship,
    bool IsPrimaryContact,
    Guid ActorUserId,
    string CorrelationId);

public sealed record UpdateParentStudentLinkCommand(
    Guid LinkId,
    GuardianRelationship Relationship,
    bool IsPrimaryContact,
    bool IsActive,
    byte[] RowVersion,
    Guid ActorUserId,
    string CorrelationId);

public sealed record ParentStudentLinkResult(
    Guid Id,
    Guid ParentProfileId,
    Guid StudentProfileId,
    GuardianRelationship Relationship,
    bool IsPrimaryContact,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string RowVersion);

public sealed record LinkedChild(
    Guid StudentProfileId,
    Guid UserId,
    string DisplayName,
    string StudentCode,
    GuardianRelationship Relationship,
    bool IsPrimaryContact);
