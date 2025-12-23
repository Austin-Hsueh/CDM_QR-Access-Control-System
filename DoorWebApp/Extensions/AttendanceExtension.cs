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

            // 5. 根據索引計算對應的費用（每 4 筆出席對應 1 筆費用）
            int feeIndex = attendanceIndex / 4;

            // 6. 回傳對應的費用記錄
            return feeIndex < combinedFees.Count ? combinedFees[feeIndex] : null;
        }

        /// <summary>
        /// 根據 Attendance 找到對應的 StudentPermissionFee (同步版本)
        /// 依「相同學生 + 相同課程」分組查詢，取得該組對應的費用記錄
        /// </summary>
        /// <param name="attendance">出席記錄</param>
        /// <param name="ctx">資料庫上下文</param>
        /// <returns>對應的 StudentPermissionFee，若無則回傳 null</returns>
        public static TblStudentPermissionFee? GetCorrespondingStudentPermissionFee(
            this TblAttendance attendance,
            DoorDbContext ctx)
        {
            if (attendance == null || attendance.StudentPermissionId == null)
                return null;

            // 載入 StudentPermission (如果尚未載入)
            if (attendance.StudentPermission == null)
            {
                ctx.Entry(attendance)
                    .Reference(a => a.StudentPermission)
                    .Load();
            }

            var studentPermission = attendance.StudentPermission;
            if (studentPermission == null)
                return null;

            // 1. 依「相同學生 + 相同課程」分組查詢所有學生權限
            var sameGroupPermissions = ctx.TblStudentPermission
                .Where(sp => !sp.IsDelete
                    && sp.UserId == studentPermission.UserId
                    && sp.CourseId == studentPermission.CourseId)
                .Select(sp => sp.Id)
                .ToList();

            if (!sameGroupPermissions.Any())
                return null;

            // 2. 合併「相同學生 + 相同課程」的簽到與費用，統一分組
            // 取得所有該組的出席記錄（按日期排序）
            var combinedAttendances = ctx.TblAttendance
                .Where(a => !a.IsDelete
                    && sameGroupPermissions.Contains(a.StudentPermissionId))
                .OrderBy(a => a.AttendanceDate)
                .ToList();

            // 3. 取得該組對應的 StudentPermissionFee（排除已刪除的記錄，按繳款日期排序）
            var combinedFees = ctx.TblStudentPermissionFee
                .Where(spf => !spf.IsDelete
                    && sameGroupPermissions.Contains(spf.StudentPermissionId))
                .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
                .ThenBy(spf => spf.Id)
                .ToList();

            if (!combinedFees.Any())
                return null;

            // 4. 找出當前 attendance 在 combinedAttendances 中的索引位置
            int attendanceIndex = combinedAttendances.FindIndex(a => a.Id == attendance.Id);
            if (attendanceIndex == -1)
                return null;

            // 5. 根據索引計算對應的費用（每 4 筆出席對應 1 筆費用）
            int feeIndex = attendanceIndex / 4;

            // 6. 回傳對應的費用記錄
            return feeIndex < combinedFees.Count ? combinedFees[feeIndex] : null;
        }

        /// <summary>
        /// 取得 Attendance 在同組中的位置資訊
        /// </summary>
        /// <param name="attendance">出席記錄</param>
        /// <param name="ctx">資料庫上下文</param>
        /// <returns>包含位置資訊的物件</returns>
        public static async Task<AttendancePositionInfo?> GetAttendancePositionInfoAsync(
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

            // 依「相同學生 + 相同課程」分組查詢所有學生權限
            var sameGroupPermissions = await ctx.TblStudentPermission
                .Where(sp => !sp.IsDelete
                    && sp.UserId == studentPermission.UserId
                    && sp.CourseId == studentPermission.CourseId)
                .Select(sp => sp.Id)
                .ToListAsync();

            if (!sameGroupPermissions.Any())
                return null;

            // 取得所有該組的出席記錄（按日期排序）
            var combinedAttendances = await ctx.TblAttendance
                .Where(a => !a.IsDelete
                    && sameGroupPermissions.Contains(a.StudentPermissionId))
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            // 取得該組對應的 StudentPermissionFee
            var combinedFees = await ctx.TblStudentPermissionFee
                .Where(spf => !spf.IsDelete
                    && sameGroupPermissions.Contains(spf.StudentPermissionId))
                .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
                .ThenBy(spf => spf.Id)
                .ToListAsync();

            // 找出當前 attendance 的位置
            int attendanceIndex = combinedAttendances.FindIndex(a => a.Id == attendance.Id);
            if (attendanceIndex == -1)
                return null;

            int feeIndex = attendanceIndex / 4;
            int positionInFee = attendanceIndex % 4; // 在該費用中的位置 (0-3)

            return new AttendancePositionInfo
            {
                AttendanceIndex = attendanceIndex,
                FeeIndex = feeIndex,
                PositionInFee = positionInFee + 1, // 轉換為 1-4
                TotalAttendances = combinedAttendances.Count,
                TotalFees = combinedFees.Count,
                CorrespondingFee = feeIndex < combinedFees.Count ? combinedFees[feeIndex] : null
            };
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

            if (!combinedFees.Any())
                return null;

            // 3. 取得所有該組的出席記錄（按日期排序）
            var combinedAttendances = await ctx.TblAttendance
                .Where(a => !a.IsDelete
                    && sameGroupPermissions.Contains(a.StudentPermissionId))
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            // 4. 檢查每個費用，找出第一個還沒塞滿 4 個出席記錄的
            for (int feeIndex = 0; feeIndex < combinedFees.Count; feeIndex++)
            {
                // 計算這個費用對應的出席記錄範圍
                int startIndex = feeIndex * 4;
                int endIndex = startIndex + 4;
                
                // 計算這個費用已經有多少個出席記錄
                int attendanceCount = 0;
                for (int i = startIndex; i < endIndex && i < combinedAttendances.Count; i++)
                {
                    attendanceCount++;
                }

                // 如果還沒滿 4 個，回傳這個費用
                if (attendanceCount < 4)
                {
                    return combinedFees[feeIndex];
                }
            }

            // 所有費用都已滿，回傳 null
            return null;
        }

        /// <summary>
        /// 根據 StudentPermission 找到對應組別中最早的還沒塞滿 4 個出席記錄的 StudentPermissionFee (同步版本)
        /// 依「相同學生 + 相同課程」分組查詢
        /// </summary>
        /// <param name="studentPermission">學生權限</param>
        /// <param name="ctx">資料庫上下文</param>
        /// <returns>還沒塞滿的費用記錄，若無則回傳 null</returns>
        public static TblStudentPermissionFee? GetFirstAvailableStudentPermissionFee(
            this TblStudentPermission studentPermission,
            DoorDbContext ctx)
        {
            if (studentPermission == null)
                return null;

            // 1. 依「相同學生 + 相同課程」分組查詢所有學生權限
            var sameGroupPermissions = ctx.TblStudentPermission
                .Where(sp => !sp.IsDelete
                    && sp.UserId == studentPermission.UserId
                    && sp.CourseId == studentPermission.CourseId)
                .Select(sp => sp.Id)
                .ToList();

            if (!sameGroupPermissions.Any())
                return null;

            // 2. 取得該組對應的 StudentPermissionFee（按繳款日期排序，從最早的開始）
            var combinedFees = ctx.TblStudentPermissionFee
                .Where(spf => !spf.IsDelete
                    && sameGroupPermissions.Contains(spf.StudentPermissionId))
                .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
                .ThenBy(spf => spf.Id)
                .ToList();

            if (!combinedFees.Any())
                return null;

            // 3. 取得所有該組的出席記錄（按日期排序）
            var combinedAttendances = ctx.TblAttendance
                .Where(a => !a.IsDelete
                    && sameGroupPermissions.Contains(a.StudentPermissionId))
                .OrderBy(a => a.AttendanceDate)
                .ToList();

            // 4. 檢查每個費用，找出第一個還沒塞滿 4 個出席記錄的
            for (int feeIndex = 0; feeIndex < combinedFees.Count; feeIndex++)
            {
                // 計算這個費用對應的出席記錄範圍
                int startIndex = feeIndex * 4;
                int endIndex = startIndex + 4;

                // 計算這個費用已經有多少個出席記錄
                int attendanceCount = 0;
                for (int i = startIndex; i < endIndex && i < combinedAttendances.Count; i++)
                {
                    attendanceCount++;
                }

                // 如果還沒滿 4 個，回傳這個費用
                if (attendanceCount < 4)
                {
                    return combinedFees[feeIndex];
                }
            }

            // 所有費用都已滿，回傳 null
            return null;
        }

        /// <summary>
        /// 根據 StudentPermission 取得該組所有費用的出席記錄填充狀態
        /// </summary>
        /// <param name="studentPermission">學生權限</param>
        /// <param name="ctx">資料庫上下文</param>
        /// <returns>費用填充狀態列表</returns>
        public static async Task<List<FeeAttendanceStatus>> GetFeeAttendanceStatusAsync(
            this TblStudentPermission studentPermission,
            DoorDbContext ctx)
        {
            var result = new List<FeeAttendanceStatus>();

            if (studentPermission == null)
                return result;

            // 1. 依「相同學生 + 相同課程」分組查詢所有學生權限
            var sameGroupPermissions = await ctx.TblStudentPermission
                .Where(sp => !sp.IsDelete
                    && sp.UserId == studentPermission.UserId
                    && sp.CourseId == studentPermission.CourseId)
                .Select(sp => sp.Id)
                .ToListAsync();

            if (!sameGroupPermissions.Any())
                return result;

            // 2. 取得該組對應的 StudentPermissionFee（按繳款日期排序）
            var combinedFees = await ctx.TblStudentPermissionFee
                .Where(spf => !spf.IsDelete
                    && sameGroupPermissions.Contains(spf.StudentPermissionId))
                .OrderBy(spf => spf.PaymentDate ?? DateTime.MinValue)
                .ThenBy(spf => spf.Id)
                .ToListAsync();

            if (!combinedFees.Any())
                return result;

            // 3. 取得所有該組的出席記錄（按日期排序）
            var combinedAttendances = await ctx.TblAttendance
                .Where(a => !a.IsDelete
                    && sameGroupPermissions.Contains(a.StudentPermissionId))
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            // 4. 計算每個費用的出席記錄狀態
            for (int feeIndex = 0; feeIndex < combinedFees.Count; feeIndex++)
            {
                int startIndex = feeIndex * 4;
                int endIndex = startIndex + 4;

                var attendancesForFee = new List<TblAttendance>();
                for (int i = startIndex; i < endIndex && i < combinedAttendances.Count; i++)
                {
                    attendancesForFee.Add(combinedAttendances[i]);
                }

                result.Add(new FeeAttendanceStatus
                {
                    Fee = combinedFees[feeIndex],
                    FeeIndex = feeIndex,
                    AttendanceCount = attendancesForFee.Count,
                    IsFull = attendancesForFee.Count >= 4,
                    RemainingSlots = 4 - attendancesForFee.Count,
                    Attendances = attendancesForFee
                });
            }

            return result;
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
        /// 在該費用中的位置（1-4）
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
        /// 是否為該費用的最後一次出席（第 4 次）
        /// </summary>
        public bool IsLastAttendanceOfFee => PositionInFee == 4;

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
        /// 是否已滿（4 筆出席）
        /// </summary>
        public bool IsFull { get; set; }

        /// <summary>
        /// 剩餘可填充的出席記錄數量
        /// </summary>
        public int RemainingSlots { get; set; }

        /// <summary>
        /// 該費用對應的所有出席記錄
        /// </summary>
        public List<TblAttendance> Attendances { get; set; } = new List<TblAttendance>();

        /// <summary>
        /// 填充進度百分比（0-100）
        /// </summary>
        public decimal ProgressPercentage => (decimal)AttendanceCount / 4 * 100;

        /// <summary>
        /// 填充進度文字（例如：3/4）
        /// </summary>
        public string ProgressText => $"{AttendanceCount}/4";
    }
}
