DELIMITER $$
CREATE TRIGGER trg_AccessEventLog_Insert_Attendance
AFTER INSERT ON tblAccessEventLog
FOR EACH ROW
BEGIN
    DECLARE isParent INT DEFAULT 0;
    -- 只針對大門
    IF NEW.DoorNumber = 1 THEN
        -- 判斷是否為父帳號
        SELECT COUNT(*) INTO isParent FROM tblUser WHERE ParentId = NEW.UserAddress AND IsDelete = 0;
        IF isParent > 0 THEN
            -- 若是父帳號，找出所有子帳號，並為每個子帳號插入出席紀錄
            INSERT INTO tblAttendance (StudentPermissionId, AttendanceDate, AttendanceType, IsTrigger, ModifiedUserId, CreatedTime, ModifiedTime, IsDelete)
            SELECT 
                sp.Id, 
                DATE(NEW.EventTime), 
                1, 
                1, 
                NEW.UserAddress, 
                NOW(), 
                NOW(), 
                0
            FROM tblStudentPermission sp
            JOIN tblUser u ON u.Id = sp.UserId
            WHERE u.ParentId = NEW.UserAddress
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