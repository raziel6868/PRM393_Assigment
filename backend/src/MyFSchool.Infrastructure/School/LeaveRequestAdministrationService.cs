using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.Identity;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.School;

public sealed class LeaveRequestAdministrationService(
    MyFSchoolDbContext dbContext,
    TimeProvider timeProvider) : ILeaveRequestAdministrationService
{
    private const int MaxPageSize = 100;

    public async Task<OperationResult<LeaveRequestResult>> SubmitAsync(
        SubmitLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (command.EndDate < command.StartDate)
            return OperationResult<LeaveRequestResult>.Failure("invalidDateRange");
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Length is < 20 or > 500)
            return OperationResult<LeaveRequestResult>.Failure("reasonLengthInvalid");
        if (!await dbContext.StudentProfiles.AnyAsync(profile => profile.Id == command.StudentProfileId && profile.IsActive, cancellationToken))
            return OperationResult<LeaveRequestResult>.Failure("studentProfileNotFound");

        // Authorization: the requester must be an active Parent linked to this Student.
        var linkedParent = await dbContext.ParentProfiles
            .Where(parent => parent.UserId == command.ActorUserId && parent.IsActive)
            .Join(dbContext.ParentStudentLinks,
                parent => parent.Id,
                link => link.ParentProfileId,
                (parent, link) => new { parent, link })
            .Where(joined => joined.link.StudentProfileId == command.StudentProfileId
                && joined.link.IsActive)
            .Select(joined => joined.parent.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (linkedParent == Guid.Empty)
            return OperationResult<LeaveRequestResult>.Failure("studentNotLinked");

        // Dedup: at most one Pending request per (StudentProfileId, StartDate, EndDate)
        var duplicateExists = await dbContext.LeaveRequests.AnyAsync(
            request => request.StudentProfileId == command.StudentProfileId
                && request.StartDate == command.StartDate
                && request.EndDate == command.EndDate
                && request.Status == LeaveStatus.Pending,
            cancellationToken);
        if (duplicateExists)
            return OperationResult<LeaveRequestResult>.Failure("leaveRequestAlreadyPending");

        var nowUtc = timeProvider.GetUtcNow();
        var entity = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            StudentProfileId = command.StudentProfileId,
            RequesterUserId = command.ActorUserId,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            StartSession = command.StartSession,
            EndSession = command.EndSession,
            ReasonCategory = command.ReasonCategory,
            Reason = command.Reason.Trim(),
            Status = LeaveStatus.Pending,
            CreatedAtUtc = nowUtc
        };
        dbContext.LeaveRequests.Add(entity);
        Audit("leaveRequestSubmitted", command.ActorUserId, command.CorrelationId, nowUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResult<LeaveRequestResult>.Success(ToResult(entity));
    }

    public async Task<OperationResult<LeaveRequestResult>> CancelAsync(
        CancelLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.LeaveRequests
            .SingleOrDefaultAsync(request => request.Id == command.LeaveRequestId, cancellationToken);
        if (entity is null) return OperationResult<LeaveRequestResult>.Failure("leaveRequestNotFound");
        if (entity.RequesterUserId != command.ActorUserId)
            return OperationResult<LeaveRequestResult>.Failure("leaveRequestNotOwned");
        if (!RowVersionMatches(command.RowVersion, entity.RowVersion))
            return OperationResult<LeaveRequestResult>.Failure("concurrencyConflict");
        if (entity.Status != LeaveStatus.Pending)
            return OperationResult<LeaveRequestResult>.Failure("leaveRequestNotPending");

        var nowUtc = timeProvider.GetUtcNow();
        entity.Status = LeaveStatus.Cancelled;
        entity.DecidedAtUtc = nowUtc;
        entity.DecisionNote = "Phụ huynh huỷ đơn";
        Audit("leaveRequestCancelled", command.ActorUserId, command.CorrelationId, nowUtc);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<LeaveRequestResult>.Failure("concurrencyConflict");
        }
        return OperationResult<LeaveRequestResult>.Success(ToResult(entity));
    }

    public async Task<OperationResult<LeaveRequestPage>> ListMineAsync(
        Guid actorUserId,
        Guid? studentProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var boundedPage = page < 1 ? 1 : page;
        var boundedPageSize = pageSize switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };

        var studentIds = await ResolveStudentProfileIdsAsync(actorUserId, studentProfileId, cancellationToken);
        if (studentIds is null) return OperationResult<LeaveRequestPage>.Failure("studentScopeForbidden");

        var query = dbContext.LeaveRequests.AsNoTracking()
            .Where(request => studentIds.Contains(request.StudentProfileId))
            .OrderByDescending(request => request.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)boundedPageSize);
        var items = await query
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize)
            .ToListAsync(cancellationToken);

        return OperationResult<LeaveRequestPage>.Success(new LeaveRequestPage(
            items.Select(ToResult).ToList(),
            boundedPage, boundedPageSize, totalCount, totalPages));
    }

    public async Task<OperationResult<LeaveRequestResult>> GetForParentAsync(
        Guid actorUserId,
        Guid leaveRequestId,
        CancellationToken cancellationToken)
    {
        var studentIds = await ResolveStudentProfileIdsAsync(actorUserId, null, cancellationToken);
        if (studentIds is null) return OperationResult<LeaveRequestResult>.Failure("leaveRequestNotFound");

        var entity = await dbContext.LeaveRequests.AsNoTracking()
            .SingleOrDefaultAsync(request => request.Id == leaveRequestId
                && studentIds.Contains(request.StudentProfileId), cancellationToken);
        return entity is null
            ? OperationResult<LeaveRequestResult>.Failure("leaveRequestNotFound")
            : OperationResult<LeaveRequestResult>.Success(ToResult(entity));
    }

    public async Task<OperationResult<LeaveRequestPage>> ListTeacherQueueAsync(
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var assignedStudentIds = await (
            from teacher in dbContext.TeacherProfiles.AsNoTracking()
            join assignment in dbContext.TeacherClassSubjectAssignments.AsNoTracking() on teacher.Id equals assignment.TeacherProfileId
            join enrollment in dbContext.StudentEnrollments.AsNoTracking() on assignment.ClassId equals enrollment.ClassId
            where teacher.UserId == actorUserId && teacher.IsActive &&
                  assignment.IsActive && enrollment.ClassId == assignment.ClassId
            select enrollment.StudentProfileId).Distinct().ToListAsync(cancellationToken);

        if (assignedStudentIds.Count == 0)
            return OperationResult<LeaveRequestPage>.Success(new LeaveRequestPage(Array.Empty<LeaveRequestResult>(), page, pageSize, 0, 0));

        var boundedPage = page < 1 ? 1 : page;
        var boundedPageSize = pageSize switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };

        var query = dbContext.LeaveRequests.AsNoTracking()
            .Where(request => assignedStudentIds.Contains(request.StudentProfileId)
                && request.Status == LeaveStatus.Pending)
            .OrderBy(request => request.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)boundedPageSize);
        var items = await query
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize)
            .ToListAsync(cancellationToken);

        return OperationResult<LeaveRequestPage>.Success(new LeaveRequestPage(
            items.Select(ToResult).ToList(),
            boundedPage, boundedPageSize, totalCount, totalPages));
    }

    public async Task<OperationResult<LeaveRequestResult>> DecideAsync(
        DecideLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.LeaveRequests
            .SingleOrDefaultAsync(request => request.Id == command.LeaveRequestId, cancellationToken);
        if (entity is null) return OperationResult<LeaveRequestResult>.Failure("leaveRequestNotFound");
        if (!await TeacherCanDecideAsync(command.ActorUserId, entity.StudentProfileId, cancellationToken))
            return OperationResult<LeaveRequestResult>.Failure("leaveRequestAccessDenied");
        if (!RowVersionMatches(command.RowVersion, entity.RowVersion))
            return OperationResult<LeaveRequestResult>.Failure("concurrencyConflict");
        if (entity.Status != LeaveStatus.Pending)
            return OperationResult<LeaveRequestResult>.Failure("leaveRequestNotPending");
        if (!command.Approve && string.IsNullOrWhiteSpace(command.DecisionNote))
            return OperationResult<LeaveRequestResult>.Failure("rejectionReasonRequired");

        var nowUtc = timeProvider.GetUtcNow();
        entity.Status = command.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        entity.DecisionNote = command.DecisionNote?.Trim();
        entity.ReviewerUserId = command.ActorUserId;
        entity.ReviewedAtUtc = nowUtc;
        entity.DecidedAtUtc = nowUtc;

        Audit(command.Approve ? "leaveRequestApproved" : "leaveRequestRejected",
            command.ActorUserId, command.CorrelationId, nowUtc);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<LeaveRequestResult>.Failure("concurrencyConflict");
        }
        return OperationResult<LeaveRequestResult>.Success(ToResult(entity));
    }

    public async Task<OperationResult<LeaveRequestResult>> GetForTeacherAsync(
        Guid actorUserId,
        Guid leaveRequestId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.LeaveRequests.AsNoTracking()
            .SingleOrDefaultAsync(request => request.Id == leaveRequestId, cancellationToken);
        if (entity is null) return OperationResult<LeaveRequestResult>.Failure("leaveRequestNotFound");
        if (!await TeacherCanDecideAsync(actorUserId, entity.StudentProfileId, cancellationToken))
            return OperationResult<LeaveRequestResult>.Failure("leaveRequestAccessDenied");
        return OperationResult<LeaveRequestResult>.Success(ToResult(entity));
    }

    private async Task<List<Guid>?> ResolveStudentProfileIdsAsync(
        Guid actorUserId,
        Guid? requestedStudentId,
        CancellationToken cancellationToken)
    {
        // Parent: must have an active link to the requested child, or any child when null.
        var linkedIds = await (
            from parent in dbContext.ParentProfiles.AsNoTracking()
            join link in dbContext.ParentStudentLinks.AsNoTracking() on parent.Id equals link.ParentProfileId
            where parent.UserId == actorUserId && parent.IsActive && link.IsActive
            select link.StudentProfileId).ToListAsync(cancellationToken);

        if (linkedIds.Count > 0)
        {
            return requestedStudentId is null
                ? linkedIds
                : linkedIds.Contains(requestedStudentId.Value) ? new List<Guid> { requestedStudentId.Value } : null;
        }

        // Student: own profile.
        var ownId = await dbContext.StudentProfiles.AsNoTracking()
            .Where(profile => profile.UserId == actorUserId && profile.IsActive)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (ownId is null) return null;
        return requestedStudentId is null || requestedStudentId == ownId ? new List<Guid> { ownId.Value } : null;
    }

    private async Task<bool> TeacherCanDecideAsync(Guid teacherUserId, Guid studentProfileId, CancellationToken cancellationToken) =>
        await (
            from teacher in dbContext.TeacherProfiles.AsNoTracking()
            join assignment in dbContext.TeacherClassSubjectAssignments.AsNoTracking() on teacher.Id equals assignment.TeacherProfileId
            join enrollment in dbContext.StudentEnrollments.AsNoTracking() on assignment.ClassId equals enrollment.ClassId
            where teacher.UserId == teacherUserId && teacher.IsActive &&
                  assignment.IsActive && enrollment.StudentProfileId == studentProfileId
            select 1).AnyAsync(cancellationToken);

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

    private static LeaveRequestResult ToResult(LeaveRequest entity) => new(
        entity.Id,
        entity.StudentProfileId,
        entity.RequesterUserId,
        entity.StartDate,
        entity.EndDate,
        entity.StartSession.ToWire(),
        entity.EndSession.ToWire(),
        entity.ReasonCategory.ToWire(),
        entity.Reason,
        entity.DecisionNote,
        entity.ReviewerUserId,
        entity.ReviewedAtUtc,
        entity.Status.ToWire(),
        entity.CreatedAtUtc,
        entity.DecidedAtUtc,
        Convert.ToBase64String(entity.RowVersion));

    private static bool RowVersionMatches(byte[] supplied, byte[] persisted) =>
        supplied.Length == persisted.Length && CryptographicOperations.FixedTimeEquals(supplied, persisted);

    private static bool IsUniqueConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}