using System.ComponentModel;
using DoorMcpServer.DoorApi;
using McpKit;
using ModelContextProtocol.Server;

namespace DoorMcpServer.Tools;

[McpServerToolType]
public sealed class FinanceTools(DoorApiClient api, ToolJson json)
{
    [McpServerTool(Name = "get_student_payments", Title = "Get a student's payment records", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadFinance)]
    [Description("All payment records of one student across all courses, newest first. Only periods that HAVE a payment appear here; " +
                 "to find unpaid periods use get_student_attendance (is_paid = false). Amounts are in TWD.")]
    public async Task<string> GetStudentPayments(
        [Description("The student's user_id.")] int student_id,
        [Description("Also include voided (soft-deleted) payments. Default false.")] bool include_voided = false,
        [Description("1-based page number. Default 1.")] int? page = null,
        [Description("Items per page, 1-100. Default 20.")] int? page_size = null,
        CancellationToken cancellationToken = default)
    {
        if (student_id <= 0) throw ToolException.InvalidArgument("student_id must be a positive integer.");

        var rows = await api.GetAsync<List<WirePaymentRecord>>($"api/v1/StudentPayment/ByStudent/{student_id}", cancellationToken) ?? [];

        var items = rows
            .Where(r => include_voided || !r.IsDelete)
            .Select(r => new
            {
                r.PaymentId,
                FeePeriodId = r.StudentPermissionFeeId,
                PermissionId = r.StudentPermissionId,
                r.CourseName,
                r.TeacherName,
                DueDate = DoorConventions.NormalizeDate(r.PaymentDate),
                PayDate = DoorConventions.NormalizeDate(r.PayDate),
                AmountPaid = r.Pay,
                r.DiscountAmount,
                ReceiptNumber = DoorConventions.NullIfBlank(r.ReceiptNumber),
                Remark = DoorConventions.NullIfBlank(r.Remark),
                IsVoided = r.IsDelete ? true : (bool?)null,
                // 後端這欄其實是門禁時段的起訖日（"yyyy/MM/dd~yyyy/MM/dd"），不是單一上課日
                EnrolmentDateRange = DoorConventions.NullIfBlank(r.ScheduleDate?.Replace('/', '-')),
                LessonTime = string.IsNullOrWhiteSpace(r.StartTime) ? null : $"{r.StartTime}-{r.EndTime}",
                ClassroomName = DoorConventions.NullIfBlank(r.ClassroomName),
            })
            .ToList();

        return json.Serialize(Paging.Slice(items, page, page_size));
    }

    [McpServerTool(Name = "get_fee_period_summary", Title = "Get one fee period's billing summary", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadFinance)]
    [Description("Billing summary of one paid period (fee period): list price, discount, receivable, paid, outstanding, receipt number, " +
                 "and the refund if one was made. fee_period_id comes from get_student_attendance or get_student_payments. Amounts are in TWD.")]
    public async Task<string> GetFeePeriodSummary(
        [Description("The fee period id (fee_period_id).")] int fee_period_id,
        CancellationToken cancellationToken = default)
    {
        if (fee_period_id <= 0) throw ToolException.InvalidArgument("fee_period_id must be a positive integer.");

        // 退費端點的回應是繳費摘要的超集合，一次呼叫就夠
        var s = await api.GetAsync<WireFeeSummary>($"api/v1/StudentRefund/{fee_period_id}", cancellationToken)
                ?? throw ToolException.NotFound($"No fee period with fee_period_id {fee_period_id}.");

        return json.Serialize(new
        {
            FeePeriodId = s.StudentPermissionFeeId,
            PermissionId = s.StudentPermissionId,
            s.StudentId,
            StudentName = DoorConventions.NullIfBlank(s.StudentName),
            CourseName = DoorConventions.NullIfBlank(s.CourseName),
            ListAmount = s.CurrentAmount,
            DiscountAmount = s.TotalDiscount,
            s.ReceivableAmount,
            s.PaidAmount,
            s.OutstandingAmount,
            PayDate = DoorConventions.NormalizeDate(s.PayDate),
            ReceiptNumber = DoorConventions.NullIfBlank(s.ReceiptNumber),
            Remark = DoorConventions.NullIfBlank(s.Remark),
            Refund = s.RefundId is null
                ? null
                : new
                {
                    s.RefundId,
                    RefundDate = DoorConventions.NormalizeDate(s.RefundDate),
                    s.RefundAmount,
                    ReceiptNumber = DoorConventions.NullIfBlank(s.RefundReceiptNumber),
                    Remark = DoorConventions.NullIfBlank(s.RefundRemark),
                },
        });
    }

