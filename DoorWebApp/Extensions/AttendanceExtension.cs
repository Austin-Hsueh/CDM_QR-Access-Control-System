using DoorDB;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DoorWebApp.Extensions
{
    /// <summary>
    /// TblAttendance 相關的擴展方法
    /// </summary>
    public static class AttendanceExtension
    {
        /// <summary>
        /// 根據 Attendance 找到對應的 StudentPermissionFee
        /// 依「相同學生 + 相同課程」分組查詢，取得該組對應的費用記錄
        /// </summary>
        /// <param name="attendance">出席記錄</param>
        /// <param name="ctx">資料庫上下文</param>
        /// <returns>對應的 StudentPermissionFee，若無則回傳 null</returns>
        public static async Task<TblStudentPermissionFee?> GetCorrespondingStudentPermissionFeeAsync(
            this TblAttendance attendance, 
            DoorDbContext ctx)
        {
            if (attendance == null || attendance.StudentPermissionId == null)
                return null;

            // 載入 StudentPermission (如果尚未載入)
            if (attendance.StudentPermission == null)
            {
                await ctx.Entry(attendance)
                    .Reference(a => a.StudentPermission)
                    .LoadAsync();
            }

            var studentPermission = attendance.StudentPermission;
            if (studentPermission == null)
                return null;

            // 1. 依「相同學生 + 相同課程」分組查詢所有學生權限
            var sameGroupPermissions = await ctx.TblStudentPermission
                .Where(sp => !sp.IsDelete
                    && sp.UserId == studentPermission.UserId
                    && sp.CourseId == studentPermission.CourseId)
                .Select(sp => sp.Id)
                .ToListAsync();

            if (!sameGroupPermissions.Any())
                return null;

            // 2. 合併「相同學生 + 相同課程」的簽到與費用，統一分組
            // 取得所有該組的出席記錄（按日期排序）
            var combinedAttendances = await ctx.TblAttendance
                .Where(a => !a.IsDelete 
                    && sameGroupPermissions.Contains(a.StudentPermissionId))
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            // 3. 取得該組對應的 StudentPermissionFee（排除已刪除的記錄，按繳款日期排序）
            var combinedFees = await ctx.TblStudentPermissionFee
                .Where(spf => !spf.IsDelete 
                    && sameGroupPermissions.Contains(spf.StudentPermissionId))
                .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
                .ThenBy(spf => spf.Id)
                .ToListAsync();

            if (!combinedFees.Any())
                return null;

            // 4. 找出當前 attendance 在 combinedAttendances 中的索引位置
            int attendanceIndex = combinedAttendances.FindIndex(a => a.Id == attendance.Id);
            if (attendanceIndex == -1)
                return null;

            // 5. 根據每筆費用的 Hours 動態切分出席記錄，找到對應的費用
            int currentStart = 0;
            foreach (var fee in combinedFees)
            {
                int hours = fee.Hours > 0 ? (int)fee.Hours : 4; // 防止 Hours=0
                int currentEnd = currentStart + hours;

                if (attendanceIndex >= currentStart && attendanceIndex < currentEnd)
                {
                    return fee;
                }

                currentStart = currentEnd;
            }

            // 找不到對應費用
            return null;
        }

        /// <summary>
        /// 根據 StudentPermission 找到對應組別中最早的還沒塞滿 4 個出席記錄的 StudentPermissionFee
        /// 依「相同學生 + 相同課程」分組查詢
        /// </summary>
        /// <param name="studentPermission">學生權限</param>
        /// <param name="ctx">資料庫上下文</param>
        /// <returns>還沒塞滿的費用記錄，若無則回傳 null</returns>
        public static async Task<TblStudentPermissionFee?> GetFirstAvailableStudentPermissionFeeAsync(
            this TblStudentPermission studentPermission,
            DoorDbContext ctx)
        {
            if (studentPermission == null)
                return null;

            // 1. 依「相同學生 + 相同課程」分組查詢所有學生權限
            var sameGroupPermissions = await ctx.TblStudentPermission
                .Where(sp => !sp.IsDelete
                    && sp.UserId == studentPermission.UserId
                    && sp.CourseId == studentPermission.CourseId)
                .Select(sp => sp.Id)
                .ToListAsync();

            if (!sameGroupPermissions.Any())
                return null;

            // 2. 取得該組對應的 StudentPermissionFee（按繳款日期排序，從最早的開始）
            var combinedFees = await ctx.TblStudentPermissionFee
                .Where(spf => !spf.IsDelete
                    && sameGroupPermissions.Contains(spf.StudentPermissionId))
                .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
                .ThenBy(spf => spf.Id)
                .ToListAsync();

            // 3. 取得所有該組的出席記錄（按日期排序）
            var combinedAttendances = await ctx.TblAttendance
                .Where(a => !a.IsDelete
                    && sameGroupPermissions.Contains(a.StudentPermissionId))
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            // 4. 檢查每個費用，找出第一個還沒塞滿 Hours 的出席記錄的
            if (combinedFees.Any())
            {
                int currentStart = 0;
                foreach (var fee in combinedFees)
                {
                    int hours = fee.Hours > 0 ? (int)fee.Hours : 4; // 防止 Hours=0
                    int currentEnd = currentStart + hours;

                    // 計算這個費用已經有多少個出席記錄
                    int attendanceCount = 0;
                    for (int i = currentStart; i < currentEnd && i < combinedAttendances.Count; i++)
                    {
                        attendanceCount++;
                    }

                    // 如果還沒滿，回傳這個費用
                    if (attendanceCount < hours)
                    {
                        return fee;
                    }

                    currentStart = currentEnd;
                }
            }

            // 5. 所有費用都已滿，建立新的 StudentPermissionFee
            // 讀取課程費用資訊
            var courseFee = await ctx.TblCourseFee
                .Where(cf => cf.CourseId == studentPermission.CourseId)
                .FirstOrDefaultAsync();

            if (courseFee == null)
                return null;

            // 計算新費用的金額
            int tuitionFee = courseFee.Amount;
            int materialFee = courseFee.MaterialFee;
            int totalAmount = tuitionFee + materialFee;

            // 建立新的 StudentPermissionFee
            var newFee = new TblStudentPermissionFee
            {
                StudentPermissionId = studentPermission.Id,
                TotalAmount = totalAmount,
                Hours = courseFee.Hours,  // 設置課程時數
                TeacherSplitRatio = null, // 會在建立時決定
                CourseSplitRatio = courseFee.SplitRatio,
                PaymentDate = null, // 根據需求設為 null
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
                IsDelete = false
            };

            ctx.TblStudentPermissionFee.Add(newFee);
            await ctx.SaveChangesAsync();

            return newFee;
        }
    }

    /// <summary>
    /// Attendance 位置資訊
    /// </summary>
    public class AttendancePositionInfo
    {
        /// <summary>
        /// Attendance 在同組中的索引（從 0 開始）
        /// </summary>
        public int AttendanceIndex { get; set; }

        /// <summary>
        /// 對應的費用索引（從 0 開始）
        /// </summary>
        public int FeeIndex { get; set; }

        /// <summary>
        /// 在該費用中的位置（1-based）
        /// </summary>
        public int PositionInFee { get; set; }

        /// <summary>
        /// 該組的總出席數
        /// </summary>
        public int TotalAttendances { get; set; }

        /// <summary>
        /// 該組的總費用數
        /// </summary>
        public int TotalFees { get; set; }

        /// <summary>
        /// 對應的費用記錄
        /// </summary>
        public TblStudentPermissionFee? CorrespondingFee { get; set; }

        /// <summary>
        /// 是否為該費用的最後一次出席（依 Hours 判定）
        /// </summary>
        public bool IsLastAttendanceOfFee => CorrespondingFee != null && PositionInFee == (int)(CorrespondingFee.Hours > 0 ? CorrespondingFee.Hours : 4);

        /// <summary>
        /// 是否為該費用的第一次出席
        /// </summary>
        public bool IsFirstAttendanceOfFee => PositionInFee == 1;
    }

    /// <summary>
    /// 費用的出席記錄填充狀態
    /// </summary>
    public class FeeAttendanceStatus
    {
        /// <summary>
        /// 費用記錄
        /// </summary>
        public TblStudentPermissionFee Fee { get; set; } = null!;

        /// <summary>
        /// 費用索引（從 0 開始）
        /// </summary>
        public int FeeIndex { get; set; }

        /// <summary>
        /// 已有的出席記錄數量
        /// </summary>
        public int AttendanceCount { get; set; }

        /// <summary>
        /// 是否已滿（依 Hours 判斷）
        /// </summary>
        public bool IsFull { get; set; }

        /// <summary>
        /// 剩餘可填充的出席記錄數量（依 Hours 判斷）
        /// </summary>
        public int RemainingSlots { get; set; }

        /// <summary>
        /// 該費用對應的所有出席記錄
        /// </summary>
        public List<TblAttendance> Attendances { get; set; } = new List<TblAttendance>();

        /// <summary>
        /// 填充進度百分比（0-100，依 Hours 計算）
        /// </summary>
        public decimal ProgressPercentage => Fee != null && Fee.Hours > 0 ? (decimal)AttendanceCount / Fee.Hours * 100 : 0;

        /// <summary>
        /// 填充進度文字（例如：3/4，依 Hours 計算）
        /// </summary>
        public string ProgressText => Fee != null && Fee.Hours > 0 ? $"{AttendanceCount}/{Fee.Hours}" : $"{AttendanceCount}/0";
    }
}
