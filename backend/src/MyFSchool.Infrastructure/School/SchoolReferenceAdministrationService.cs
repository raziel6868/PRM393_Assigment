using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.Identity;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.School;

public sealed class SchoolReferenceAdministrationService(
    MyFSchoolDbContext dbContext,
    TimeProvider timeProvider) : ISchoolReferenceAdministrationService
{
    public async Task<OperationResult<SchoolYearResult>> CreateSchoolYearAsync(
        CreateSchoolYearCommand command,
        CancellationToken cancellationToken)
    {
        if (command.EndDate <= command.StartDate)
        {
            return OperationResult<SchoolYearResult>.Failure("invalidDateRange");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var schoolYear = new SchoolYear
        {
            Id = Guid.NewGuid(),
            Code = command.Code.Trim(),
            DisplayName = command.DisplayName.Trim(),
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
        dbContext.SchoolYears.Add(schoolYear);
        Audit("schoolYearCreated", command.ActorUserId, command.CorrelationId, nowUtc);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            return OperationResult<SchoolYearResult>.Failure("schoolYearAlreadyExists");
        }

        return OperationResult<SchoolYearResult>.Success(ToResult(schoolYear));
    }

    public async Task<OperationResult<ClassResult>> CreateClassAsync(
        CreateClassCommand command,
        CancellationToken cancellationToken)
    {
        var schoolYear = await dbContext.SchoolYears
            .Where(year => year.Id == command.SchoolYearId && year.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        if (schoolYear is null)
        {
            return OperationResult<ClassResult>.Failure("schoolYearNotFound");
        }
        if (command.HomeroomTeacherProfileId is { } homeroomId &&
            !await dbContext.TeacherProfiles.AnyAsync(profile => profile.Id == homeroomId && profile.IsActive, cancellationToken))
        {
            return OperationResult<ClassResult>.Failure("teacherProfileNotFound");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var classroom = new ClassRoom
        {
            Id = Guid.NewGuid(),
            Code = command.Code.Trim(),
            DisplayName = command.DisplayName.Trim(),
            GradeLevel = command.GradeLevel,
            SchoolYearId = command.SchoolYearId,
            HomeroomTeacherProfileId = command.HomeroomTeacherProfileId,
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
        dbContext.ClassRooms.Add(classroom);
        Audit("classCreated", command.ActorUserId, command.CorrelationId, nowUtc);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            return OperationResult<ClassResult>.Failure("classAlreadyExists");
        }

        return OperationResult<ClassResult>.Success(ToResult(classroom, schoolYear.Code));
    }

    public async Task<OperationResult<ClassResult>> UpdateClassAsync(
        UpdateClassCommand command,
        CancellationToken cancellationToken)
    {
        var classroom = await dbContext.ClassRooms
            .SingleOrDefaultAsync(item => item.Id == command.ClassId, cancellationToken);
        if (classroom is null)
        {
            return OperationResult<ClassResult>.Failure("classNotFound");
        }
        if (!RowVersionMatches(command.RowVersion, classroom.RowVersion))
        {
            return OperationResult<ClassResult>.Failure("concurrencyConflict");
        }
        if (command.HomeroomTeacherProfileId is { } homeroomId &&
            !await dbContext.TeacherProfiles.AnyAsync(profile => profile.Id == homeroomId && profile.IsActive, cancellationToken))
        {
            return OperationResult<ClassResult>.Failure("teacherProfileNotFound");
        }

        classroom.DisplayName = command.DisplayName.Trim();
        classroom.GradeLevel = command.GradeLevel;
        classroom.HomeroomTeacherProfileId = command.HomeroomTeacherProfileId;
        classroom.IsActive = command.IsActive;

        var nowUtc = timeProvider.GetUtcNow();
        Audit("classUpdated", command.ActorUserId, command.CorrelationId, nowUtc);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ClassResult>.Failure("concurrencyConflict");
        }

        var schoolYearCode = await dbContext.SchoolYears
            .Where(year => year.Id == classroom.SchoolYearId)
            .Select(year => year.Code)
            .SingleAsync(cancellationToken);
        return OperationResult<ClassResult>.Success(ToResult(classroom, schoolYearCode));
    }

    public async Task<OperationResult<SubjectResult>> CreateSubjectAsync(
        CreateSubjectCommand command,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Code = command.Code.Trim(),
            DisplayName = command.DisplayName.Trim(),
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
        dbContext.Subjects.Add(subject);
        Audit("subjectCreated", command.ActorUserId, command.CorrelationId, nowUtc);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            return OperationResult<SubjectResult>.Failure("subjectAlreadyExists");
        }

        return OperationResult<SubjectResult>.Success(new SubjectResult(
            subject.Id, subject.Code, subject.DisplayName, subject.IsActive,
            Convert.ToBase64String(subject.RowVersion)));
    }

    public async Task<OperationResult<TeacherAssignmentResult>> CreateTeacherAssignmentAsync(
        CreateTeacherAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.TeacherProfiles.AnyAsync(profile => profile.Id == command.TeacherProfileId && profile.IsActive, cancellationToken))
        {
            return OperationResult<TeacherAssignmentResult>.Failure("teacherProfileNotFound");
        }
        if (!await dbContext.ClassRooms.AnyAsync(item => item.Id == command.ClassId && item.IsActive, cancellationToken))
        {
            return OperationResult<TeacherAssignmentResult>.Failure("classNotFound");
        }
        if (!await dbContext.Subjects.AnyAsync(item => item.Id == command.SubjectId && item.IsActive, cancellationToken))
        {
            return OperationResult<TeacherAssignmentResult>.Failure("subjectNotFound");
        }
        if (!await dbContext.SchoolYears.AnyAsync(year => year.Id == command.SchoolYearId && year.IsActive, cancellationToken))
        {
            return OperationResult<TeacherAssignmentResult>.Failure("schoolYearNotFound");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var assignment = new TeacherClassSubjectAssignment
        {
            Id = Guid.NewGuid(),
            TeacherProfileId = command.TeacherProfileId,
            ClassId = command.ClassId,
            SubjectId = command.SubjectId,
            SchoolYearId = command.SchoolYearId,
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
        dbContext.TeacherClassSubjectAssignments.Add(assignment);
        Audit("teacherAssignmentCreated", command.ActorUserId, command.CorrelationId, nowUtc);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            return OperationResult<TeacherAssignmentResult>.Failure("teacherAssignmentAlreadyExists");
        }

        return OperationResult<TeacherAssignmentResult>.Success(new TeacherAssignmentResult(
            assignment.Id, assignment.TeacherProfileId, assignment.ClassId, assignment.SubjectId,
            assignment.SchoolYearId, assignment.IsActive,
            Convert.ToBase64String(assignment.RowVersion)));
    }

    public async Task<OperationResult<StudentEnrollmentResult>> CreateStudentEnrollmentAsync(
        CreateStudentEnrollmentCommand command,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.StudentProfiles.AnyAsync(profile => profile.Id == command.StudentProfileId && profile.IsActive, cancellationToken))
        {
            return OperationResult<StudentEnrollmentResult>.Failure("studentProfileNotFound");
        }
        if (!await dbContext.ClassRooms.AnyAsync(item => item.Id == command.ClassId && item.IsActive, cancellationToken))
        {
            return OperationResult<StudentEnrollmentResult>.Failure("classNotFound");
        }
        if (!await dbContext.SchoolYears.AnyAsync(year => year.Id == command.SchoolYearId && year.IsActive, cancellationToken))
        {
            return OperationResult<StudentEnrollmentResult>.Failure("schoolYearNotFound");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var enrollment = new StudentEnrollment
        {
            Id = Guid.NewGuid(),
            StudentProfileId = command.StudentProfileId,
            ClassId = command.ClassId,
            SchoolYearId = command.SchoolYearId,
            EnrolledOn = command.EnrolledOn,
            CreatedAtUtc = nowUtc
        };
        dbContext.StudentEnrollments.Add(enrollment);
        Audit("studentEnrolled", command.ActorUserId, command.CorrelationId, nowUtc);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            return OperationResult<StudentEnrollmentResult>.Failure("studentEnrollmentAlreadyExists");
        }

        return OperationResult<StudentEnrollmentResult>.Success(new StudentEnrollmentResult(
            enrollment.Id, enrollment.StudentProfileId, enrollment.ClassId, enrollment.SchoolYearId,
            enrollment.EnrolledOn, enrollment.LeftOn, Convert.ToBase64String(enrollment.RowVersion)));
    }

    private void Audit(string eventType, Guid actorUserId, string correlationId, DateTimeOffset occurredAtUtc)
    {
        dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            ActorUserId = actorUserId,
            SubjectUserId = actorUserId,
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAtUtc
        });
    }

    private static SchoolYearResult ToResult(SchoolYear year) => new(
        year.Id, year.Code, year.DisplayName, year.StartDate, year.EndDate, year.IsActive,
        Convert.ToBase64String(year.RowVersion));

    private static ClassResult ToResult(ClassRoom classroom, string schoolYearCode) => new(
        classroom.Id, classroom.Code, classroom.DisplayName, classroom.GradeLevel,
        classroom.SchoolYearId, schoolYearCode, classroom.HomeroomTeacherProfileId,
        classroom.IsActive, Convert.ToBase64String(classroom.RowVersion));

    private static bool RowVersionMatches(byte[] supplied, byte[] persisted) =>
        supplied.Length == persisted.Length && CryptographicOperations.FixedTimeEquals(supplied, persisted);

    private static bool IsUniqueConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
