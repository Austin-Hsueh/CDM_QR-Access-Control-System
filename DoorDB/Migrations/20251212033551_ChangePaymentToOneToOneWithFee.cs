using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class ChangePaymentToOneToOneWithFee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 使用 SQL 命令直接執行複雜的遷移操作
            migrationBuilder.Sql(
                @"SET FOREIGN_KEY_CHECKS=0;

-- 1. 修改 TblPayment 的外鍵關係
ALTER TABLE `tblPayment` DROP FOREIGN KEY `FK_tblPayment_tblStudentPermission_StudentPermissionId`;
ALTER TABLE `tblPayment` DROP INDEX `IX_tblPayment_StudentPermissionId`;
ALTER TABLE `tblPayment` CHANGE COLUMN `StudentPermissionId` `StudentPermissionFeeId` INT NOT NULL;
ALTER TABLE `tblPayment` ADD CONSTRAINT `FK_tblPayment_tblStudentPermissionFee_StudentPermissionFeeId` FOREIGN KEY (`StudentPermissionFeeId`) REFERENCES `tblStudentPermissionFee`(`Id`) ON DELETE CASCADE;
CREATE UNIQUE INDEX `IX_tblPayment_StudentPermissionFeeId` ON `tblPayment` (`StudentPermissionFeeId`);

SET FOREIGN_KEY_CHECKS=1;

-- 2. 移除 TblStudentPermissionFee 的 UNIQUE 約束，改為一般索引以支援一對多關係
SET @fk_name = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'tblStudentPermissionFee' 
                AND COLUMN_NAME = 'StudentPermissionId' 
                AND REFERENCED_TABLE_NAME = 'tblStudentPermission' 
                LIMIT 1);

SET @drop_fk = CONCAT('ALTER TABLE `tblStudentPermissionFee` DROP FOREIGN KEY `', @fk_name, '`');
PREPARE stmt FROM @drop_fk;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 刪除 UNIQUE 索引
ALTER TABLE `tblStudentPermissionFee` DROP INDEX `IX_tblStudentPermissionFee_StudentPermissionId`;

-- 重新建立非 UNIQUE 索引
CREATE INDEX `IX_tblStudentPermissionFee_StudentPermissionId` ON `tblStudentPermissionFee` (`StudentPermissionId`);

-- 重新建立外鍵約束
ALTER TABLE `tblStudentPermissionFee` ADD CONSTRAINT `FK_tblStudentPermissionFee_tblStudentPermission_StudentPermissi~` FOREIGN KEY (`StudentPermissionId`) REFERENCES `tblStudentPermission`(`Id`) ON DELETE CASCADE;",
                suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"-- 1. 還原 TblStudentPermissionFee 的 UNIQUE 索引
SET @fk_name = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'tblStudentPermissionFee' 
                AND COLUMN_NAME = 'StudentPermissionId' 
                AND REFERENCED_TABLE_NAME = 'tblStudentPermission' 
                LIMIT 1);

SET @drop_fk = CONCAT('ALTER TABLE `tblStudentPermissionFee` DROP FOREIGN KEY `', @fk_name, '`');
PREPARE stmt FROM @drop_fk;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

ALTER TABLE `tblStudentPermissionFee` DROP INDEX `IX_tblStudentPermissionFee_StudentPermissionId`;
CREATE UNIQUE INDEX `IX_tblStudentPermissionFee_StudentPermissionId` ON `tblStudentPermissionFee` (`StudentPermissionId`);
ALTER TABLE `tblStudentPermissionFee` ADD CONSTRAINT `FK_tblStudentPermissionFee_tblStudentPermission_StudentPermissionId` FOREIGN KEY (`StudentPermissionId`) REFERENCES `tblStudentPermission`(`Id`) ON DELETE CASCADE;

SET FOREIGN_KEY_CHECKS=0;

-- 2. 還原 TblPayment 的外鍵關係
ALTER TABLE `tblPayment` DROP FOREIGN KEY `FK_tblPayment_tblStudentPermissionFee_StudentPermissionFeeId`;
ALTER TABLE `tblPayment` DROP INDEX `IX_tblPayment_StudentPermissionFeeId`;
ALTER TABLE `tblPayment` CHANGE COLUMN `StudentPermissionFeeId` `StudentPermissionId` INT NOT NULL;
ALTER TABLE `tblPayment` ADD CONSTRAINT `FK_tblStudentPermissionFee_tblStudentPermission_StudentPermissi~` FOREIGN KEY (`StudentPermissionId`) REFERENCES `tblStudentPermission`(`Id`) ON DELETE CASCADE;
CREATE INDEX `IX_tblPayment_StudentPermissionId` ON `tblPayment` (`StudentPermissionId`);

SET FOREIGN_KEY_CHECKS=1;",
                suppressTransaction: true);
        }
    }
}
