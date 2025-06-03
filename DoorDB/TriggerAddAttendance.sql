DELIMITER $$
CREATE TRIGGER trg_AccessEventLog_Insert_Attendance
AFTER INSERT ON tblAccessEventLog
FOR EACH ROW
BEGIN
    -- 只針對大門
    IF NEW.DoorNumber = 1 THEN
        INSERT INTO tblAttendance (StudentPermissionId, 
                                   AttendanceDate, 
                                   AttendanceType, 
                                   IsTrigger, 
                                   ModifiedUserId, 
                                   CreatedTime, 
                                   ModifiedTime, 
                                   IsDelete)
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
                WHERE 1=1
                  AND a.StudentPermissionId = sp.Id
                  AND a.AttendanceDate = DATE(NEW.EventTime)
                  AND a.IsDelete = 0
            );
    END IF;
END $$
DELIMITER ;