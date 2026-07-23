using ClosedXML.Excel;

namespace MyFSchool.Infrastructure.Imports;

internal static class ExcelTemplateBuilder
{
    public const string SheetStudents = "Students";
    public const string SheetParents = "Parents";
    public const string SheetParentStudentLinks = "ParentStudentLinks";
    public const string SheetTeachers = "Teachers";
    public const string SheetTeacherAssignments = "TeacherAssignments";
    public const string InstructionsSheetName = "Instructions";

    public static readonly IReadOnlyList<string> Sheets = new[]
    {
        SheetStudents,
        SheetParents,
        SheetParentStudentLinks,
        SheetTeachers,
        SheetTeacherAssignments,
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Headers =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [SheetStudents] = new[] { "studentCode", "fullName", "dateOfBirth", "classCode", "userName", "email" },
            [SheetParents] = new[] { "parentCode", "fullName", "email", "phone" },
            [SheetParentStudentLinks] = new[] { "parentCode", "studentCode", "relationship", "isPrimaryContact" },
            [SheetTeachers] = new[] { "employeeCode", "fullName", "email", "phone" },
            [SheetTeacherAssignments] = new[] { "employeeCode", "classCode", "subjectCode", "schoolYearCode" },
        };

    /// <summary>
    /// Renders the canonical P0 Excel template. Each sheet uses fixed machine headers
    /// (no aliasing); the workbook carries a Vietnamese Instructions sheet pinned to
    /// the canonical template version so downstream imports always know what they got.
    /// </summary>
    public static byte[] Render(string templateVersion)
    {
        using var workbook = new XLWorkbook();

        var instructionsSheet = workbook.AddWorksheet(InstructionsSheetName);
        WriteInstructions(instructionsSheet, templateVersion);

        foreach (var sheet in Sheets)
        {
            var ws = workbook.AddWorksheet(sheet);
            var headers = Headers[sheet];
            for (var col = 0; col < headers.Count; col++)
            {
                ws.Cell(1, col + 1).Value = headers[col];
            }
            ws.Row(1).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(1);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteInstructions(IXLWorksheet sheet, string templateVersion)
    {
        var rows = new[]
        {
            "Hướng dẫn nhập liệu MyFSchool",
            $"Phiên bản mẫu: {templateVersion}",
            string.Empty,
            "- Không được thay đổi tên cột hoặc thứ tự cột.",
            "- Mỗi workbook phải có đủ các sheet: Students, Parents, ParentStudentLinks, Teachers, TeacherAssignments.",
            "- studentCode, parentCode, employeeCode là mã ổn định do nhà trường cấp và không được trùng nhau trong workbook.",
            "- dateOfBirth dùng định dạng YYYY-MM-DD hoặc ô ngày Excel thật.",
            "- Email phải hợp lệ và duy nhất khi được cung cấp (Teacher/Parent bắt buộc có email).",
            "- classCode, subjectCode, schoolYearCode phải tồn tại trong cơ sở dữ liệu MyFSchool; nếu không hàng đó sẽ bị từ chối.",
            "- relationship dùng một trong các giá trị: father, mother, guardian, other.",
            "- Liên kết phụ huynh/học sinh yêu cầu cả parentCode và studentCode tồn tại trong workbook hoặc đã có trong cơ sở dữ liệu.",
            "- Mã hoặc email đã tồn tại sẽ được báo ở bước kiểm tra để tránh tạo tài khoản trùng.",
            string.Empty,
            "Liên hệ quản trị viên nếu cần hỗ trợ thêm.",
        };

        for (var i = 0; i < rows.Length; i++)
        {
            sheet.Cell(i + 1, 1).Value = rows[i];
        }
        sheet.Column(1).Width = 90;
    }
}
