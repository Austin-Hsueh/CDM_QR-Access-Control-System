-- 清理殘留的老師課表
--
-- 老師課表：權限 TeacherId = 0 且 PermissionLevel = 1（UserId 為老師），是學生課表的對應副本。
-- 過去刪除學生課表時沒有同步刪除老師課表，殘留的老師課表會讓排程在沒課的時段仍派發老師門禁。
-- 殘留定義：同一天、同一時段（StartTime + EndTime）已沒有任何有效的學生課表（該老師為 TeacherId）。
--
-- 只處理今天(含)以後的課表，過去的課表保留不動。
-- 採軟刪除 (IsDelete = 1)，並先把 Id 備份到 _bak_orphan_teacher_schedule，可用最下方的還原語法復原。

SET @today = DATE_FORMAT(CURDATE(), '%Y/%m/%d');

-- 1. 預覽：列出將被刪除的老師課表
SELECT ts.Id, ts.ScheduleDate, ts.StartTime, ts.EndTime, u.DisplayName AS Teacher, tp.Id AS PermissionId, tp.CourseId
FROM tblschedule ts
JOIN tblstudentpermission tp ON tp.Id = ts.StudentPermissionId
JOIN tbluser u ON u.Id = tp.UserId
WHERE ts.IsDelete = 0 AND ts.IsEnable = 1
  AND IFNULL(tp.TeacherId, 0) = 0 AND tp.PermissionLevel = 1
  AND ts.ScheduleDate >= @today
  AND NOT EXISTS (
        SELECT 1
        FROM tblschedule s2
        JOIN tblstudentpermission p2 ON p2.Id = s2.StudentPermissionId
        WHERE s2.IsDelete = 0 AND s2.IsEnable = 1 AND p2.IsDelete = 0
          AND p2.TeacherId = tp.UserId
          AND s2.ScheduleDate = ts.ScheduleDate
          AND s2.StartTime = ts.StartTime
          AND s2.EndTime = ts.EndTime)
ORDER BY ts.ScheduleDate, ts.StartTime, u.DisplayName;

-- 2. 執行：備份 Id 後軟刪除（CREATE TABLE 會隱式 commit，所以放在交易外）
CREATE TABLE IF NOT EXISTS _bak_orphan_teacher_schedule (
    ScheduleId   INT      NOT NULL PRIMARY KEY,
    CleanedTime  DATETIME NOT NULL
);

START TRANSACTION;

INSERT IGNORE INTO _bak_orphan_teacher_schedule (ScheduleId, CleanedTime)
SELECT ts.Id, NOW()
FROM tblschedule ts
JOIN tblstudentpermission tp ON tp.Id = ts.StudentPermissionId
WHERE ts.IsDelete = 0 AND ts.IsEnable = 1
  AND IFNULL(tp.TeacherId, 0) = 0 AND tp.PermissionLevel = 1
  AND ts.ScheduleDate >= @today
  AND NOT EXISTS (
        SELECT 1
        FROM tblschedule s2
        JOIN tblstudentpermission p2 ON p2.Id = s2.StudentPermissionId
        WHERE s2.IsDelete = 0 AND s2.IsEnable = 1 AND p2.IsDelete = 0
          AND p2.TeacherId = tp.UserId
          AND s2.ScheduleDate = ts.ScheduleDate
          AND s2.StartTime = ts.StartTime
          AND s2.EndTime = ts.EndTime);

UPDATE tblschedule ts
JOIN _bak_orphan_teacher_schedule b ON b.ScheduleId = ts.Id
SET ts.IsDelete = 1, ts.ModifiedTime = NOW(6)
WHERE ts.IsDelete = 0;

SELECT ROW_COUNT() AS deleted_teacher_schedules;

COMMIT;

-- 3. 還原（需要時才執行）
-- UPDATE tblschedule ts
-- JOIN _bak_orphan_teacher_schedule b ON b.ScheduleId = ts.Id
-- SET ts.IsDelete = 0, ts.ModifiedTime = NOW(6);
