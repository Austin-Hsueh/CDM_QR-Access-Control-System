
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;

namespace DoorWebDB
{
    internal class Class1
    {
        //DELIMITER 

        //CREATE TRIGGER trg_after_insert_accesseventlog
        //AFTER INSERT ON tblaccesseventlog
        //FOR EACH ROW
        //BEGIN
        //DECLARE bookinglog_exists INT DEFAULT 0;
        //    DECLARE user_exists INT DEFAULT 0;

        //-- 檢查 tblbookinglog 中是否已有相同的 UserAddress 且日期部分相同的 EventTime
        //SELECT COUNT(1) INTO bookinglog_exists
        //FROM tblbookinglog
        //WHERE UserAddress = NEW.UserAddress
        //  AND DATE(EventTime) = DATE(NEW.EventTime);
      
        //-- 檢查 tbluser 表中是否存在對應的 UserAddress
        //SELECT COUNT(1) INTO user_exists
        //FROM tbluser
        //WHERE Id = NEW.UserAddress;

        //-- 如果不存在相同記錄且 UserAddress 有效，則插入新的記錄
        //IF bookinglog_exists = 0 AND user_exists = 1 THEN
        //    INSERT INTO tblbookinglog(Id, UserAddress, EventTime, IsDelete, UpdateUserId)
        //    VALUES(NEW.Id, NEW.UserAddress, NEW.EventTime, 0, NULL);
        //    END IF;
        //    END//

        //    DELIMITER;

    }
}