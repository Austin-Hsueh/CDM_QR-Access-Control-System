using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DoorDB;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

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
                                table.Cell().Element(BodyCell).Text(index.ToString());
                                table.Cell().Element(BodyCell).Text(row.StudentName);
                                table.Cell().Element(BodyCell).Text(row.Instrument);

                                for (var i = 0; i < 8; i++)
                                {
                                    var lesson = i < row.Lessons.Count ? row.Lessons[i] : null;
                                    var text = lesson == null ? string.Empty : $"{lesson.Date}\n{lesson.Amount:0}";
                                    table.Cell().Element(BodyCell).Text(text);
                                }

                                var special = row.SpecialBonus.HasValue ? $"{row.SpecialBonus:0}" : string.Empty;
                                table.Cell().Element(BodyCell).Text(special);

                                var totalText = $"{row.Lessons.Count} ({row.TotalHours:0.0}H)\n${row.TotalAmount:N0}";
                                table.Cell().Element(BodyCell).Text(totalText);

                                index++;
                            }
                        });

                        col.Item().PaddingTop(8).Text($"老師基本折帳比: {sample.BaseRatio:0.00}    跳級人數: {sample.PromotionCount}    跳級折帳比: {sample.PromotionRatio:0.00}    小計: ${sample.TotalAmount:N0}");

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

                // 檢查是否有未刪除的學生權限費用記錄
                var hasStudentPermissionFee = sp.StudentPermissionFees?.Any(f => !f.IsDelete) ?? false;
                if (!hasStudentPermissionFee)
                {
                    continue;
                }

                // 找出當前 attendance 在該 StudentPermission 中的索引（依日期排序）
                var spAttendances = attendancesByPermission.GetValueOrDefault(att.StudentPermissionId) ?? new List<TblAttendance>();
                var attendanceIndex = spAttendances.FindIndex(a => a.Id == att.Id);
                if (attendanceIndex < 0) attendanceIndex = 0;

                // 計算屬於第幾組（每4筆一組）
                int feeGroupIndex = attendanceIndex / 4;

                // 取得該組對應的 StudentPermissionFee（排除已刪除的記錄）
                var sortedFees = sp.StudentPermissionFees?.Where(f => !f.IsDelete).OrderBy(f => f.Id).ToList() ?? new List<TblStudentPermissionFee>();
                var correspondingFee = feeGroupIndex < sortedFees.Count ? sortedFees[feeGroupIndex] : null;

                if (!includePaid)
                {
                    // 檢查該組的 StudentPermissionFee 是否有繳款
                    var hasPaidFee = correspondingFee?.Payment != null && correspondingFee.Payment.Pay > 0;
                    if (!hasPaidFee) continue;
                }

                var user = sp.User;
                var studentName = user?.DisplayName ?? user?.Username ?? $"學生 {sp.UserId}";
                var instrument = sp.Course?.CourseFee?.FeeCode ?? sp.Course?.Name ?? "-";

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

        private static string FormatPeriod(DateTime start, DateTime end)
        {
            var startRocYear = start.Year - 1911;
            var endRocYear = end.Year - 1911;
            if (start.Year == end.Year && start.Month == end.Month)
                return $"{startRocYear:000}/{start.Month:00}";
            else
                return $"{startRocYear:000}/{start.Month:00}~{endRocYear:000}/{end.Month:00}";
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container.Border(0.5f).Padding(4).AlignCenter();
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container.Border(0.5f).Padding(4).AlignCenter();
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

                // 檢查是否有未刪除的學生權限費用記錄
                var hasStudentPermissionFee = sp.StudentPermissionFees?.Any(f => !f.IsDelete) ?? false;
                if (!hasStudentPermissionFee)
                {
                    continue;
                }

                // 找出當前 attendance 在該 StudentPermission 中的索引（依日期排序）
                var spAttendances = attendancesByPermission.GetValueOrDefault(att.StudentPermissionId) ?? new List<TblAttendance>();
                var attendanceIndex = spAttendances.FindIndex(a => a.Id == att.Id);
                if (attendanceIndex < 0) attendanceIndex = 0;

                // 計算屬於第幾組（每4筆一組）
                int feeGroupIndex = attendanceIndex / 4;

                // 取得該組對應的 StudentPermissionFee（排除已刪除的記錄）
                var sortedFees = sp.StudentPermissionFees?.Where(f => !f.IsDelete).OrderBy(f => f.Id).ToList() ?? new List<TblStudentPermissionFee>();
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

                // 使用對應組的 StudentPermissionFee 判斷是否有付款
                var spHasPayment = correspondingFee?.Payment != null && correspondingFee.Payment.Pay > 0;

                // 每筆課程的欠費只計算一次
                var receivableTotal = (sp.Course?.CourseFee?.Amount ?? 0) + (sp.Course?.CourseFee?.MaterialFee ?? 0);
                var receivedTotal = sp.StudentPermissionFees?
                    .Where(spf => spf.Payment != null && !spf.IsDelete)
                    .Sum(spf => (spf.Payment!.Pay) + (spf.Payment!.DiscountAmount)) ?? 0;
                var arrearsAmount = Math.Max(receivableTotal - receivedTotal, 0);
                builder.AddArrears(sp.Id, arrearsAmount);

                builder.AddLesson(sp.UserId, hours, salaryAmount, sourceHoursTotalAmount, spHasPayment);
            }

            var rows = teacherGroups.Values
                .Select(b => b.ToRow())
                .OrderBy(r => r.TeacherName)
                .ToList();

            var totalStudents = rows.Sum(r => r.StudentCount);
            var totalLessons = rows.Sum(r => r.LessonCount);
            var totalArrears = rows.Sum(r => r.Arrears);
            var totalReceived = rows.Sum(r => r.ReceivedAmount);
            var totalSalary = rows.Sum(r => r.SalaryAmount);
            var totalPaidSalary = rows.Sum(r => r.PaidSalary);
            var totalSupplementSalary = rows.Sum(r => r.SupplementSalary);
            var totalProfit = rows.Sum(r => r.Profit);

            var period = FormatPeriod(start, end);

            return new CompanyProfitData
            {
                Period = period,
                Rows = rows,
                TotalStudents = totalStudents,
                TotalLessons = totalLessons,
                TotalArrears = totalArrears,
                TotalReceived = totalReceived,
                TotalSalary = totalSalary,
                TotalPaidSalary = totalPaidSalary,
                TotalSupplementSalary = totalSupplementSalary,
                TotalProfit = totalProfit
            };
        }

        private static byte[] RenderCompanyProfitPdf(CompanyProfitData data)
        {
            var headers = new[]
            {
                "序號", "上課老師", "學生數", "堂數", "學費欠費",
                "實收學費", "折帳薪資", "應付薪資", "補發薪資", "公司毛利", "%"
            };

            return Document.Create(doc =>
            {
                doc.Page(page =>
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
                            row.RelativeItem().AlignRight().Text("Page:1").FontSize(10);
                        });

                        col.Item().AlignCenter().Text($"{data.Period} 個別班－公司獲利彙總表").Bold().FontSize(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(36);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(0.9f);
                                columns.RelativeColumn(0.9f);
                                columns.RelativeColumn(1.0f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.ConstantColumn(48);
                            });

                            table.Header(header =>
                            {
                                foreach (var h in headers)
                                    header.Cell().Element(HeaderCell).Text(h).SemiBold();
                            });

                            var index = 1;
                            foreach (var r in data.Rows)
                            {
                                table.Cell().Element(BodyCell).Text(index.ToString());
                                table.Cell().Element(BodyCell).Text(r.TeacherName);
                                table.Cell().Element(BodyCell).Text(r.StudentCount.ToString());
                                table.Cell().Element(BodyCell).Text(r.LessonCount.ToString("0.00"));
                                table.Cell().Element(BodyCell).Text(r.Arrears.ToString("N0"));
                                table.Cell().Element(BodyCell).Text(r.ReceivedAmount.ToString("N0"));
                                table.Cell().Element(BodyCell).Text(r.SalaryAmount.ToString("N0"));
                                table.Cell().Element(BodyCell).Text(r.PaidSalary.ToString("N0"));
                                table.Cell().Element(BodyCell).Text(r.SupplementSalary.ToString("N0"));
                                table.Cell().Element(BodyCell).Text(r.Profit.ToString("N0"));
                                table.Cell().Element(BodyCell).Text($"{r.ProfitRate:0.00}%");
                                index++;
                            }

                            table.Cell().Element(BodyCell).Text("合計:");
                            table.Cell().Element(BodyCell).Text("");
                            table.Cell().Element(BodyCell).Text(data.TotalStudents.ToString());
                            table.Cell().Element(BodyCell).Text(data.TotalLessons.ToString("0.00"));
                            table.Cell().Element(BodyCell).Text(data.TotalArrears.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalReceived.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalSalary.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalPaidSalary.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalSupplementSalary.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(data.TotalProfit.ToString("N0"));
                            var totalRate = data.TotalReceived > 0 ? (data.TotalProfit / data.TotalReceived * 100) : 0;
                            table.Cell().Element(BodyCell).Text($"{totalRate:0.00}%");
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
            private readonly HashSet<int> _arrearsSpIds = new();
            private decimal _totalHours;
            private decimal _totalSalary;            // 折帳薪資合計（全部）
            private decimal _totalReceived;          // 依 min(拆帳比) 回算的實收學費合計
            private decimal _paidSalary;             // 有 StudentPermissionFee 的薪資合計
            private decimal _supplementSalary;       // 無 StudentPermissionFee 的薪資合計
            private decimal _arrears;                // 學費欠費合計

            public TeacherProfitBuilder(int teacherId, string teacherName)
            {
                TeacherId = teacherId;
                TeacherName = teacherName;
            }

            public void AddArrears(int studentPermissionId, decimal arrearsAmount)
            {
                if (arrearsAmount <= 0) return;
                if (_arrearsSpIds.Add(studentPermissionId))
                    _arrears += arrearsAmount;
            }

            public void AddLesson(int studentId, decimal hours, decimal salaryAmount, decimal sourceHoursTotalAmount, bool hasFeePayment)
            {
                _studentIds.Add(studentId);
                _totalHours += hours;
                _totalSalary += salaryAmount;

                // 使用 AttendanceFee 的 SourceHoursTotalAmount 作為實收學費
                _totalReceived += sourceHoursTotalAmount;

                if (hasFeePayment)
                    _paidSalary += salaryAmount;
                else
                    _supplementSalary += salaryAmount;
            }

            public ProfitRow ToRow()
            {
                var receivedAmount = _totalReceived;
                var profit = receivedAmount - _totalSalary; // 公司毛利 = 實收 - 拆帳（薪資）
                var profitRate = receivedAmount > 0 ? (profit / receivedAmount * 100) : 0;

                return new ProfitRow
                {
                    TeacherName = TeacherName,
                    StudentCount = _studentIds.Count,
                    LessonCount = _totalHours,
                    Arrears = _arrears,
                    ReceivedAmount = receivedAmount,
                    SalaryAmount = _totalSalary,
                    PaidSalary = _paidSalary,
                    SupplementSalary = _supplementSalary,
                    Profit = profit,
                    ProfitRate = profitRate
                };
            }
        }

        private class ProfitRow
        {
            public string TeacherName { get; set; } = string.Empty;
            public int StudentCount { get; set; }
            public decimal LessonCount { get; set; }
            public decimal Arrears { get; set; }
            public decimal ReceivedAmount { get; set; }
            public decimal SalaryAmount { get; set; }
            public decimal PaidSalary { get; set; }
            public decimal SupplementSalary { get; set; }
            public decimal Profit { get; set; }
            public decimal ProfitRate { get; set; }
        }

        private class CompanyProfitData
        {
            public string Period { get; set; } = string.Empty;
            public List<ProfitRow> Rows { get; set; } = new();
            public int TotalStudents { get; set; }
            public decimal TotalLessons { get; set; }
            public decimal TotalArrears { get; set; }
            public decimal TotalReceived { get; set; }
            public decimal TotalSalary { get; set; }
            public decimal TotalPaidSalary { get; set; }
            public decimal TotalSupplementSalary { get; set; }
            public decimal TotalProfit { get; set; }
        }
    }
}
