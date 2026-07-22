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
[Route("api/v1/grades")]
public sealed class GradesController(IGradeAdministrationService gradeService) : ControllerBase
{
    [HttpGet("semesters")]
    public async Task<IActionResult> GetSemesters(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await gradeService.GetSemestersAsync(userId, cancellationToken);
        if (!result.IsSuccess)
            return ProblemResponse(400, result.ErrorCode!, "Không thể tải học kỳ", "Vui lòng thử lại.");
        return Ok(result.Value);
    }

    [HttpGet("summary/{semesterKey}")]
    public async Task<IActionResult> GetGradeSummary(
        string semesterKey,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(semesterKey))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Khóa học kỳ không hợp lệ" });

        var role = ResolveRole();
        var result = await gradeService.GetGradeSummaryAsync(userId, role, semesterKey, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "studentNotLinked")
                return ProblemResponse(403, "studentNotLinked", "Không có quyền xem", "Học sinh này không phải con của bạn.");
            if (result.ErrorCode == "semesterNotFound")
                return ProblemResponse(404, "semesterNotFound", "Không tìm thấy học kỳ", "Học kỳ không tồn tại.");
            if (result.ErrorCode == "invalidSemesterKey")
                return ProblemResponse(400, "invalidSemesterKey", "Khóa học kỳ không hợp lệ", "Vui lòng chọn lại học kỳ.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể tải điểm", "Vui lòng thử lại.");
        }
        return Ok(result.Value);
    }

    [HttpGet("{gradeId:guid}")]
    public async Task<IActionResult> GetGradeDetail(
        Guid gradeId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var role = ResolveRole();
        var result = await gradeService.GetGradeDetailAsync(userId, role, gradeId, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "gradeNotFound")
                return ProblemResponse(404, "gradeNotFound", "Không tìm thấy điểm", "Điểm không tồn tại hoặc bạn không có quyền xem.");
            if (result.ErrorCode == "studentNotLinked")
                return ProblemResponse(403, "studentNotLinked", "Không có quyền xem", "Học sinh này không phải con của bạn.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể tải chi tiết điểm", "Vui lòng thử lại.");
        }
        return Ok(result.Value);
    }

    private string ResolveRole()
    {
        if (User.IsInRole("student")) return "student";
        if (User.IsInRole("teacher")) return "teacher";
        if (User.IsInRole("parent")) return "parent";
        return "unknown";
    }

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
[Route("api/v1/teacher")]
public sealed class TeacherGradesController(IGradeAdministrationService gradeService) : ControllerBase
{
    [HttpGet("classes/{classId:guid}/assessments")]
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

    [HttpPost("assessments")]
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

    [HttpPost("assessments/{assessmentId:guid}/grade-entries")]
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
            s.GradeEntryId, s.StudentProfileId, s.StudentCode, s.StudentDisplayName,
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
