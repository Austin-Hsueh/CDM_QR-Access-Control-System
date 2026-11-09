-- 清理已刪除權限底下殘留的課表
--
-- 過去在「門禁設定」頁刪除權限時只刪權限本身，底下課表沒有一起刪除，
-- 這些課表在課表頁看不到，但會被關帳批次簽到、學生課表、MCP 讀到（例：權限 7620 每週日 13:30 無老師、無課程的假課）。
--
-- 只處理今天(含)以後、且當天沒有簽到紀錄的課表；過去的課表保留不動（程式已改為不再讀取已刪除權限的課表）。
-- 採軟刪除 (IsDelete = 1)，並先把 Id 備份到 _bak_orphan_schedule_deleted_perm，可用最下方的還原語法復原。

SET @today = DATE_FORMAT(CURDATE(), '%Y/%m/%d');

-- 1. 預覽
SELECT p.Id AS PermissionId, u.DisplayName AS UserName, COUNT(*) AS Schedules,
       MIN(s.ScheduleDate) AS FirstDate, MAX(s.ScheduleDate) AS LastDate
FROM tblschedule s
JOIN tblstudentpermission p ON p.Id = s.StudentPermissionId
JOIN tbluser u ON u.Id = p.UserId
WHERE p.IsDelete = 1 AND s.IsDelete = 0
  AND s.ScheduleDate >= @today
  AND NOT EXISTS (SELECT 1 FROM tblattendance a
                  WHERE a.StudentPermissionId = s.StudentPermissionId AND a.IsDelete = 0
                    AND REPLACE(a.AttendanceDate, '-', '/') = s.ScheduleDate)
GROUP BY p.Id, u.DisplayName
ORDER BY Schedules DESC;

-- 2. 執行：備份 Id 後軟刪除（CREATE TABLE 會隱式 commit，所以放在交易外）
CREATE TABLE IF NOT EXISTS _bak_orphan_schedule_deleted_perm (
    ScheduleId   INT      NOT NULL PRIMARY KEY,
    CleanedTime  DATETIME NOT NULL
);

START TRANSACTION;

INSERT IGNORE INTO _bak_orphan_schedule_deleted_perm (ScheduleId, CleanedTime)
SELECT s.Id, NOW()
FROM tblschedule s
JOIN tblstudentpermission p ON p.Id = s.StudentPermissionId
WHERE p.IsDelete = 1 AND s.IsDelete = 0
  AND s.ScheduleDate >= @today
  AND NOT EXISTS (SELECT 1 FROM tblattendance a
                  WHERE a.StudentPermissionId = s.StudentPermissionId AND a.IsDelete = 0
                    AND REPLACE(a.AttendanceDate, '-', '/') = s.ScheduleDate);

UPDATE tblschedule s
JOIN _bak_orphan_schedule_deleted_perm b ON b.ScheduleId = s.Id
SET s.IsDelete = 1, s.ModifiedTime = NOW(6)
WHERE s.IsDelete = 0;

SELECT ROW_COUNT() AS deleted_schedules;

COMMIT;

-- 3. 還原（需要時才執行）
-- UPDATE tblschedule s
-- JOIN _bak_orphan_schedule_deleted_perm b ON b.ScheduleId = s.Id
-- SET s.IsDelete = 0, s.ModifiedTime = NOW(6);
