using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.Administrator)]
[Route("api/v1/admin")]
public sealed class AdminSchoolController(
    ISchoolReferenceAdministrationService schoolService) : ControllerBase
{
    [HttpPost("school-years")]
    public async Task<IActionResult> CreateSchoolYear(
        CreateSchoolYearRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        if (request.EndDate <= request.StartDate)
            return ValidationProblemResponse("endDate", "Ngày kết thúc phải sau ngày bắt đầu.");

        var result = await schoolService.CreateSchoolYearAsync(
            new CreateSchoolYearCommand(
                request.Code, request.DisplayName, request.StartDate, request.EndDate,
                actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (result.IsSuccess)
        {
            var year = result.Value!;
            return Created($"/api/v1/admin/school-years/{year.Id}", ToResponse(year));
        }
        return result.ErrorCode == "schoolYearAlreadyExists"
            ? ProblemResponse(409, "schoolYearAlreadyExists", "Năm học đã tồn tại", "Mã năm học đã được sử dụng.")
            : ProblemResponse(400, result.ErrorCode!, "Không thể tạo năm học", "Vui lòng kiểm tra thông tin đã nhập.");
    }

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass(
        CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        var result = await schoolService.CreateClassAsync(
            new CreateClassCommand(
                request.Code, request.DisplayName, request.GradeLevel, request.SchoolYearId,
                request.HomeroomTeacherProfileId, actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        return ClassResult(result, created: true);
    }

    [HttpPatch("classes/{classId:guid}")]
    public async Task<IActionResult> UpdateClass(
        Guid classId,
        UpdateClassRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        if (!TryParseRowVersion(request.RowVersion, out var rowVersion))
            return ValidationProblemResponse("rowVersion", "Phiên bản lớp không hợp lệ.");

        var result = await schoolService.UpdateClassAsync(
            new UpdateClassCommand(
                classId, request.DisplayName, request.GradeLevel, request.HomeroomTeacherProfileId,
                request.IsActive, rowVersion, actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        return ClassResult(result, created: false);
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        var result = await schoolService.CreateSubjectAsync(
            new CreateSubjectCommand(request.Code, request.DisplayName, actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (result.IsSuccess)
        {
            var subject = result.Value!;
            return Created($"/api/v1/admin/subjects/{subject.Id}",
                new SubjectResponse(subject.Id, subject.Code, subject.DisplayName, subject.IsActive, subject.RowVersion));
        }
        return result.ErrorCode == "subjectAlreadyExists"
            ? ProblemResponse(409, "subjectAlreadyExists", "Môn học đã tồn tại", "Mã môn học đã được sử dụng.")
            : ProblemResponse(400, result.ErrorCode!, "Không thể tạo môn học", "Vui lòng kiểm tra thông tin đã nhập.");
    }

    [HttpPost("teacher-assignments")]
    public async Task<IActionResult> CreateTeacherAssignment(
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        var result = await schoolService.CreateTeacherAssignmentAsync(
            new CreateTeacherAssignmentCommand(
                request.TeacherProfileId, request.ClassId, request.SubjectId, request.SchoolYearId,
                actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (result.IsSuccess)
        {
            var assignment = result.Value!;
            return Created($"/api/v1/admin/teacher-assignments/{assignment.Id}",
                new TeacherAssignmentResponse(
                    assignment.Id, assignment.TeacherProfileId, assignment.ClassId,
                    assignment.SubjectId, assignment.SchoolYearId, assignment.IsActive, assignment.RowVersion));
        }
        return result.ErrorCode switch
        {
            "teacherProfileNotFound" => ProblemResponse(404, "teacherProfileNotFound", "Không tìm thấy giáo viên", "Hồ sơ giáo viên không tồn tại hoặc không còn hoạt động."),
            "classNotFound" => ProblemResponse(404, "classNotFound", "Không tìm thấy lớp", "Lớp học không tồn tại hoặc không còn hoạt động."),
            "subjectNotFound" => ProblemResponse(404, "subjectNotFound", "Không tìm thấy môn học", "Môn học không tồn tại hoặc không còn hoạt động."),
            "schoolYearNotFound" => ProblemResponse(404, "schoolYearNotFound", "Không tìm thấy năm học", "Năm học không tồn tại hoặc không còn hoạt động."),
            "teacherAssignmentAlreadyExists" => ProblemResponse(409, "teacherAssignmentAlreadyExists", "Phân công đã tồn tại", "Giáo viên đã được phân công môn học này cho lớp này."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể tạo phân công", "Vui lòng kiểm tra thông tin đã nhập.")
        };
    }

    [HttpPost("student-enrollments")]
    public async Task<IActionResult> CreateStudentEnrollment(
        CreateStudentEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        var result = await schoolService.CreateStudentEnrollmentAsync(
            new CreateStudentEnrollmentCommand(
                request.StudentProfileId, request.ClassId, request.SchoolYearId,
                request.EnrolledOn, actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (result.IsSuccess)
        {
            var enrollment = result.Value!;
            return Created($"/api/v1/admin/student-enrollments/{enrollment.Id}",
                new StudentEnrollmentResponse(
                    enrollment.Id, enrollment.StudentProfileId, enrollment.ClassId,
                    enrollment.SchoolYearId, enrollment.EnrolledOn, enrollment.LeftOn, enrollment.RowVersion));
        }
        return result.ErrorCode switch
        {
            "studentProfileNotFound" => ProblemResponse(404, "studentProfileNotFound", "Không tìm thấy học sinh", "Hồ sơ học sinh không tồn tại hoặc không còn hoạt động."),
            "classNotFound" => ProblemResponse(404, "classNotFound", "Không tìm thấy lớp", "Lớp học không tồn tại hoặc không còn hoạt động."),
            "schoolYearNotFound" => ProblemResponse(404, "schoolYearNotFound", "Không tìm thấy năm học", "Năm học không tồn tại hoặc không còn hoạt động."),
            "studentEnrollmentAlreadyExists" => ProblemResponse(409, "studentEnrollmentAlreadyExists", "Học sinh đã được xếp lớp", "Học sinh đã có lớp trong năm học này."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể xếp lớp", "Vui lòng kiểm tra thông tin đã nhập.")
        };
    }

    private IActionResult ClassResult(OperationResult<ClassResult> result, bool created)
    {
        if (result.IsSuccess)
        {
            var classroom = result.Value!;
            return created
                ? Created($"/api/v1/admin/classes/{classroom.Id}", ToResponse(classroom))
                : Ok(ToResponse(classroom));
        }
        return result.ErrorCode switch
        {
            "schoolYearNotFound" => ProblemResponse(404, "schoolYearNotFound", "Không tìm thấy năm học", "Năm học không tồn tại hoặc không còn hoạt động."),
            "teacherProfileNotFound" => ProblemResponse(404, "teacherProfileNotFound", "Không tìm thấy giáo viên", "Hồ sơ giáo viên không tồn tại hoặc không còn hoạt động."),
            "classNotFound" => ProblemResponse(404, "classNotFound", "Không tìm thấy lớp", "Lớp học không tồn tại hoặc không còn hoạt động."),
            "classAlreadyExists" => ProblemResponse(409, "classAlreadyExists", "Lớp đã tồn tại", "Mã lớp đã được sử dụng trong năm học này."),
            "concurrencyConflict" => ProblemResponse(409, "concurrencyConflict", "Dữ liệu đã thay đổi", "Lớp đã được cập nhật bởi một thao tác khác."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể lưu lớp", "Vui lòng kiểm tra thông tin đã nhập.")
        };
    }

    private static SchoolYearResponse ToResponse(SchoolYearResult year) =>
        new(year.Id, year.Code, year.DisplayName, year.StartDate, year.EndDate, year.IsActive, year.RowVersion);

    private static ClassResponse ToResponse(ClassResult classroom) =>
        new(classroom.Id, classroom.Code, classroom.DisplayName, classroom.GradeLevel,
            classroom.SchoolYearId, classroom.SchoolYearCode, classroom.HomeroomTeacherProfileId,
            classroom.IsActive, classroom.RowVersion);

    private bool TryGetActorUserId(out Guid actorUserId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out actorUserId);

    private static bool TryParseRowVersion(string value, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }

    private BadRequestObjectResult ValidationProblemResponse(string field, string message)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Yêu cầu không hợp lệ",
            Detail = "Vui lòng kiểm tra các trường được đánh dấu."
        };
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
