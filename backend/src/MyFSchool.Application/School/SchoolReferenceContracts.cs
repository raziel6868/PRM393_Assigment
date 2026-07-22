using MyFSchool.Domain.School;
using MyFSchool.Application.Identity;

namespace MyFSchool.Application.School;

public sealed record CreateSchoolYearCommand(
    string Code,
    string DisplayName,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid ActorUserId,
    string CorrelationId);

public sealed record SchoolYearResult(
    Guid Id,
    string Code,
    string DisplayName,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    string RowVersion);

public sealed record CreateClassCommand(
    string Code,
    string DisplayName,
    int GradeLevel,
    Guid SchoolYearId,
    Guid? HomeroomTeacherProfileId,
    Guid ActorUserId,
    string CorrelationId);

public sealed record UpdateClassCommand(
    Guid ClassId,
    string DisplayName,
    int GradeLevel,
    Guid? HomeroomTeacherProfileId,
    bool IsActive,
    byte[] RowVersion,
    Guid ActorUserId,
    string CorrelationId);

public sealed record ClassResult(
    Guid Id,
    string Code,
    string DisplayName,
    int GradeLevel,
    Guid SchoolYearId,
    string SchoolYearCode,
    Guid? HomeroomTeacherProfileId,
    bool IsActive,
    string RowVersion);

public sealed record CreateSubjectCommand(
    string Code,
    string DisplayName,
    Guid ActorUserId,
    string CorrelationId);

public sealed record SubjectResult(
    Guid Id,
    string Code,
    string DisplayName,
    bool IsActive,
    string RowVersion);

public sealed record CreateTeacherAssignmentCommand(
    Guid TeacherProfileId,
    Guid ClassId,
    Guid SubjectId,
    Guid SchoolYearId,
    Guid ActorUserId,
    string CorrelationId);

public sealed record TeacherAssignmentResult(
    Guid Id,
    Guid TeacherProfileId,
    Guid ClassId,
    Guid SubjectId,
    Guid SchoolYearId,
    bool IsActive,
    string RowVersion);

public sealed record CreateStudentEnrollmentCommand(
    Guid StudentProfileId,
    Guid ClassId,
    Guid SchoolYearId,
    DateOnly EnrolledOn,
    Guid ActorUserId,
    string CorrelationId);

public sealed record StudentEnrollmentResult(
    Guid Id,
    Guid StudentProfileId,
    Guid ClassId,
    Guid SchoolYearId,
    DateOnly EnrolledOn,
    DateOnly? LeftOn,
    string RowVersion);

public sealed record StudentClassScope(
    Guid ClassId,
    string ClassCode,
    string ClassDisplayName,
    Guid SchoolYearId,
    string SchoolYearCode);

public sealed record TeacherClassScope(
    Guid ClassId,
    string ClassCode,
    string ClassDisplayName,
    Guid SubjectId,
    string SubjectCode,
    string SubjectDisplayName,
    Guid SchoolYearId,
    string SchoolYearCode,
    bool IsHomeroom);

public sealed record ParentChildClassScope(
    Guid StudentProfileId,
    Guid StudentUserId,
    string StudentDisplayName,
    string StudentCode,
    Guid ClassId,
    string ClassCode,
    string ClassDisplayName,
    Guid SchoolYearId,
    string SchoolYearCode);

public interface ISchoolReferenceAdministrationService
{
    Task<OperationResult<SchoolYearResult>> CreateSchoolYearAsync(
        CreateSchoolYearCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<ClassResult>> CreateClassAsync(
        CreateClassCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<ClassResult>> UpdateClassAsync(
        UpdateClassCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<SubjectResult>> CreateSubjectAsync(
        CreateSubjectCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<TeacherAssignmentResult>> CreateTeacherAssignmentAsync(
        CreateTeacherAssignmentCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<StudentEnrollmentResult>> CreateStudentEnrollmentAsync(
        CreateStudentEnrollmentCommand command,
        CancellationToken cancellationToken);
}

public interface ISchoolScopeQueryService
{
    Task<IReadOnlyList<StudentClassScope>> GetStudentClassesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherClassScope>> GetTeacherClassesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ParentChildClassScope>> GetParentChildrenClassesAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
