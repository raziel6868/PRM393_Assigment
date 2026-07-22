using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.School;

public sealed class GradeAdministrationService(
    MyFSchoolDbContext dbContext,
    TimeProvider timeProvider) : IGradeAdministrationService
{
    public async Task<OperationResult<GradePage>> GetStudentGradesAsync(
        Guid userId,
        Guid? schoolYearId,
        int? semester,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var studentProfileId = await dbContext.StudentProfiles
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!studentProfileId.HasValue)
            return OperationResult<GradePage>.Failure("studentProfileNotFound");

        return await GetGradesCoreAsync(studentProfileId.Value, schoolYearId, semester, page, pageSize, cancellationToken);
    }

    public async Task<OperationResult<GradePage>> GetParentChildGradesAsync(
        Guid parentUserId,
        Guid studentProfileId,
        Guid? schoolYearId,
        int? semester,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var linked = await (
            from parent in dbContext.ParentProfiles.AsNoTracking()
            join link in dbContext.ParentStudentLinks.AsNoTracking() on parent.Id equals link.ParentProfileId
            where parent.UserId == parentUserId && parent.IsActive && link.IsActive
            select link.StudentProfileId
        ).ToListAsync(cancellationToken);

        if (!linked.Contains(studentProfileId))
            return OperationResult<GradePage>.Failure("studentNotLinked");

        return await GetGradesCoreAsync(studentProfileId, schoolYearId, semester, page, pageSize, cancellationToken);
    }

    private async Task<OperationResult<GradePage>> GetGradesCoreAsync(
        Guid studentProfileId,
        Guid? schoolYearId,
        int? semester,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var boundedPage = page < 1 ? 1 : page;
        var boundedPageSize = pageSize switch { < 1 => 20, > 100 => 100, _ => pageSize };

        var gradesQuery = from entry in dbContext.GradeEntries.AsNoTracking()
                          join assessment in dbContext.Assessments.AsNoTracking() on entry.AssessmentId equals assessment.Id
                          join subject in dbContext.Subjects.AsNoTracking() on assessment.SubjectId equals subject.Id
                          join year in dbContext.SchoolYears.AsNoTracking() on assessment.SchoolYearId equals year.Id
                          join classroom in dbContext.ClassRooms.AsNoTracking() on assessment.ClassId equals classroom.Id
                          where entry.StudentProfileId == studentProfileId
                          where !schoolYearId.HasValue || year.Id == schoolYearId.Value
                          where !semester.HasValue || assessment.Semester == semester.Value
                          orderby year.Code descending, assessment.Semester, subject.DisplayName, assessment.DisplayName
                          select new
                          {
                              entry.Id,
                              assessment.Code,
                              assessment.DisplayName,
                              assessment.AssessmentType,
                              assessment.Semester,
                              YearId = year.Id,
                              YearCode = year.Code,
                              ClassId = classroom.Id,
                              ClassCode = classroom.Code,
                              SubjectId = subject.Id,
                              SubjectName = subject.DisplayName,
                              entry.Score,
                              assessment.MaxScore,
                              entry.TeacherComment,
                              entry.RecordedAtUtc
                          };

        var records = await gradesQuery.ToListAsync(cancellationToken);

        var grouped = records
            .GroupBy(r => new { r.YearId, r.YearCode, r.Semester, r.SubjectId, r.SubjectName })
            .Select(g => new StudentGradeSummary(
                g.Key.YearId,
                g.Key.YearCode,
                g.Key.Semester,
                g.Key.SubjectId,
                g.Key.SubjectName,
                g.Where(x => x.Score.HasValue).Select(x => x.Score!.Value).DefaultIfEmpty(0).Average(),
                g.Count(),
                g.Select(r => new GradeResult(
                    r.Id, r.Id, r.Code, r.DisplayName, r.AssessmentType, r.Semester,
                    r.YearId, r.YearCode, r.ClassId, r.ClassCode, r.SubjectId, r.SubjectName,
                    r.Score, r.MaxScore, r.TeacherComment, r.RecordedAtUtc)).ToList()))
            .ToList();

        var totalCount = grouped.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)boundedPageSize);
        var items = grouped.Skip((boundedPage - 1) * boundedPageSize).Take(boundedPageSize).ToList();

        return OperationResult<GradePage>.Success(new GradePage(items, boundedPage, boundedPageSize, totalCount, totalPages));
    }

    public async Task<OperationResult<IReadOnlyList<AssessmentRoster>>> GetAssessmentRostersAsync(
        Guid teacherUserId,
        Guid classId,
        Guid? subjectId,
        Guid schoolYearId,
        int semester,
        CancellationToken cancellationToken)
    {
        var teacherProfileId = await dbContext.TeacherProfiles
            .Where(p => p.UserId == teacherUserId && p.IsActive)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!teacherProfileId.HasValue)
            return OperationResult<IReadOnlyList<AssessmentRoster>>.Failure("teacherProfileNotFound");

        var isAssigned = await dbContext.TeacherClassSubjectAssignments.AsNoTracking()
            .AnyAsync(a => a.TeacherProfileId == teacherProfileId.Value && a.ClassId == classId
                && a.SchoolYearId == schoolYearId && a.IsActive, cancellationToken);
        if (!isAssigned)
            return OperationResult<IReadOnlyList<AssessmentRoster>>.Failure("classNotAssigned");

        var assessmentsQuery = dbContext.Assessments.AsNoTracking()
            .Where(a => a.ClassId == classId && a.SchoolYearId == schoolYearId && a.Semester == semester
                && (!subjectId.HasValue || a.SubjectId == subjectId.Value))
            .OrderBy(a => a.DisplayName);

        var assessments = await assessmentsQuery.ToListAsync(cancellationToken);
        if (assessments.Count == 0)
            return OperationResult<IReadOnlyList<AssessmentRoster>>.Success(Array.Empty<AssessmentRoster>());

        var studentIds = await dbContext.StudentEnrollments.AsNoTracking()
            .Where(e => e.ClassId == classId && e.SchoolYearId == schoolYearId)
            .Select(e => e.StudentProfileId)
            .ToListAsync(cancellationToken);

        var students = await (
            from student in dbContext.StudentProfiles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on student.UserId equals user.Id
            where studentIds.Contains(student.Id) && student.IsActive
            orderby student.StudentCode
            select new { student.Id, student.StudentCode, user.DisplayName }
        ).ToListAsync(cancellationToken);

        if (students.Count == 0)
        {
            var emptyRosters = assessments.Select(a => new AssessmentRoster(a.Id, Array.Empty<GradeEntryItem>(), Convert.ToBase64String(a.RowVersion))).ToList();
            return OperationResult<IReadOnlyList<AssessmentRoster>>.Success(emptyRosters);
        }

        var studentIdList = students.Select(s => s.Id).ToList();
        var assessmentIdList = assessments.Select(a => a.Id).ToList();
        var existingEntries = await dbContext.GradeEntries.AsNoTracking()
            .Where(e => studentIdList.Contains(e.StudentProfileId) && assessmentIdList.Contains(e.AssessmentId))
            .ToDictionaryAsync(e => (e.AssessmentId, e.StudentProfileId), cancellationToken);

        var results = assessments.Select(assessment =>
        {
            var studentEntries = students.Select(s =>
            {
                existingEntries.TryGetValue((assessment.Id, s.Id), out var entry);
                return new GradeEntryItem(s.Id, s.StudentCode, s.DisplayName, entry?.Score, entry?.Score);
            }).ToList();
            return new AssessmentRoster(assessment.Id, studentEntries, Convert.ToBase64String(assessment.RowVersion));
        }).ToList();

        return OperationResult<IReadOnlyList<AssessmentRoster>>.Success(results);
    }

    public async Task<OperationResult<AssessmentResult>> CreateAssessmentAsync(
        CreateAssessmentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.MinScore >= command.MaxScore)
            return OperationResult<AssessmentResult>.Failure("invalidScoreRange");

        if (!await dbContext.SchoolYears.AnyAsync(y => y.Id == command.SchoolYearId && y.IsActive, cancellationToken))
            return OperationResult<AssessmentResult>.Failure("schoolYearNotFound");
        if (!await dbContext.ClassRooms.AnyAsync(c => c.Id == command.ClassId && c.IsActive, cancellationToken))
            return OperationResult<AssessmentResult>.Failure("classNotFound");
        if (!await dbContext.Subjects.AnyAsync(s => s.Id == command.SubjectId && s.IsActive, cancellationToken))
            return OperationResult<AssessmentResult>.Failure("subjectNotFound");

        var teacherProfileId = await dbContext.TeacherProfiles
            .Where(p => p.UserId == command.ActorUserId && p.IsActive)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!teacherProfileId.HasValue)
            return OperationResult<AssessmentResult>.Failure("teacherProfileNotFound");

        var nowUtc = timeProvider.GetUtcNow();
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            Code = command.Code.Trim(),
            DisplayName = command.DisplayName.Trim(),
            AssessmentType = command.AssessmentType.Trim(),
            SchoolYearId = command.SchoolYearId,
            Semester = command.Semester,
            ClassId = command.ClassId,
            SubjectId = command.SubjectId,
            MinScore = command.MinScore,
            MaxScore = command.MaxScore,
            Weight = command.Weight,
            DueDate = command.DueDate,
            IsPublished = command.IsPublished,
            CreatedAtUtc = nowUtc
        };
        dbContext.Assessments.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var yearCode = await dbContext.SchoolYears.Where(y => y.Id == command.SchoolYearId).Select(y => y.Code).SingleAsync(cancellationToken);
        var classCode = await dbContext.ClassRooms.Where(c => c.Id == command.ClassId).Select(c => c.Code).SingleAsync(cancellationToken);
        var subjectName = await dbContext.Subjects.Where(s => s.Id == command.SubjectId).Select(s => s.DisplayName).SingleAsync(cancellationToken);

        return OperationResult<AssessmentResult>.Success(new AssessmentResult(
            assessment.Id, assessment.Code, assessment.DisplayName, assessment.AssessmentType,
            assessment.SchoolYearId, yearCode, assessment.Semester,
            assessment.ClassId, classCode, assessment.SubjectId, subjectName,
            assessment.MinScore, assessment.MaxScore, assessment.Weight,
            assessment.DueDate, assessment.IsPublished, Convert.ToBase64String(assessment.RowVersion)));
    }

    public async Task<OperationResult<AssessmentRoster>> SaveGradeEntriesAsync(
        SaveGradeEntriesCommand command,
        CancellationToken cancellationToken)
    {
        var assessment = await dbContext.Assessments
            .SingleOrDefaultAsync(a => a.Id == command.AssessmentId, cancellationToken);
        if (assessment is null) return OperationResult<AssessmentRoster>.Failure("assessmentNotFound");

        var teacherProfileId = await dbContext.TeacherProfiles
            .Where(p => p.UserId == command.ActorUserId && p.IsActive)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!teacherProfileId.HasValue)
            return OperationResult<AssessmentRoster>.Failure("teacherProfileNotFound");

        var isAssigned = await dbContext.TeacherClassSubjectAssignments.AsNoTracking()
            .AnyAsync(a => a.TeacherProfileId == teacherProfileId.Value && a.ClassId == assessment.ClassId
                && a.SchoolYearId == assessment.SchoolYearId && a.IsActive, cancellationToken);
        if (!isAssigned)
            return OperationResult<AssessmentRoster>.Failure("classNotAssigned");

        var studentIds = await dbContext.StudentEnrollments.AsNoTracking()
            .Where(e => e.ClassId == assessment.ClassId && e.SchoolYearId == assessment.SchoolYearId)
            .Select(e => e.StudentProfileId)
            .ToListAsync(cancellationToken);

        var results = new List<GradeEntryItem>();
        var studentCodeLookup = await (
            from student in dbContext.StudentProfiles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on student.UserId equals user.Id
            where studentIds.Contains(student.Id) && student.IsActive
            select new { student.Id, student.StudentCode, user.DisplayName }
        ).ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var update in command.Entries)
        {
            if (!studentIds.Contains(update.StudentProfileId))
                continue;
            if (update.Score.HasValue && (update.Score < assessment.MinScore || update.Score > assessment.MaxScore))
                continue;
            if (!studentCodeLookup.TryGetValue(update.StudentProfileId, out var studentInfo))
                continue;

            var existing = await dbContext.GradeEntries
                .Where(e => e.AssessmentId == command.AssessmentId && e.StudentProfileId == update.StudentProfileId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                var newEntry = new GradeEntry
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = command.AssessmentId,
                    StudentProfileId = update.StudentProfileId,
                    Score = update.Score,
                    TeacherComment = update.TeacherComment,
                    RecordedByUserId = command.ActorUserId,
                    RecordedAtUtc = timeProvider.GetUtcNow()
                };
                dbContext.GradeEntries.Add(newEntry);
            }
            else
            {
                if (update.RowVersion is not null && !RowVersionMatches(update.RowVersion, existing.RowVersion))
                    continue;
                existing.Score = update.Score;
                existing.TeacherComment = update.TeacherComment;
                existing.RecordedByUserId = command.ActorUserId;
                existing.RecordedAtUtc = timeProvider.GetUtcNow();
            }

            results.Add(new GradeEntryItem(update.StudentProfileId, studentInfo.StudentCode,
                studentInfo.DisplayName, update.Score, update.Score));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<AssessmentRoster>.Success(new AssessmentRoster(
            command.AssessmentId, results, Convert.ToBase64String(assessment.RowVersion)));
    }

    private static bool RowVersionMatches(byte[] supplied, byte[] persisted) =>
        supplied.Length == persisted.Length && CryptographicOperations.FixedTimeEquals(supplied, persisted);
}
