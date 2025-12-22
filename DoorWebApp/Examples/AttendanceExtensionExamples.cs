using DoorDB;
using DoorWebApp.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DoorWebApp.Examples
{
    /// <summary>
    /// AttendanceExtension 使用範例
    /// </summary>
    public class AttendanceExtensionExamples
    {
        private readonly DoorDbContext ctx;

        public AttendanceExtensionExamples(DoorDbContext context)
        {
            ctx = context;
        }

        /// <summary>
        /// 範例 1: 基本用法 - 找到對應的費用
        /// </summary>
        public async Task Example1_GetCorrespondingFee(int attendanceId)
        {
            // 取得出席記錄
            var attendance = await ctx.TblAttendance
                .Include(a => a.StudentPermission)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
            {
                Console.WriteLine("找不到出席記錄");
                return;
            }

            // 使用擴展方法取得對應的費用
            var correspondingFee = await attendance.GetCorrespondingStudentPermissionFeeAsync(ctx);

            if (correspondingFee != null)
            {
                Console.WriteLine($"出席日期: {attendance.AttendanceDate}");
                Console.WriteLine($"對應費用ID: {correspondingFee.Id}");
                Console.WriteLine($"總金額: {correspondingFee.TotalAmount}");
                Console.WriteLine($"繳款日期: {correspondingFee.PaymentDate}");
                
                if (correspondingFee.TeacherSplitRatio.HasValue)
                {
                    Console.WriteLine($"老師拆帳比: {correspondingFee.TeacherSplitRatio:P0}");
                    decimal teacherAmount = correspondingFee.TotalAmount * correspondingFee.TeacherSplitRatio.Value;
                    Console.WriteLine($"老師分潤: {teacherAmount:N0} 元");
                }
            }
            else
            {
                Console.WriteLine("找不到對應的費用記錄");
            }
        }

        /// <summary>
        /// 範例 2: 取得位置資訊
        /// </summary>
        public async Task Example2_GetPositionInfo(int attendanceId)
        {
            var attendance = await ctx.TblAttendance
                .Include(a => a.StudentPermission)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null) return;

            // 取得位置資訊
            var positionInfo = await attendance.GetAttendancePositionInfoAsync(ctx);

            if (positionInfo != null)
            {
                Console.WriteLine($"=== 課程進度資訊 ===");
                Console.WriteLine($"這是第 {positionInfo.AttendanceIndex + 1} 次出席");
                Console.WriteLine($"對應第 {positionInfo.FeeIndex + 1} 筆費用");
                Console.WriteLine($"本期進度: {positionInfo.PositionInFee}/4");
                Console.WriteLine($"總進度: {positionInfo.TotalAttendances} 次出席 / {positionInfo.TotalFees} 筆費用");

                if (positionInfo.IsFirstAttendanceOfFee)
                {
                    Console.WriteLine("✨ 這是本期的第一次上課");
                }
                else if (positionInfo.IsLastAttendanceOfFee)
                {
                    Console.WriteLine("🎉 恭喜！本期課程已完成（4/4）");
                }

                if (positionInfo.CorrespondingFee != null)
                {
                    Console.WriteLine($"本期費用: {positionInfo.CorrespondingFee.TotalAmount} 元");
                }
            }
        }

        /// <summary>
        /// 範例 3: 顯示學生的所有出席和對應費用
        /// </summary>
        public async Task Example3_ShowAllAttendancesWithFees(int studentPermissionId)
        {
            // 取得學生的所有出席記錄
            var attendances = await ctx.TblAttendance
                .Where(a => a.StudentPermissionId == studentPermissionId && !a.IsDelete)
                .Include(a => a.StudentPermission)
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            Console.WriteLine($"=== 出席記錄與費用對應 ===");
            Console.WriteLine($"共 {attendances.Count} 筆出席記錄\n");

            int currentFeeId = -1;
            int feeCount = 0;

            foreach (var attendance in attendances)
            {
                var fee = await attendance.GetCorrespondingStudentPermissionFeeAsync(ctx);
                var info = await attendance.GetAttendancePositionInfoAsync(ctx);

                // 當切換到新的費用時，顯示費用標題
                if (fee != null && fee.Id != currentFeeId)
                {
                    currentFeeId = fee.Id;
                    feeCount++;
                    Console.WriteLine($"\n--- 第 {feeCount} 期費用 (ID: {fee.Id}, 金額: {fee.TotalAmount} 元) ---");
                }

                string progress = info != null ? $"{info.PositionInFee}/4" : "?/4";
                string completeMark = info?.IsLastAttendanceOfFee == true ? " ✓" : "";
                
                Console.WriteLine($"  [{progress}] {attendance.AttendanceDate} - {attendance.AttendanceType}{completeMark}");
            }
        }

        /// <summary>
        /// 範例 4: 檢查課程是否可以結算
        /// </summary>
        public async Task<bool> Example4_CheckIfPeriodIsComplete(int attendanceId)
        {
            var attendance = await ctx.TblAttendance
                .Include(a => a.StudentPermission)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null) return false;

            var positionInfo = await attendance.GetAttendancePositionInfoAsync(ctx);

            if (positionInfo != null && positionInfo.IsLastAttendanceOfFee)
            {
                Console.WriteLine("✅ 本期課程已完成，可以結算");
                
                if (positionInfo.CorrespondingFee != null)
                {
                    var fee = positionInfo.CorrespondingFee;
                    Console.WriteLine($"費用ID: {fee.Id}");
                    Console.WriteLine($"總金額: {fee.TotalAmount}");
                    
                    if (fee.TeacherSplitRatio.HasValue && fee.CourseSplitRatio.HasValue)
                    {
                        decimal teacherAmount = fee.TotalAmount * fee.TeacherSplitRatio.Value;
                        decimal courseAmount = fee.TotalAmount * fee.CourseSplitRatio.Value;
                        
                        Console.WriteLine($"老師應得: {teacherAmount:N0} 元 ({fee.TeacherSplitRatio:P0})");
                        Console.WriteLine($"機構應得: {courseAmount:N0} 元 ({fee.CourseSplitRatio:P0})");
                    }
                }
                
                return true;
            }
            else if (positionInfo != null)
            {
                Console.WriteLine($"⏳ 本期進度: {positionInfo.PositionInFee}/4，尚未完成");
                return false;
            }

            return false;
        }

        /// <summary>
        /// 範例 5: 批次查詢多筆出席的費用資訊
        /// </summary>
        public async Task Example5_BatchGetFees(List<int> attendanceIds)
        {
            var results = new List<(int AttendanceId, string Date, int? FeeId, int? Amount)>();

            var attendances = await ctx.TblAttendance
                .Where(a => attendanceIds.Contains(a.Id))
                .Include(a => a.StudentPermission)
                .ToListAsync();

            foreach (var attendance in attendances)
            {
                var fee = await attendance.GetCorrespondingStudentPermissionFeeAsync(ctx);
                
                results.Add((
                    AttendanceId: attendance.Id,
                    Date: attendance.AttendanceDate,
                    FeeId: fee?.Id,
                    Amount: fee?.TotalAmount
                ));
            }

            // 顯示結果
            Console.WriteLine("出席ID\t日期\t\t費用ID\t金額");
            Console.WriteLine("------------------------------------------------");
            foreach (var result in results)
            {
                Console.WriteLine($"{result.AttendanceId}\t{result.Date}\t{result.FeeId ?? 0}\t{result.Amount ?? 0}");
            }
        }

        /// <summary>
        /// 範例 6: 同步版本使用 (已載入資料)
        /// </summary>
        public void Example6_SynchronousUsage(int attendanceId)
        {
            // 先載入所有需要的資料
            var attendance = ctx.TblAttendance
                .Include(a => a.StudentPermission)
                .FirstOrDefault(a => a.Id == attendanceId);

            if (attendance != null)
            {
                // 使用同步版本
                var fee = attendance.GetCorrespondingStudentPermissionFee(ctx);

                if (fee != null)
                {
                    Console.WriteLine($"對應費用: {fee.Id}, 金額: {fee.TotalAmount}");
                }
            }
        }

        /// <summary>
        /// 範例 7: 找到還沒塞滿的費用（新增出席時使用）
        /// </summary>
        public async Task Example7_FindAvailableFee(int studentPermissionId)
        {
            var studentPermission = await ctx.TblStudentPermission
                .FirstOrDefaultAsync(sp => sp.Id == studentPermissionId);

            if (studentPermission == null)
            {
                Console.WriteLine("找不到學生權限");
                return;
            }

            // 找到最早的還沒塞滿的費用
            var availableFee = await studentPermission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

            if (availableFee != null)
            {
                Console.WriteLine($"=== 找到可用的費用 ===");
                Console.WriteLine($"費用ID: {availableFee.Id}");
                Console.WriteLine($"總金額: {availableFee.TotalAmount}");
                Console.WriteLine($"繳款日期: {availableFee.PaymentDate}");
                Console.WriteLine($"老師拆帳比: {availableFee.TeacherSplitRatio:P0}");
                Console.WriteLine($"課程拆帳比: {availableFee.CourseSplitRatio:P0}");
            }
            else
            {
                Console.WriteLine("❌ 所有費用都已滿或沒有費用記錄");
                Console.WriteLine("請先新增費用記錄");
            }
        }

        /// <summary>
        /// 範例 8: 查看所有費用的填充狀態
        /// </summary>
        public async Task Example8_ViewAllFeeStatus(int studentPermissionId)
        {
            var studentPermission = await ctx.TblStudentPermission
                .Include(sp => sp.User)
                .Include(sp => sp.Course)
                .FirstOrDefaultAsync(sp => sp.Id == studentPermissionId);

            if (studentPermission == null) return;

            // 取得所有費用的填充狀態
            var statuses = await studentPermission.GetFeeAttendanceStatusAsync(ctx);

            Console.WriteLine($"=== {studentPermission.User?.Name} - {studentPermission.Course?.Name} ===");
            Console.WriteLine($"共 {statuses.Count} 筆費用\n");

            foreach (var status in statuses)
            {
                string fullMark = status.IsFull ? "✓ 已滿" : "○ 未滿";
                string barChart = new string('█', status.AttendanceCount) + new string('░', status.RemainingSlots);
                
                Console.WriteLine($"第 {status.FeeIndex + 1} 期 - 費用ID: {status.Fee.Id}");
                Console.WriteLine($"  進度: [{barChart}] {status.ProgressText} ({status.ProgressPercentage:F0}%) {fullMark}");
                Console.WriteLine($"  金額: {status.Fee.TotalAmount} 元");
                Console.WriteLine($"  繳款日期: {status.Fee.PaymentDate?.ToString("yyyy/MM/dd") ?? "未繳款"}");
                Console.WriteLine($"  剩餘名額: {status.RemainingSlots} 個");
                
                if (status.Attendances.Any())
                {
                    Console.WriteLine($"  出席日期: {string.Join(", ", status.Attendances.Select(a => a.AttendanceDate))}");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 範例 9: 新增出席記錄時自動找到對應的費用
        /// </summary>
        public async Task Example9_AddAttendanceToAvailableFee(int studentPermissionId, string attendanceDate)
        {
            var studentPermission = await ctx.TblStudentPermission
                .FirstOrDefaultAsync(sp => sp.Id == studentPermissionId);

            if (studentPermission == null)
            {
                Console.WriteLine("找不到學生權限");
                return;
            }

            // 1. 找到可用的費用
            var availableFee = await studentPermission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

            if (availableFee == null)
            {
                Console.WriteLine("❌ 沒有可用的費用，無法新增出席記錄");
                Console.WriteLine("請先確認是否有費用記錄，或所有費用是否都已滿");
                return;
            }

            // 2. 建立新的出席記錄
            var newAttendance = new TblAttendance
            {
                StudentPermissionId = studentPermissionId,
                AttendanceDate = attendanceDate,
                AttendanceType = 0, // 正常出席
                IsDelete = false,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };

            ctx.TblAttendance.Add(newAttendance);
            await ctx.SaveChangesAsync();

            Console.WriteLine($"✅ 成功新增出席記錄");
            Console.WriteLine($"出席日期: {attendanceDate}");
            Console.WriteLine($"對應費用: {availableFee.Id}");

            // 3. 檢查該費用是否已滿
            var updatedStatus = await studentPermission.GetFeeAttendanceStatusAsync(ctx);
            var currentFeeStatus = updatedStatus.FirstOrDefault(s => s.Fee.Id == availableFee.Id);

            if (currentFeeStatus != null)
            {
                Console.WriteLine($"該費用進度: {currentFeeStatus.ProgressText}");
                
                if (currentFeeStatus.IsFull)
                {
                    Console.WriteLine("🎉 該期課程已完成（4/4）");
                }
            }
        }

        /// <summary>
        /// 範例 10: 檢查是否可以新增出席記錄
        /// </summary>
        public async Task<bool> Example10_CanAddAttendance(int studentPermissionId)
        {
            var studentPermission = await ctx.TblStudentPermission
                .FirstOrDefaultAsync(sp => sp.Id == studentPermissionId);

            if (studentPermission == null)
            {
                Console.WriteLine("找不到學生權限");
                return false;
            }

            var availableFee = await studentPermission.GetFirstAvailableStudentPermissionFeeAsync(ctx);

            if (availableFee != null)
            {
                Console.WriteLine("✅ 可以新增出席記錄");
                Console.WriteLine($"將會記錄到費用ID: {availableFee.Id}");
                return true;
            }
            else
            {
                Console.WriteLine("❌ 無法新增出席記錄");
                Console.WriteLine("原因：沒有可用的費用或所有費用都已滿");
                
                // 顯示目前狀態
                var statuses = await studentPermission.GetFeeAttendanceStatusAsync(ctx);
                if (statuses.Any())
                {
                    Console.WriteLine($"\n目前共有 {statuses.Count} 筆費用，全部已滿");
                    Console.WriteLine("建議：新增新的費用記錄");
                }
                else
                {
                    Console.WriteLine("\n目前沒有任何費用記錄");
                    Console.WriteLine("建議：先新增費用記錄");
                }
                
                return false;
            }
        }
    }
}
