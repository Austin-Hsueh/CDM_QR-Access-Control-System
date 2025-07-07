DELIMITER $$
CREATE TRIGGER trg_AccessEventLog_Insert_Attendance
AFTER INSERT ON tblAccessEventLog
FOR EACH ROW
BEGIN
    -- 只針對大門
    IF NEW.DoorNumber = 1 THEN
        -- 先找出是否為父帳號
        DECLARE parentId INT;
        SELECT Id INTO parentId FROM tblUser WHERE Id = NEW.UserAddress AND IsDelete = 0 AND ParentId IS NULL;
        IF parentId IS NOT NULL THEN
            -- 若是父帳號，找出所有子帳號，並為每個子帳號插入出席紀錄
            INSERT INTO tblAttendance (StudentPermissionId, AttendanceDate, AttendanceType, IsTrigger, ModifiedUserId, CreatedTime, ModifiedTime, IsDelete)
            SELECT 
                sp.Id, 
                DATE(NEW.EventTime), 
                1, 
                1, 
                51, 
                NOW(), 
                NOW(), 
                0
            FROM tblStudentPermission sp
            JOIN tblUser u ON u.Id = sp.UserId
            WHERE u.ParentId = parentId
              AND sp.IsDelete = 0
              AND DATE(NEW.EventTime) >= STR_TO_DATE(sp.DateFrom, '%Y/%m/%d')
              AND DATE(NEW.EventTime) <= STR_TO_DATE(sp.DateTo, '%Y/%m/%d')
              AND TIME(NEW.EventTime) <= STR_TO_DATE(sp.TimeTo, '%H:%i')
              AND FIND_IN_SET(
                  CASE WHEN DAYOFWEEK(NEW.EventTime) = 1 THEN 7 ELSE DAYOFWEEK(NEW.EventTime) - 1 END,
                  sp.Days
              ) > 0
              AND NOT EXISTS (
                  SELECT 1 FROM tblAttendance a
                  WHERE a.StudentPermissionId = sp.Id
                    AND a.AttendanceDate = DATE(NEW.EventTime)
                    AND a.IsDelete = 0
              );
        ELSE
            -- 若不是父帳號，維持原本行為
            INSERT INTO tblAttendance (StudentPermissionId, AttendanceDate, AttendanceType, IsTrigger, ModifiedUserId, CreatedTime, ModifiedTime, IsDelete)
            SELECT 
                sp.Id, 
                DATE(NEW.EventTime), 
                1, 
                1, 
                51, 
                NOW(), 
                NOW(), 
                0
            FROM tblStudentPermission sp
            WHERE 
                sp.UserId = NEW.UserAddress
                AND sp.IsDelete = 0
                AND DATE(NEW.EventTime) >= STR_TO_DATE(sp.DateFrom, '%Y/%m/%d')
                AND DATE(NEW.EventTime) <= STR_TO_DATE(sp.DateTo, '%Y/%m/%d')
                AND TIME(NEW.EventTime) <= STR_TO_DATE(sp.TimeTo, '%H:%i')
                AND FIND_IN_SET(
                    CASE WHEN DAYOFWEEK(NEW.EventTime) = 1 THEN 7 ELSE DAYOFWEEK(NEW.EventTime) - 1 END,
                    sp.Days
                ) > 0
                AND NOT EXISTS (
                    SELECT 1 FROM tblAttendance a
                    WHERE a.StudentPermissionId = sp.Id
                      AND a.AttendanceDate = DATE(NEW.EventTime)
                      AND a.IsDelete = 0
                );
        END IF;
    END IF;
END $$
DELIMITER ;