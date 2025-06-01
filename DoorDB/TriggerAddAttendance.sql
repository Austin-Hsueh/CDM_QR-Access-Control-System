DELIMITER $$
CREATE TRIGGER trg_AccessEventLog_Insert_Attendance
AFTER INSERT ON tblAccessEventLog
FOR EACH ROW
BEGIN
    -- 只針對大門
    IF NEW.DoorNumber = 1 THEN
        -- 取得該UserId對應的課程（假設tblStudentPermission有UserId, CourseId）
        INSERT INTO tblAttendance (UserId, CourseId, AttendanceDate, CreatedTime, ModifiedTime, IsDelete)
        SELECT NEW.UserAddress, sp.CourseId, DATE(NEW.EventTime), NOW(), NOW(), 0
        FROM tblStudentPermission sp
        WHERE sp.UserId = NEW.UserAddress
        ON DUPLICATE KEY UPDATE ModifiedTime = NOW();
    END IF;
END $$
DELIMITER ;