    [McpServerTool(Name = "get_daily_checkin_status", Title = "Get a day's sign-in status", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadSchedule)]
    [Description("For one day: every scheduled student lesson and whether attendance has been recorded for it, plus whether the day " +
                 "can be closed (all lessons recorded). A lesson counts as recorded when ANY attendance record exists — check " +
                 "attendance_type (present / absent / leave) for what was recorded.")]
    public async Task<string> GetDailyCheckinStatus(
        [Description("The day, YYYY-MM-DD.")] string date,
        CancellationToken cancellationToken = default)
    {
        // 後端用原始字串比對簽到日期，所以一定要送 yyyy-MM-dd
        var day = DoorConventions.ToIso(DoorConventions.ParseDate(date, nameof(date)));
        var wire = await api.GetAsync<WireDailyStatus>($"api/v1/CloseAccount/DailyStatus/{day}", cancellationToken)
                   ?? throw new ToolException("backend_error", "The backend returned no data.");

        return json.Serialize(new
        {
            Date = day,
            TotalLessons = wire.TotalSchedules,
            RecordedCount = wire.CheckedInCount,
            NotRecordedCount = wire.NotCheckedInCount,
            wire.CanCloseAccount,
            Lessons = (wire.ScheduleStatuses ?? [])
                .OrderBy(s => s.StartTime, StringComparer.Ordinal)
                .Select(s => new
                {
                    s.ScheduleId,
                    PermissionId = s.StudentPermissionId,
                    s.StudentId,
                    StudentCode = DoorConventions.NullIfBlank(s.Username),
                    s.StudentName,
                    CourseName = DoorConventions.NullIfBlank(s.CourseName),
                    ClassroomName = DoorConventions.NullIfBlank(s.ClassroomName),
                    s.StartTime,
                    s.EndTime,
                    AttendanceRecorded = s.AttendanceId is not null,
                    AttendanceType = DoorConventions.AttendanceType(s.AttendanceType),
                    s.AttendanceId,
                    RecordedAt = s.CheckedInTime?.ToString("yyyy-MM-dd HH:mm"),
                })
                .ToList(),
        });
    }

    [McpServerTool(Name = "get_close_account", Title = "Get a day's closing figures", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadFinance)]
    [Description("Daily account-closing figures for one day: yesterday's petty cash, business income (payments minus refunds), " +
                 "closing amount, deposit and petty-cash balance. If the day has not been closed yet, is_closed is false and the figures " +
                 "are a live preview (deposit and petty_cash_balance are 0). Amounts are in TWD.")]
    public async Task<string> GetCloseAccount(
        [Description("The day, YYYY-MM-DD.")] string date,
        CancellationToken cancellationToken = default)
    {
        var day = DoorConventions.ToIso(DoorConventions.ParseDate(date, nameof(date)));
        var wire = await api.GetAsync<WireCloseAccount>($"api/v1/CloseAccount/Detail/{day}", cancellationToken)
                   ?? throw new ToolException("backend_error", "The backend returned no data.");

        return json.Serialize(CloseAccountDto.From(wire));
    }

    [McpServerTool(Name = "list_close_accounts", Title = "List closed days", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [RequireScope(Scopes.ReadFinance)]
    [Description("List the days that HAVE been closed within a date range, newest first, with their closing figures. " +
                 "Days missing from the list were not closed. Defaults to the last 30 days. Amounts are in TWD.")]
    public async Task<string> ListCloseAccounts(
        [Description("First day, YYYY-MM-DD (inclusive). Omit for 30 days ago.")] string? date_from = null,
        [Description("Last day, YYYY-MM-DD (inclusive). Omit for today.")] string? date_to = null,
        [Description("1-based page number. Default 1.")] int? page = null,
        [Description("Items per page, 1-100. Default 31.")] int? page_size = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(date_from)) query.Add("startDate=" + DoorConventions.ToIso(DoorConventions.ParseDate(date_from, nameof(date_from))));
        if (!string.IsNullOrWhiteSpace(date_to)) query.Add("endDate=" + DoorConventions.ToIso(DoorConventions.ParseDate(date_to, nameof(date_to))));

        var path = "api/v1/CloseAccount" + (query.Count > 0 ? "?" + string.Join('&', query) : "");
        var rows = await api.GetAsync<List<WireCloseAccount>>(path, cancellationToken) ?? [];

        return json.Serialize(Paging.Slice(rows.Select(CloseAccountDto.From).ToList(), page, page_size ?? 31));
    }

    public sealed class CloseAccountDto
    {
        public string? Date { get; init; }
        public bool IsClosed { get; init; }
        public int YesterdayPettyCash { get; init; }
        public int BusinessIncome { get; init; }
        public int ClosingAmount { get; init; }
        public int DepositAmount { get; init; }
        public int PettyCashBalance { get; init; }

        public static CloseAccountDto From(WireCloseAccount c) => new()
        {
            Date = DoorConventions.NormalizeDate(c.CloseDate),
            IsClosed = c.Id != 0, // 後端對未關帳的日期回傳一筆未存檔、Id = 0 的試算資料
            YesterdayPettyCash = c.YesterdayPettyIncome,
            BusinessIncome = c.BusinessIncome,
            ClosingAmount = c.CloseAccountAmount,
            DepositAmount = c.DepositAmount,
            PettyCashBalance = c.PettyIncome,
        };
    }
}
