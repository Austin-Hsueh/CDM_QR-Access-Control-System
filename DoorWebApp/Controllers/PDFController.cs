using DoorDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog.Targets;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoorWebApp.Controllers
{
    [Route("api/pdf/")]
    [ApiController]
    public class PDFController : ControllerBase
    {
        private readonly ILogger<PDFController> _log;
        private readonly DoorDbContext _ctx;
        private static bool _licenseInitialized;
        private static readonly object _licenseLock = new object();

        public PDFController(ILogger<PDFController> log, DoorDbContext ctx)
        {
            _log = log;
            _ctx = ctx;
        }

        [HttpGet("v1/SalaryReport")]
        public IActionResult GetSalaryReport(
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] int teacherId,
            [FromQuery] bool includePaid = true)
        {
            if (!DateTime.TryParse(startDate, out var start))
                return BadRequest("結算日期起始格式錯誤");
            if (!DateTime.TryParse(endDate, out var end))
                return BadRequest("結算日期結束格式錯誤");
            if (end < start)
                return BadRequest("結算日期結束不能早於起始日期");

            EnsureQuestPdfLicense();

            var data = BuildSalaryReportData(start, end, teacherId, includePaid);
            var pdfBytes = RenderSalaryPdf(data);
            var fileName = $"salary-report-{start:yyyyMMdd}-{end:yyyyMMdd}-teacher-{teacherId}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("v1/CompanyProfitSummary")]
        public IActionResult GetCompanyProfitReport(
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] int teacherId,
            [FromQuery] bool includePaid = true)
        {
            if (!DateTime.TryParse(startDate, out var start))
                return BadRequest("結算日期起始格式錯誤");
            if (!DateTime.TryParse(endDate, out var end))
                return BadRequest("結算日期結束格式錯誤");
            if (end < start)
                return BadRequest("結算日期結束不能早於起始日期");

            EnsureQuestPdfLicense();

            var data = BuildCompanyProfitData(start, end, teacherId, includePaid);
            var pdf = RenderCompanyProfitPdf(data);
            var fileName = $"company-profit-summary-{start:yyyyMMdd}-{end:yyyyMMdd}-teacher-{teacherId}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        private static void EnsureQuestPdfLicense()
        {
            if (_licenseInitialized) return;
            lock (_licenseLock)
            {
                if (_licenseInitialized) return;
                QuestPDF.Settings.License = LicenseType.Community;
                _licenseInitialized = true;
            }
        }

        // ======== Salary Report Rendering & Data ========
        private byte[] RenderSalaryPdf(SalaryReportSample sample)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Microsoft JhengHei").FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("VMS-POS").SemiBold().FontSize(12);
                            row.RelativeItem().AlignCenter().Text("私立樂光音樂短期補習班").Bold().FontSize(12);
                            row.RelativeItem().AlignRight().Text($"頁碼 1").FontSize(10);
                        });

                        col.Item()
                            .PaddingBottom(6)
                            .AlignCenter()
                            .Text($"教師 {sample.TeacherName} {sample.Period} 上課拆帳明細表")
                            .Bold().FontSize(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(28);
                                columns.RelativeColumn(1.8f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("序").SemiBold();
                                header.Cell().Element(HeaderCell).Text("上課學生").SemiBold();
                                header.Cell().Element(HeaderCell).Text("樂器").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數一").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數二").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數三").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數四").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數五").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數六").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數七").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數八").SemiBold();
                                header.Cell().Element(HeaderCell).Text("特殊加給").SemiBold();
                                header.Cell().Element(HeaderCell).Text("堂數/鐘點折帳小計").SemiBold();
                            });

                            var index = 1;
                            foreach (var row in sample.Rows)
                            {
                                var lessonChunks = Chunk(row.Lessons, 8);

                                for (var chunkIndex = 0; chunkIndex < lessonChunks.Count; chunkIndex++)
                                {
                                    var chunk = lessonChunks[chunkIndex];
                                    var isFirst = chunkIndex == 0;
                                    var isLast = chunkIndex == lessonChunks.Count - 1;

                                    table.Cell().Element(BodyCell).Text(isFirst ? index.ToString() : string.Empty);
                                    table.Cell().Element(BodyCell).Text(isFirst ? row.StudentName : string.Empty);
                                    table.Cell().Element(BodyCell).Text(isFirst ? row.Instrument : string.Empty);

                                    for (var i = 0; i < 8; i++)
                                    {
                                        var lesson = i < chunk.Count ? chunk[i] : null;
                                        var text = lesson == null ? string.Empty : $"{lesson.Date}\n{lesson.Amount:0.00}";
                                        table.Cell().Element(BodyCell).Text(text);
                                    }

                                    var special = isFirst && row.SpecialBonus.HasValue ? $"{row.SpecialBonus:0.00}" : string.Empty;
                                    table.Cell().Element(BodyCell).Text(special);

                                    var totalText = isLast ? $"{row.Lessons.Count} ({row.TotalHours:0.0}H)\n${row.TotalAmount:0.00}" : string.Empty;
                                    table.Cell().Element(BodyCell).Text(totalText);
                                }

                                index++;
                            }
                        });

                        var subtotalCeil = Math.Ceiling(sample.TotalAmount);
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text($"老師基本折帳比: {sample.BaseRatio:0.00}    跳級人數: {sample.PromotionCount}    跳級折帳比: {sample.PromotionRatio:0.00}");
                            row.RelativeItem().AlignRight().Text($"小計: ${subtotalCeil:N0}");
                        });

                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text($"總堂數: {sample.TotalLessons}");
                            row.RelativeItem().AlignRight().Text($"總時數: {sample.TotalHours:0.0}");
                        });
                    });
                });
            }).GeneratePdf();
        }

        private SalaryReportSample BuildSalaryReportData(DateTime start, DateTime end, int teacherId, bool includePaid)
        {
            var teacher = _ctx.TblUsers.FirstOrDefault(t => t.Id == teacherId && !t.IsDelete);
            var teacherName = teacher?.DisplayName ?? teacher?.Username ?? $"老師 {teacherId}";
            var teacherRatio = _ctx.TblTeacherSettlement.FirstOrDefault(x => x.TeacherId == teacherId)?.SplitRatio ?? 0m;

            var attendances = _ctx.TblAttendance
                .Include(a => a.AttendanceFee)
                .Include(a => a.StudentPermission)!
                    .ThenInclude(sp => sp.User)
                .Include(a => a.StudentPermission)!
                    .ThenInclude(sp => sp.Course)!
                        .ThenInclude(c => c.CourseFee)
                .Include(a => a.StudentPermission)!
                    .ThenInclude(sp => sp.StudentPermissionFees)!
                        .ThenInclude(spf => spf.Payment)
                .Where(a => !a.IsDelete && a.StudentPermission != null && a.StudentPermission.TeacherId == teacherId)
                .AsNoTracking()
                .ToList();

            var rowsByStudent = new Dictionary<int, SalaryRowBuilder>();

            // 對每個 StudentPermission 的 Attendance 進行分組處理
            var attendancesByPermission = attendances
                .Where(a => ParseDate(a.AttendanceDate) != null)
                .GroupBy(a => a.StudentPermissionId)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.AttendanceDate).ToList());

            foreach (var att in attendances)
            {
                var attendanceDate = ParseDate(att.AttendanceDate);
                if (attendanceDate == null || attendanceDate < start || attendanceDate > end) continue;

                var sp = att.StudentPermission;
                if (sp == null || sp.IsDelete) continue;

                var permissions = _ctx.TblStudentPermission
                    .Where(sp1 => !sp1.IsDelete
                                 && sp1.UserId == sp.UserId
                                 && sp1.CourseId == sp.CourseId)
                    .Include(sp => sp.Course)                 // 課程名稱
                        .ThenInclude(c => c.CourseFee)        // 課程費用 + 教材費
                    .Include(sp => sp.StudentPermissionFees)  // 學生權限費用列表
                        .ThenInclude(spf => spf.Payment)       // 已收金額 & 結帳單號 (一對一)
                    .Include(sp => sp.Attendances)            // 課程一~四
                    .ToList();
                var combinedAttendances = permissions
                    .SelectMany(sp => sp.Attendances ?? new List<TblAttendance>())
                    .Where(a => !a.IsDelete)
                    .OrderBy(a => a.AttendanceDate)
                    .ToList();
                var combinedFees = permissions
                    .SelectMany(sp => sp.StudentPermissionFees ?? new List<TblStudentPermissionFee>())
                    .Where(spf => !spf.IsDelete)
                    .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
                    .ThenBy(spf => spf.Id)
                    .ToList();

                // 檢查是否有未刪除的學生權限費用記錄
                var hasStudentPermissionFee = sp.StudentPermissionFees?.Any(f => !f.IsDelete) ?? false;
                if (!hasStudentPermissionFee)
                {
                    continue;
                }

                // 找出當前 attendance 在該 StudentPermission 中的索引（依日期排序）
                var spAttendances = combinedAttendances;
                var attendanceIndex = spAttendances.FindIndex(a => a.Id == att.Id);
                if (attendanceIndex < 0) attendanceIndex = 0;

                // 依各筆費用的 Hours 動態計算所屬組別
                var sortedFees = combinedFees.Where(f => !f.IsDelete).OrderBy(f => f.Id).ToList() ?? new List<TblStudentPermissionFee>();
                int feeGroupIndex = 0;
                int accIndex = 0;
                for (int i = 0; i < sortedFees.Count; i++)
                {
                    int hoursPerFee = (int)Math.Max(1, Math.Ceiling(sortedFees[i].Hours != 0 ? (double)sortedFees[i].Hours : 4));
                    if (attendanceIndex < accIndex + hoursPerFee)
                    {
                        feeGroupIndex = i;
                        break;
                    }
                    accIndex += hoursPerFee;
                    feeGroupIndex = i + 1;
                }
                var correspondingFee = feeGroupIndex < sortedFees.Count ? sortedFees[feeGroupIndex] : null;

                if (!includePaid)
                {
                    // 檢查該組的 StudentPermissionFee 是否有繳款
                    var hasPaidFee = correspondingFee?.Payment != null && correspondingFee.Payment.Pay > 0;
                    if (!hasPaidFee) continue;
                }

                var user = sp.User;
                var studentName = user?.DisplayName ?? user?.Username ?? $"學生 {sp.UserId}";
                var instrument = sp.Course?.Name ?? "-";

                var fee = att.AttendanceFee;
                var hours = fee?.Hours > 0 ? fee.Hours : 1m;
                var amount = (fee?.Amount ?? 0) + (fee?.AdjustmentAmount ?? 0);

                if (!rowsByStudent.TryGetValue(sp.UserId, out var builder))
                {
                    builder = new SalaryRowBuilder(studentName, instrument);
                    rowsByStudent[sp.UserId] = builder;
                }

                builder.Lessons.Add(new LessonItem(attendanceDate.Value.ToString("MM/dd"), amount, hours));
            }

            var rows = rowsByStudent.Values
                .Select(b => b.ToRow())
                .OrderBy(r => r.StudentName)
                .ToList();

            var totalLessons = rows.Sum(r => r.Lessons.Count);
            var totalHours = rows.Sum(r => r.TotalHours);
            var totalAmount = rows.Sum(r => r.TotalAmount);

            var period = FormatPeriod(start, end);

            return new SalaryReportSample
            {
                TeacherName = teacherName,
                Period = period,
                BaseRatio = teacherRatio,
                PromotionCount = 0,
                PromotionRatio = 0,
                Rows = rows,
                TotalLessons = totalLessons,
                TotalHours = totalHours,
                TotalAmount = totalAmount
            };
        }

        private static DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var dt)) return dt;
            return null;
        }

        private static List<List<T>> Chunk<T>(IReadOnlyList<T> source, int size)
        {
            var result = new List<List<T>>();
            if (size <= 0 || source.Count == 0) return result;

            for (var i = 0; i < source.Count; i += size)
            {
                var sliceSize = Math.Min(size, source.Count - i);
                var segment = new List<T>(sliceSize);
                for (var j = 0; j < sliceSize; j++)
                {
                    segment.Add(source[i + j]);
                }
                result.Add(segment);
            }

            return result;
        }

        private static string FormatPeriod(DateTime start, DateTime end)
        {
            var startRocYear = start.Year - 1911;
            var endRocYear = end.Year - 1911;
            if (start.Year == end.Year && start.Month == end.Month)
                return $"{startRocYear:000}/{start.Month:00}";
            else
                return $"{startRocYear:000}/{start.Month:00}~{endRocYear:000}/{end.Month:00}";
        }

        private static IContainer LeftHeaderCell(IContainer container)
        {
            return container
                .BorderTop(1.2f)
                .BorderLeft(1.2f)
                .BorderBottom(1.2f)
                .BorderColor(Colors.Black)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .AlignCenter()
                .AlignMiddle();
        }
        private static IContainer RightHeaderCell(IContainer container)
        {
            return container
                .BorderTop(1.2f)
                .BorderRight(1.2f)
                .BorderBottom(1.2f)
                .BorderColor(Colors.Black)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .AlignCenter()
                .AlignMiddle();
        }
        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .BorderTop(1.2f)
                .BorderBottom(1.2f)
                .BorderColor(Colors.Black)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer LeftBodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderLeft(0.5f)
                .BorderColor(Colors.Grey.Medium)
                .PaddingVertical(3)
                .PaddingHorizontal(3)
                .AlignMiddle();
        }
        private static IContainer RightBodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderRight(0.5f)
                .BorderColor(Colors.Grey.Medium)
                .PaddingVertical(3)
                .PaddingHorizontal(3)
                .AlignMiddle();
        }
        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Medium)
                .PaddingVertical(3)
                .PaddingHorizontal(3)
                .AlignMiddle();
        }

        private record LessonItem(string Date, decimal Amount, decimal Hours);

        private record SalaryRow(
            string StudentName,
            string Instrument,
            List<LessonItem> Lessons,
            decimal? SpecialBonus = null
        )
        {
            public decimal TotalHours => Lessons.Sum(x => x.Hours);
            public decimal TotalAmount => Lessons.Sum(x => x.Amount) + (SpecialBonus ?? 0);
        }

        private class SalaryRowBuilder
        {
            public SalaryRowBuilder(string studentName, string instrument)
            {
                StudentName = studentName;
                Instrument = instrument;
            }

            public string StudentName { get; }
            public string Instrument { get; }
            public List<LessonItem> Lessons { get; } = new();

            public SalaryRow ToRow() => new(StudentName, Instrument, Lessons);
        }

        private class SalaryReportSample
        {
            public string TeacherName { get; set; } = string.Empty;
            public string Period { get; set; } = string.Empty;
            public decimal BaseRatio { get; set; }
            public int PromotionCount { get; set; }
            public decimal PromotionRatio { get; set; }
            public List<SalaryRow> Rows { get; set; } = new();
            public int TotalLessons { get; set; }
            public decimal TotalHours { get; set; }
            public decimal TotalAmount { get; set; }
        }

        private CompanyProfitData BuildCompanyProfitData(DateTime start, DateTime end, int teacherId, bool includePaid)
        {
            var query = _ctx.TblAttendance
                .Include(a => a.AttendanceFee)
                .Include(a => a.StudentPermission)!
                    .ThenInclude(sp => sp.User)
                .Include(a => a.StudentPermission)!
                    .ThenInclude(sp => sp.Teacher)!
                        .ThenInclude(sp => sp.TeacherSettlement)
                .Include(a => a.StudentPermission)!
                    .ThenInclude(sp => sp.Course)!
                        .ThenInclude(c => c.CourseFee)
                .Include(a => a.StudentPermission)!
                    .ThenInclude(sp => sp.StudentPermissionFees)!
                        .ThenInclude(spf => spf.Payment)
                .Where(a => !a.IsDelete && a.StudentPermission != null && a.StudentPermission.TeacherId == teacherId);

            var attendances = query.AsNoTracking().ToList();

            var teacherGroups = new Dictionary<int, TeacherProfitBuilder>();

            // 對每個 StudentPermission 的 Attendance 進行分組處理
            var attendancesByPermission = attendances
                .Where(a => ParseDate(a.AttendanceDate) != null)
                .GroupBy(a => a.StudentPermissionId)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.AttendanceDate).ToList());

            foreach (var att in attendances)
            {
                var attendanceDate = ParseDate(att.AttendanceDate);
                if (attendanceDate == null || attendanceDate < start || attendanceDate > end) continue;

                var sp = att.StudentPermission;
                if (sp == null || sp.IsDelete) continue;

                var permissions = _ctx.TblStudentPermission
                    .Where(sp1 => !sp1.IsDelete
                                 && sp1.UserId == sp.UserId
                                 && sp1.CourseId == sp.CourseId)
                    .Include(sp => sp.Course)                 // 課程名稱
                        .ThenInclude(c => c.CourseFee)        // 課程費用 + 教材費
                    .Include(sp => sp.StudentPermissionFees)  // 學生權限費用列表
                        .ThenInclude(spf => spf.Payment)       // 已收金額 & 結帳單號 (一對一)
                    .Include(sp => sp.Attendances)            // 課程一~四
                    .ToList();
                var combinedAttendances = permissions
                    .SelectMany(sp => sp.Attendances ?? new List<TblAttendance>())
                    .Where(a => !a.IsDelete)
                    .OrderBy(a => a.AttendanceDate)
                    .ToList();
                var combinedFees = permissions
                    .SelectMany(sp => sp.StudentPermissionFees ?? new List<TblStudentPermissionFee>())
                    .Where(spf => !spf.IsDelete)
                    .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
                    .ThenBy(spf => spf.Id)
                    .ToList();

                // 檢查是否有未刪除的學生權限費用記錄
                var hasStudentPermissionFee = sp.StudentPermissionFees?.Any(f => !f.IsDelete) ?? false;
                if (!hasStudentPermissionFee)
                {
                    continue;
                }

                // 找出當前 attendance 在該 StudentPermission 中的索引（依日期排序）
                var spAttendances = combinedAttendances;
                var attendanceIndex = spAttendances.FindIndex(a => a.Id == att.Id);
                if (attendanceIndex < 0) attendanceIndex = 0;

                // 依各筆費用的 Hours 動態計算所屬組別
                var sortedFees = combinedFees.Where(f => !f.IsDelete).OrderBy(f => f.Id).ToList() ?? new List<TblStudentPermissionFee>();
                int feeGroupIndex = 0;
                int accIndex = 0;
                for (int i = 0; i < sortedFees.Count; i++)
                {
                    int hoursPerFee = (int)Math.Max(1, Math.Ceiling(sortedFees[i].Hours != 0 ? (double)sortedFees[i].Hours : 4));
                    if (attendanceIndex < accIndex + hoursPerFee)
                    {
                        feeGroupIndex = i;
                        break;
                    }
                    accIndex += hoursPerFee;
                    feeGroupIndex = i + 1;
                }
                var correspondingFee = feeGroupIndex < sortedFees.Count ? sortedFees[feeGroupIndex] : null;

                if (!includePaid)
                {
                    // 檢查該組的 StudentPermissionFee 是否有繳款
                    var hasPaidFee = correspondingFee?.Payment != null && correspondingFee.Payment.Pay > 0;
                    if (!hasPaidFee) continue;
                }

                var tid = teacherId;
                if (!teacherGroups.TryGetValue(tid, out var builder))
                {
                    var teacher = sp.Teacher;
                    var teacherName = teacher?.DisplayName ?? teacher?.Username ?? $"T{tid:00000}";
                    builder = new TeacherProfitBuilder(tid, teacherName);
                    teacherGroups[tid] = builder;
                }

                var fee = att.AttendanceFee;
                var hours = fee?.Hours > 0 ? fee.Hours : 1m;
                var salaryAmount = (fee?.Amount ?? 0) + (fee?.AdjustmentAmount ?? 0);
                var sourceHoursTotalAmount = fee?.SourceHoursTotalAmount ?? 0m;

                // 計算學費欠費
                if (correspondingFee?.Payment == null)
                {
                    builder.AddArrears(att.Id, sourceHoursTotalAmount);
                }

                builder.AddLesson(sp.UserId, hours, salaryAmount, sourceHoursTotalAmount);
            }

            var rows = teacherGroups.Values
                .Select(b => b.ToRow())
                .OrderBy(r => r.TeacherName)
                .ToList();

            var totalStudents = rows.Sum(r => r.StudentCount);
            var totalLessons = rows.Sum(r => r.LessonCount);
            var totalLessonArrears = Ceil(rows.Sum(r => r.LessonArrears));
            var totalReceived = Ceil(rows.Sum(r => r.ReceivedAmount));
            var totalSplitSalary = Ceil(rows.Sum(r => r.SplitSalary));
            var totalAdvanceSalary = Ceil(rows.Sum(r => r.AdvanceSalary));
            var totalHealthInsurance = Ceil(rows.Sum(r => r.HealthInsurance));
            var totalDeposit = Ceil(rows.Sum(r => r.Deposit));
            var totalSalary = Ceil(rows.Sum(r => r.SalaryAmount));
            var totalAdvancedSalary = Ceil(rows.Sum(r => r.AdvancedSalary));
            var totalSupplementSalary = Ceil(rows.Sum(r => r.SupplementSalary));
            var totalProfit = Ceil(rows.Sum(r => r.Profit));

            var period = FormatPeriod(start, end);

            return new CompanyProfitData
            {
                Period = period,
                Rows = rows,
                TotalStudents = totalStudents,
                TotalLessons = totalLessons,
                TotalLessonArrears = totalLessonArrears,
                TotalReceived = totalReceived,
                TotalSplitSalary = totalSplitSalary,
                TotalAdvanceSalary = totalAdvanceSalary,
                TotalHealthInsurance = totalHealthInsurance,
                TotalDeposit = totalDeposit,
                TotalSalary = totalSalary,
                TotalAdvancedSalary = totalAdvancedSalary,
                TotalSupplementSalary = totalSupplementSalary,
                TotalProfit = totalProfit
            };
        }

        private static byte[] RenderCompanyProfitPdf(CompanyProfitData data)
        {
            var headers = new[]
            {
                "序號", "上課老師", "學生數", "堂數", "堂數欠費", "實收學費",
                "折帳薪資", "預支薪資", "二代健保", "保證金", "應付薪資",
                "代墊薪資", "補發薪資", "公司毛利", "%"
            };

            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Microsoft JhengHei").FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("VMS-POS").SemiBold().FontSize(12);
                            row.RelativeItem().AlignCenter().Text("私立樂光音樂短期補習班").Bold().FontSize(12);
                            row.RelativeItem().AlignRight().Text("Page:1").FontSize(10);
                        });

                        col.Item().AlignCenter().Text($"{data.Period} 個別班－公司獲利彙總表").Bold().FontSize(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(32);      // 序號
                                columns.RelativeColumn(1.5f);    // 上課老師
                                columns.RelativeColumn(0.7f);    // 學生數
                                columns.RelativeColumn(0.7f);    // 堂數
                                columns.RelativeColumn(0.9f);    // 堂數欠費
                                columns.RelativeColumn(1.0f);    // 實收學費
                                columns.RelativeColumn(1.0f);    // 折帳薪資
                                columns.RelativeColumn(0.9f);    // 預支薪資
                                columns.RelativeColumn(0.9f);    // 二代健保
                                columns.RelativeColumn(0.8f);    // 保證金
                                columns.RelativeColumn(1.0f);    // 應付薪資
                                columns.RelativeColumn(0.9f);    // 代墊薪資
                                columns.RelativeColumn(0.9f);    // 補發薪資
                                columns.RelativeColumn(1.0f);    // 公司毛利
                                columns.ConstantColumn(48);      // %
                            });

                            table.Header(header =>
                            {
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    var h = headers[i];
                                    var cellStyle = i == 0
                                        ? (Func<IContainer, IContainer>)LeftHeaderCell
                                        : (i == headers.Length - 1 ? RightHeaderCell : HeaderCell);
                                    header.Cell().Element(cellStyle).Text(h).SemiBold();
                                }
                            });

                            var index = 1;
                            foreach (var r in data.Rows)
                            {
                                table.Cell().Element(LeftBodyCell).Text(index.ToString());
                                table.Cell().Element(BodyCell).Text(r.TeacherName);
                                table.Cell().Element(BodyCell).Text(r.StudentCount.ToString());
                                table.Cell().Element(BodyCell).Text(r.LessonCount.ToString("0.00"));
                                table.Cell().Element(BodyCell).Text(r.LessonArrears.ToString("N0"));  // 堂數欠費
                                table.Cell().Element(BodyCell).Text(r.ReceivedAmount.ToString("N0"));
                                table.Cell().Element(BodyCell).Text(r.SplitSalary.ToString("N0"));    // 折帳薪資
                                table.Cell().Element(BodyCell).Text(r.AdvanceSalary.ToString("N0"));  // 預支薪資
                                table.Cell().Element(BodyCell).Text(r.HealthInsurance.ToString("N0")); // 二代健保
                                table.Cell().Element(BodyCell).Text(r.Deposit.ToString("N0"));        // 保證金
                                table.Cell().Element(BodyCell).Text(r.SalaryAmount.ToString("N0"));   // 應付薪資
                                table.Cell().Element(BodyCell).Text(r.AdvancedSalary.ToString("N0")); // 代墊薪資
                                table.Cell().Element(BodyCell).Text(r.SupplementSalary.ToString("N0")); // 補發薪資
                                table.Cell().Element(BodyCell).Text(r.Profit.ToString("N0"));
                                table.Cell().Element(RightBodyCell).Text($"{r.ProfitRate:0.00}%");
                                index++;
                            }

                            table.Cell().Element(LeftBodyCell).Text("合計:");
                            table.Cell().Element(BodyCell).Text("");
                            table.Cell().Element(BodyCell).Text(data.TotalStudents.ToString());
                            table.Cell().Element(BodyCell).Text(data.TotalLessons.ToString("0.00"));
                            table.Cell().Element(BodyCell).Text(data.TotalLessonArrears.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalReceived.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalSplitSalary.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalAdvanceSalary.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalHealthInsurance.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalDeposit.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalSalary.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalAdvancedSalary.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalSupplementSalary.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalProfit.ToString("N0"));
                            var totalRate = data.TotalReceived > 0 ? (data.TotalProfit / data.TotalReceived * 100) : 0;
                            table.Cell().Element(RightBodyCell).Text($"{totalRate:0.00}%");
                        });
                    });
                });
            }).GeneratePdf();
        }

        private class TeacherProfitBuilder
        {
            public int TeacherId { get; }
            public string TeacherName { get; }
            private readonly HashSet<int> _studentIds = new();
            private decimal _totalArrears;
            private decimal _totalHours;
            private decimal _totalSalary;            // 應付薪資合計（折帳薪資）
            private decimal _totalReceived;          // 依 min(拆帳比) 回算的實收學費合計

            public TeacherProfitBuilder(int teacherId, string teacherName)
            {
                TeacherId = teacherId;
                TeacherName = teacherName;
            }

            public void AddArrears(int attendanceId, decimal arrearsAmount)
            {
                _totalArrears += arrearsAmount;
            }

            public void AddLesson(int studentId, decimal hours, decimal salaryAmount, decimal sourceHoursTotalAmount)
            {
                _studentIds.Add(studentId);
                _totalHours += hours;
                _totalSalary += salaryAmount;

                // 使用 AttendanceFee 的 SourceHoursTotalAmount 作為實收學費
                _totalReceived += sourceHoursTotalAmount;
            }

            public ProfitRow ToRow()
            {
                var arrears = _totalArrears;
                var receivedAmount = _totalReceived;
                var profit = receivedAmount - _totalSalary; // 公司毛利 = 實收 - 應付薪資

                // 對外輸出採無條件進位
                var roundedArrears = Ceil(arrears);
                var roundedReceived = Ceil(receivedAmount);
                var roundedSalary = Ceil(_totalSalary);
                var roundedProfit = Ceil(profit);
                var roundedProfitRate = roundedReceived > 0 ? (roundedProfit / roundedReceived * 100) : 0;

                return new ProfitRow
                {
                    TeacherName = TeacherName,
                    StudentCount = _studentIds.Count,
                    LessonCount = _totalHours,
                    LessonArrears = roundedArrears,      // 堂數欠費
                    ReceivedAmount = roundedReceived,
                    SplitSalary = roundedSalary,   // 折帳薪資
                    AdvanceSalary = 0,            // 預支薪資（暂无数据）
                    HealthInsurance = 0,          // 二代健保（暂无数据）
                    Deposit = 0,                  // 保證金（暂无数据）
                    SalaryAmount = roundedSalary,  // 應付薪資
                    AdvancedSalary = 0,           // 代墊薪資（暂无数据）
                    SupplementSalary = 0,         // 補發薪資（暂无数据）
                    Profit = roundedProfit,
                    ProfitRate = roundedProfitRate
                };
            }
        }

        private static decimal Ceil(decimal value)
        {
            return Math.Ceiling(value);
        }

        private class ProfitRow
        {
            public string TeacherName { get; set; } = string.Empty;
            public int StudentCount { get; set; }
            public decimal LessonCount { get; set; }
            public decimal LessonArrears { get; set; }      // 堂數欠費
            public decimal ReceivedAmount { get; set; }
            public decimal SplitSalary { get; set; }        // 折帳薪資
            public decimal AdvanceSalary { get; set; }      // 預支薪資
            public decimal HealthInsurance { get; set; }    // 二代健保
            public decimal Deposit { get; set; }            // 保證金
            public decimal SalaryAmount { get; set; }       // 應付薪資
            public decimal AdvancedSalary { get; set; }     // 代墊薪資
            public decimal SupplementSalary { get; set; }   // 補發薪資
            public decimal Profit { get; set; }
            public decimal ProfitRate { get; set; }
        }

        private class CompanyProfitData
        {
            public string Period { get; set; } = string.Empty;
            public List<ProfitRow> Rows { get; set; } = new();
            public int TotalStudents { get; set; }
            public decimal TotalLessons { get; set; }
            public decimal TotalLessonArrears { get; set; }      // 堂數欠費
            public decimal TotalReceived { get; set; }
            public decimal TotalSplitSalary { get; set; }        // 折帳薪資
            public decimal TotalAdvanceSalary { get; set; }      // 預支薪資
            public decimal TotalHealthInsurance { get; set; }    // 二代健保
            public decimal TotalDeposit { get; set; }            // 保證金
            public decimal TotalSalary { get; set; }             // 應付薪資
            public decimal TotalAdvancedSalary { get; set; }     // 代墊薪資
            public decimal TotalSupplementSalary { get; set; }   // 補發薪資
            public decimal TotalProfit { get; set; }
        }
    }
}
