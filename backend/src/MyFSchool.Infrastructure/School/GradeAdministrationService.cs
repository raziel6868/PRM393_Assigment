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
    public async Task<OperationResult<IReadOnlyList<SemesterInfo>>> GetSemestersAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Both the student themselves and the student's linked parents need to
        // see the same semester list. Collect every StudentProfileId the caller
        // is allowed to read for, then return the union of distinct
        // (SchoolYearId, Semester) pairs that have published grade entries for
        // those students. Returning an empty list for "no permission / no
        // data" keeps the controller's 400-only contract intact; unknown
        // callers do not get a 404.
        var studentIds = await (
            from profile in dbContext.StudentProfiles.AsNoTracking()
            where profile.IsActive && profile.UserId == userId
            select profile.Id
        ).ToListAsync(cancellationToken);

        if (studentIds.Count == 0)
        {
            var linkedIds = await (
                from parent in dbContext.ParentProfiles.AsNoTracking()
                join link in dbContext.ParentStudentLinks.AsNoTracking() on parent.Id equals link.ParentProfileId
                where parent.IsActive && parent.UserId == userId && link.IsActive
                select link.StudentProfileId
            ).ToListAsync(cancellationToken);
            studentIds.AddRange(linkedIds);
        }

        if (studentIds.Count == 0)
            return OperationResult<IReadOnlyList<SemesterInfo>>.Success(Array.Empty<SemesterInfo>());

        var semesters = await (
            from entry in dbContext.GradeEntries.AsNoTracking()
            join assessment in dbContext.Assessments.AsNoTracking() on entry.AssessmentId equals assessment.Id
            join year in dbContext.SchoolYears.AsNoTracking() on assessment.SchoolYearId equals year.Id
            where studentIds.Contains(entry.StudentProfileId)
            orderby year.Code descending, assessment.Semester descending
            select new { YearId = year.Id, YearCode = year.Code, YearStart = year.StartDate, YearEnd = year.EndDate, assessment.Semester }
        )
        .Distinct()
        .ToListAsync(cancellationToken);

        var result = semesters.Select(s => new SemesterInfo(
            BuildSemesterKey(s.YearId, s.Semester),
            s.Semester,
            s.YearId,
            s.YearCode,
            s.YearStart,
            s.YearEnd))
        .ToList();

        return OperationResult<IReadOnlyList<SemesterInfo>>.Success(result);
    }

    public async Task<OperationResult<GradeSummaryResult>> GetGradeSummaryAsync(
        Guid userId,
        string role,
        string semesterKey,
        CancellationToken cancellationToken)
    {
        if (!TryParseSemesterKey(semesterKey, out var schoolYearId, out var semesterNumber))
            return OperationResult<GradeSummaryResult>.Failure("invalidSemesterKey");

        var year = await dbContext.SchoolYears.AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == schoolYearId, cancellationToken);
        if (year is null)
            return OperationResult<GradeSummaryResult>.Failure("semesterNotFound");

        Guid? studentProfileId = null;
        if (role == "student")
        {
            studentProfileId = await dbContext.StudentProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else if (role == "parent")
        {
            studentProfileId = await (
                from parent in dbContext.ParentProfiles.AsNoTracking()
                join link in dbContext.ParentStudentLinks.AsNoTracking() on parent.Id equals link.ParentProfileId
                where parent.UserId == userId && parent.IsActive && link.IsActive
                orderby link.IsPrimaryContact descending
                select link.StudentProfileId
            ).FirstOrDefaultAsync(cancellationToken);
        }

        if (!studentProfileId.HasValue)
            return OperationResult<GradeSummaryResult>.Failure("studentProfileNotFound");

        var grades = await (
            from entry in dbContext.GradeEntries.AsNoTracking()
            join assessment in dbContext.Assessments.AsNoTracking() on entry.AssessmentId equals assessment.Id
            join subject in dbContext.Subjects.AsNoTracking() on assessment.SubjectId equals subject.Id
            where entry.StudentProfileId == studentProfileId.Value
                && assessment.SchoolYearId == schoolYearId
                && assessment.Semester == semesterNumber
            select new
            {
                SubjectId = subject.Id,
                SubjectName = subject.DisplayName,
                GradeId = entry.Id,
                AssessmentName = assessment.DisplayName,
                entry.Score
            }
        ).ToListAsync(cancellationToken);

        var subjects = grades
            .GroupBy(g => new { g.SubjectId, g.SubjectName })
            .Select(g => new SubjectGradeSummary(
                g.Key.SubjectId,
                g.Key.SubjectName,
                g.Where(x => x.Score.HasValue).Select(x => x.Score!.Value).DefaultIfEmpty(0).Average(),
                g.Count(),
                g.Select(x => new GradeSummaryEntry(
                    x.GradeId,
                    x.AssessmentName,
                    x.Score)).ToList()))
            .ToList();

        return OperationResult<GradeSummaryResult>.Success(new GradeSummaryResult(
            semesterKey,
            semesterNumber,
            schoolYearId,
            year.Code,
            subjects));
    }

    public async Task<OperationResult<GradeDetailResult>> GetGradeDetailAsync(
        Guid userId,
        string role,
        Guid gradeId,
        CancellationToken cancellationToken)
    {
        var gradeEntry = await (
            from entry in dbContext.GradeEntries.AsNoTracking()
            join assessment in dbContext.Assessments.AsNoTracking() on entry.AssessmentId equals assessment.Id
            join subject in dbContext.Subjects.AsNoTracking() on assessment.SubjectId equals subject.Id
            join year in dbContext.SchoolYears.AsNoTracking() on assessment.SchoolYearId equals year.Id
            join classroom in dbContext.ClassRooms.AsNoTracking() on assessment.ClassId equals classroom.Id
            where entry.Id == gradeId
            select new { entry, assessment, subject, year, classroom }
        ).FirstOrDefaultAsync(cancellationToken);

        if (gradeEntry is null)
            return OperationResult<GradeDetailResult>.Failure("gradeNotFound");

        Guid? studentProfileId = null;
        if (role == "student")
        {
            studentProfileId = await dbContext.StudentProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else if (role == "parent")
        {
            var linked = await (
                from parent in dbContext.ParentProfiles.AsNoTracking()
                join link in dbContext.ParentStudentLinks.AsNoTracking() on parent.Id equals link.ParentProfileId
                where parent.UserId == userId && parent.IsActive && link.IsActive
                select link.StudentProfileId
            ).ToListAsync(cancellationToken);

            if (!linked.Contains(gradeEntry.entry.StudentProfileId))
                return OperationResult<GradeDetailResult>.Failure("studentNotLinked");
            studentProfileId = gradeEntry.entry.StudentProfileId;
        }
        else if (role == "teacher")
        {
            // Teacher access is scoped through an active class/subject/school-year
            // assignment against the assessment the grade belongs to. Admins never
            // reach this method.
            var isAssigned = await (
                from teacher in dbContext.TeacherProfiles.AsNoTracking()
                join assignment in dbContext.TeacherClassSubjectAssignments.AsNoTracking()
                    on teacher.Id equals assignment.TeacherProfileId
                where teacher.UserId == userId
                    && teacher.IsActive
                    && assignment.IsActive
                    && assignment.ClassId == gradeEntry.assessment.ClassId
                    && assignment.SubjectId == gradeEntry.assessment.SubjectId
                    && assignment.SchoolYearId == gradeEntry.assessment.SchoolYearId
                select assignment.Id
            ).AnyAsync(cancellationToken);

            if (!isAssigned)
                return OperationResult<GradeDetailResult>.Failure("gradeNotFound");
            studentProfileId = gradeEntry.entry.StudentProfileId;
        }

        if (!studentProfileId.HasValue || studentProfileId.Value != gradeEntry.entry.StudentProfileId)
            return OperationResult<GradeDetailResult>.Failure("gradeNotFound");

        return OperationResult<GradeDetailResult>.Success(new GradeDetailResult(
            gradeEntry.entry.Id,
            gradeEntry.assessment.Id,
            gradeEntry.assessment.Code,
            gradeEntry.assessment.DisplayName,
            gradeEntry.assessment.AssessmentType,
            gradeEntry.assessment.Semester,
            gradeEntry.year.Id,
            gradeEntry.year.Code,
            gradeEntry.classroom.Id,
            gradeEntry.classroom.Code,
            gradeEntry.subject.Id,
            gradeEntry.subject.DisplayName,
            gradeEntry.entry.Score,
            gradeEntry.assessment.MaxScore,
            gradeEntry.entry.TeacherComment,
            gradeEntry.entry.RecordedAtUtc));
    }

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
                return new GradeEntryItem(entry?.Id ?? Guid.Empty, s.Id, s.StudentCode, s.DisplayName, entry?.Score, entry?.Score);
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

            results.Add(new GradeEntryItem(existing?.Id ?? Guid.Empty, update.StudentProfileId, studentInfo.StudentCode,
                studentInfo.DisplayName, update.Score, update.Score));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<AssessmentRoster>.Success(new AssessmentRoster(
            command.AssessmentId, results, Convert.ToBase64String(assessment.RowVersion)));
    }

    private static bool RowVersionMatches(byte[] supplied, byte[] persisted) =>
        supplied.Length == persisted.Length && CryptographicOperations.FixedTimeEquals(supplied, persisted);

    // Semester keys are deterministic opaque tokens of the form
    // "{schoolYearId:N}-{semesterNumber}". They travel in URL segments, so the
    // format stays inside ASCII and round-trips without lossy encoding. Any
    // value that does not match this exact shape is rejected as
    // invalidSemesterKey, which the controller surfaces as a 400 problem.
    internal static string BuildSemesterKey(Guid schoolYearId, int semesterNumber) =>
        $"{schoolYearId:N}-{semesterNumber}";

    internal static bool TryParseSemesterKey(string semesterKey, out Guid schoolYearId, out int semesterNumber)
    {
        schoolYearId = Guid.Empty;
        semesterNumber = 0;
        if (string.IsNullOrWhiteSpace(semesterKey)) return false;
        var separator = semesterKey.LastIndexOf('-');
        if (separator <= 0 || separator >= semesterKey.Length - 1) return false;
        var yearPart = semesterKey[..separator];
        var semesterPart = semesterKey[(separator + 1)..];
        if (!Guid.TryParseExact(yearPart, "N", out schoolYearId)) return false;
        if (!int.TryParse(semesterPart, out semesterNumber)) return false;
        if (semesterNumber < 1) return false;
        return true;
    }
}
