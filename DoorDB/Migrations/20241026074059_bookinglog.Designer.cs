﻿// <auto-generated />
using System;
using DoorDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DoorWebDB.Migrations
{
    [DbContext(typeof(DoorDbContext))]
    [Migration("20241026074059_bookinglog")]
    partial class bookinglog
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.31")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("DoorDB.TblAuditLog", b =>
                {
                    b.Property<int>("Serial")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("ActType")
                        .HasColumnType("int");

                    b.Property<DateTime>("ActionTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("IP")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Serial");

                    b.ToTable("tblAuditLog");
                });

            modelBuilder.Entity("DoorDB.TblBookingLog", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime>("EventTime")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("UpdateUserId")
                        .HasColumnType("int");

                    b.Property<int>("UserAddress")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserAddress");

                    b.ToTable("tblBookingLog");
                });

            modelBuilder.Entity("DoorDB.TblPermission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasComment("權限項目Id");

                    b.Property<string>("DateFrom")
                        .IsRequired()
                        .HasColumnType("varchar(10)")
                        .HasComment("權限日期起");

                    b.Property<string>("DateTo")
                        .IsRequired()
                        .HasColumnType("varchar(10)");

                    b.Property<string>("Days")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsEnable")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("PermissionLevel")
                        .HasColumnType("int");

                    b.Property<string>("TimeFrom")
                        .IsRequired()
                        .HasColumnType("varchar(5)")
                        .HasComment("權限時間起");

                    b.Property<string>("TimeTo")
                        .IsRequired()
                        .HasColumnType("varchar(5)");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasComment("權限項目所屬使用者([tblUser].[Id])");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("tblPermission");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            DateFrom = "2024/07/21",
                            DateTo = "2124/07/21",
                            Days = "1,2,3,4,5,6,7",
                            IsDelete = false,
                            IsEnable = true,
                            PermissionLevel = 1,
                            TimeFrom = "00:00",
                            TimeTo = "24:00",
                            UserId = 51
                        },
                        new
                        {
                            Id = 2,
                            DateFrom = "2024/07/21",
                            DateTo = "2124/07/21",
                            Days = "",
                            IsDelete = false,
                            IsEnable = true,
                            PermissionLevel = 1,
                            TimeFrom = "00:00",
                            TimeTo = "24:00",
                            UserId = 52
                        });
                });

            modelBuilder.Entity("DoorDB.TblPermissionGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("tblPermissionGroup");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "大門"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Car教室"
                        },
                        new
                        {
                            Id = 3,
                            Name = "Sunny教室"
                        },
                        new
                        {
                            Id = 4,
                            Name = "儲藏室"
                        });
                });

            modelBuilder.Entity("DoorDB.TblQRCodeStorage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("ModifiedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("QRCodeData")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("qrcodeTxt")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("userTag")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("tblQRCodeStorage");
                });

            modelBuilder.Entity("DoorDB.TblRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<bool>("CanDelete")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("CreatorUserId")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsEnable")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("ModifiedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("tblRole");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            CanDelete = false,
                            CreatedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2821),
                            CreatorUserId = 1,
                            Description = "管理者",
                            IsDelete = false,
                            IsEnable = true,
                            ModifiedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2821),
                            Name = "Admin"
                        },
                        new
                        {
                            Id = 2,
                            CanDelete = false,
                            CreatedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2823),
                            CreatorUserId = 1,
                            Description = "老師",
                            IsDelete = false,
                            IsEnable = true,
                            ModifiedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2823),
                            Name = "User"
                        },
                        new
                        {
                            Id = 3,
                            CanDelete = false,
                            CreatedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2824),
                            CreatorUserId = 1,
                            Description = "學生",
                            IsDelete = false,
                            IsEnable = true,
                            ModifiedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2824),
                            Name = "User"
                        },
                        new
                        {
                            Id = 4,
                            CanDelete = false,
                            CreatedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2825),
                            CreatorUserId = 1,
                            Description = "值班人員",
                            IsDelete = false,
                            IsEnable = true,
                            ModifiedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2825),
                            Name = "User"
                        });
                });

            modelBuilder.Entity("DoorDB.TblStudentPermission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasComment("權限項目Id");

                    b.Property<string>("DateFrom")
                        .IsRequired()
                        .HasColumnType("varchar(10)")
                        .HasComment("權限日期起");

                    b.Property<string>("DateTo")
                        .IsRequired()
                        .HasColumnType("varchar(10)");

                    b.Property<string>("Days")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsEnable")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("PermissionLevel")
                        .HasColumnType("int");

                    b.Property<string>("TimeFrom")
                        .IsRequired()
                        .HasColumnType("varchar(5)")
                        .HasComment("權限時間起");

                    b.Property<string>("TimeTo")
                        .IsRequired()
                        .HasColumnType("varchar(5)");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasComment("權限項目所屬使用者([tblUser].[Id])");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("tblStudentPermission");
                });

            modelBuilder.Entity("DoorDB.TblUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("AccountType")
                        .IsRequired()
                        .HasColumnType("varchar(10)");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<bool>("IsDelete")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsEnable")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("LastLoginIP")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("LastLoginTime")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("ModifiedTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Secret")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Type")
                        .HasColumnType("int")
                        .HasComment("選課狀態 預設0,1在學,2停課,3約課");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("locale")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("tblUser");

                    b.HasData(
                        new
                        {
                            Id = 51,
                            AccountType = "LOCAL",
                            CreateTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2807),
                            DisplayName = "Administrator",
                            Email = "",
                            IsDelete = false,
                            IsEnable = true,
                            LastLoginIP = "",
                            ModifiedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2816),
                            Phone = "0",
                            Secret = "1qaz2wsx",
                            Type = 0,
                            Username = "admin",
                            locale = 1
                        },
                        new
                        {
                            Id = 52,
                            AccountType = "LOCAL",
                            CreateTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2818),
                            DisplayName = "臨時大門",
                            Email = "",
                            IsDelete = false,
                            IsEnable = true,
                            LastLoginIP = "",
                            ModifiedTime = new DateTime(2024, 10, 26, 15, 40, 59, 14, DateTimeKind.Local).AddTicks(2819),
                            Phone = "0",
                            Secret = "1qaz2wsx",
                            Type = 0,
                            Username = "TemDoor",
                            locale = 1
                        });
                });

            modelBuilder.Entity("TblPermissionGroupTblStudentPermission", b =>
                {
                    b.Property<int>("PermissionGroupsId")
                        .HasColumnType("int");

                    b.Property<int>("StudentPermissionsId")
                        .HasColumnType("int");

                    b.HasKey("PermissionGroupsId", "StudentPermissionsId");

                    b.HasIndex("StudentPermissionsId");

                    b.ToTable("TblPermissionGroupTblStudentPermission");
                });

            modelBuilder.Entity("TblPermissionTblPermissionGroup", b =>
                {
                    b.Property<int>("PermissionGroupsId")
                        .HasColumnType("int");

                    b.Property<int>("PermissionsId")
                        .HasColumnType("int");

                    b.HasKey("PermissionGroupsId", "PermissionsId");

                    b.HasIndex("PermissionsId");

                    b.ToTable("TblPermissionTblPermissionGroup");

                    b.HasData(
                        new
                        {
                            PermissionGroupsId = 1,
                            PermissionsId = 1
                        },
                        new
                        {
                            PermissionGroupsId = 2,
                            PermissionsId = 1
                        },
                        new
                        {
                            PermissionGroupsId = 3,
                            PermissionsId = 1
                        },
                        new
                        {
                            PermissionGroupsId = 4,
                            PermissionsId = 1
                        },
                        new
                        {
                            PermissionGroupsId = 1,
                            PermissionsId = 2
                        });
                });

            modelBuilder.Entity("TblQRCodeStorageTblUser", b =>
                {
                    b.Property<int>("QRCodesId")
                        .HasColumnType("int");

                    b.Property<int>("UsersId")
                        .HasColumnType("int");

                    b.HasKey("QRCodesId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("TblQRCodeStorageTblUser");
                });

            modelBuilder.Entity("TblRoleTblUser", b =>
                {
                    b.Property<int>("RolesId")
                        .HasColumnType("int");

                    b.Property<int>("UsersId")
                        .HasColumnType("int");

                    b.HasKey("RolesId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("TblRoleTblUser");

                    b.HasData(
                        new
                        {
                            RolesId = 1,
                            UsersId = 51
                        },
                        new
                        {
                            RolesId = 4,
                            UsersId = 52
                        });
                });

            modelBuilder.Entity("DoorDB.TblBookingLog", b =>
                {
                    b.HasOne("DoorDB.TblUser", "User")
                        .WithMany("BookingLogs")
                        .HasForeignKey("UserAddress")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DoorDB.TblPermission", b =>
                {
                    b.HasOne("DoorDB.TblUser", "User")
                        .WithOne("Permission")
                        .HasForeignKey("DoorDB.TblPermission", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DoorDB.TblStudentPermission", b =>
                {
                    b.HasOne("DoorDB.TblUser", "User")
                        .WithMany("StudentPermissions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("TblPermissionGroupTblStudentPermission", b =>
                {
                    b.HasOne("DoorDB.TblPermissionGroup", null)
                        .WithMany()
                        .HasForeignKey("PermissionGroupsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DoorDB.TblStudentPermission", null)
                        .WithMany()
                        .HasForeignKey("StudentPermissionsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TblPermissionTblPermissionGroup", b =>
                {
                    b.HasOne("DoorDB.TblPermissionGroup", null)
                        .WithMany()
                        .HasForeignKey("PermissionGroupsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DoorDB.TblPermission", null)
                        .WithMany()
                        .HasForeignKey("PermissionsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TblQRCodeStorageTblUser", b =>
                {
                    b.HasOne("DoorDB.TblQRCodeStorage", null)
                        .WithMany()
                        .HasForeignKey("QRCodesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DoorDB.TblUser", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TblRoleTblUser", b =>
                {
                    b.HasOne("DoorDB.TblRole", null)
                        .WithMany()
                        .HasForeignKey("RolesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DoorDB.TblUser", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("DoorDB.TblUser", b =>
                {
                    b.Navigation("BookingLogs");

                    b.Navigation("Permission")
                        .IsRequired();

                    b.Navigation("StudentPermissions");
                });
#pragma warning restore 612, 618
        }
    }
}
