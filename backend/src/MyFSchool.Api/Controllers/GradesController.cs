using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
public sealed class GradesController(IGradeAdministrationService gradeService) : ControllerBase
{
    [HttpGet("api/v1/students/me/grades")]
    public async Task<IActionResult> GetStudentGrades(
        [FromQuery] Guid? schoolYearId,
        [FromQuery] int? semester,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await gradeService.GetStudentGradesAsync(userId, schoolYearId, semester, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
            return ProblemResponse(400, result.ErrorCode!, "Không thể tải điểm", "Vui lòng thử lại.");
        return Ok(MapPage(result.Value!));
    }

    [HttpGet("api/v1/parents/me/children/{studentProfileId:guid}/grades")]
    public async Task<IActionResult> GetChildGrades(
        Guid studentProfileId,
        [FromQuery] Guid? schoolYearId,
        [FromQuery] int? semester,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await gradeService.GetParentChildGradesAsync(userId, studentProfileId, schoolYearId, semester, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "studentNotLinked")
                return ProblemResponse(403, "studentNotLinked", "Không có quyền xem", "Học sinh này không phải con của bạn.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể tải điểm", "Vui lòng thử lại.");
        }
        return Ok(MapPage(result.Value!));
    }

    private static GradePageResponse MapPage(GradePage page) => new(
        page.Items.Select(MapSummary).ToList(),
        page.Page, page.PageSize, page.TotalCount, page.TotalPages);

    private static StudentGradeSummaryResponse MapSummary(StudentGradeSummary s) => new(
        s.SchoolYearId, s.SchoolYearCode, s.Semester, s.SubjectId, s.SubjectName,
        s.AverageScore, s.GradeCount,
        s.Grades.Select(MapGrade).ToList());

    private static GradeResponse MapGrade(GradeResult g) => new(
        g.Id, g.AssessmentId, g.AssessmentCode, g.AssessmentName, g.AssessmentType,
        g.Semester, g.SchoolYearId, g.SchoolYearCode, g.ClassId, g.ClassCode,
        g.SubjectId, g.SubjectName, g.Score, g.MaxScore, g.TeacherComment, g.RecordedAtUtc);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}

[ApiController]
[Authorize(Policy = SchoolPolicies.Teacher)]
public sealed class TeacherGradesController(IGradeAdministrationService gradeService) : ControllerBase
{
    [HttpGet("api/v1/teacher/classes/{classId:guid}/assessments")]
    public async Task<IActionResult> GetAssessmentRosters(
        Guid classId,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid schoolYearId,
        [FromQuery] int semester,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await gradeService.GetAssessmentRostersAsync(userId, classId, subjectId, schoolYearId, semester, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "teacherProfileNotFound")
                return ProblemResponse(403, "teacherProfileNotFound", "Không tìm thấy hồ sơ giáo viên", "Bạn không phải giáo viên.");
            if (result.ErrorCode == "classNotAssigned")
                return ProblemResponse(403, "classNotAssigned", "Không được phân công", "Bạn không được phân công dạy lớp này.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể tải danh sách điểm", "Vui lòng thử lại.");
        }
        return Ok(result.Value!.Select(MapRoster).ToList());
    }

    [HttpPost("api/v1/teacher/assessments")]
    public async Task<IActionResult> CreateAssessment(
        CreateAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        if (request.MinScore >= request.MaxScore)
            return ValidationProblem("MaxScore", "Điểm tối đa phải lớn hơn điểm tối thiểu.");

        var result = await gradeService.CreateAssessmentAsync(
            new CreateAssessmentCommand(
                request.Code, request.DisplayName, request.AssessmentType,
                request.SchoolYearId, request.Semester, request.ClassId, request.SubjectId,
                request.MinScore, request.MaxScore, request.Weight, request.DueDate,
                request.IsPublished, userId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "teacherProfileNotFound")
                return ProblemResponse(403, "teacherProfileNotFound", "Không tìm thấy hồ sơ giáo viên", "Bạn không phải giáo viên.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể tạo cột điểm", "Vui lòng kiểm tra thông tin.");
        }
        return Created($"/api/v1/teacher/assessments/{result.Value!.Id}", result.Value);
    }

    [HttpPost("api/v1/teacher/assessments/{assessmentId:guid}/grade-entries")]
    public async Task<IActionResult> SaveGradeEntries(
        Guid assessmentId,
        SaveGradeEntriesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var entries = request.Entries.Select(e =>
        {
            byte[]? rv = null;
            if (!string.IsNullOrEmpty(e.RowVersion))
            {
                try { rv = Convert.FromBase64String(e.RowVersion); }
                catch { }
            }
            return new GradeEntryUpdate(e.StudentProfileId, e.Score, e.TeacherComment, rv);
        }).ToList();

        var result = await gradeService.SaveGradeEntriesAsync(
            new SaveGradeEntriesCommand(assessmentId, entries, userId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "assessmentNotFound")
                return ProblemResponse(404, "assessmentNotFound", "Không tìm thấy cột điểm", "Cột điểm không tồn tại.");
            if (result.ErrorCode == "teacherProfileNotFound")
                return ProblemResponse(403, "teacherProfileNotFound", "Không tìm thấy hồ sơ giáo viên", "Bạn không phải giáo viên.");
            if (result.ErrorCode == "classNotAssigned")
                return ProblemResponse(403, "classNotAssigned", "Không được phân công", "Bạn không được phân công dạy lớp này.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể lưu điểm", "Vui lòng thử lại.");
        }
        return Ok(MapRoster(result.Value!));
    }

    private static AssessmentRosterResponse MapRoster(AssessmentRoster r) => new(
        r.Id,
        r.Students.Select(s => new GradeEntryItemResponse(
            s.StudentProfileId, s.StudentCode, s.StudentDisplayName,
            s.ExistingScore, s.NewScore)).ToList(),
        r.RowVersion);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private BadRequestObjectResult ValidationProblem(string field, string message)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] })
        { Status = StatusCodes.Status400BadRequest, Title = "Yêu cầu không hợp lệ" };
        problem.Extensions["code"] = "validationFailed";
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return BadRequest(problem);
    }

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
