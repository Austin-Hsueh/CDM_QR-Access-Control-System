﻿using Microsoft.EntityFrameworkCore;
using DoorDB.Enums;

namespace DoorDB
{
    public class DoorDbContext : DbContext
    {
        public DoorDbContext()
        {

        }

        public DoorDbContext(DbContextOptions<DoorDbContext> options)
            : base(options)
        {

        }

        public virtual DbSet<TblAuditLog> TblAuditLogs { set; get; } = null!;
        public virtual DbSet<TblPermission> TblPermission { set; get; } = null!;
        public virtual DbSet<TblPermissionGroup> TblPermissionGroup { set; get; } = null!;
        public virtual DbSet<TblRole> TblRoles { set; get; } = null!;
        public virtual DbSet<TblUser> TblUsers { set; get; } = null!;
        public virtual DbSet<TblQRCodeStorage> TbQRCodeStorages { set; get; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //string ConnectionStringOnDesing = "data source=127.0.0.1,1433;Database=Door;Uid=ad;Pwd=Aa123456;";
                //string ConnectionStringOnDesing = "Data Source=127.0.0.1,1433;Initial Catalog=Door;User ID=ad;Password=Aa123456;";
                //string ConnectionStringOnDesing = "Server=127.0.0.1,1433;Database=Door;User Id=ad;Password=Aa123456;";


                //optionsBuilder.UseSqlServer(ConnectionStringOnDesing);
                string ConnectionStringOnDesing = "Server=localhost;Port=3306;Database=door;Uid=ad;Pwd=Aa123456;";
                optionsBuilder.UseMySql(ConnectionStringOnDesing, ServerVersion.AutoDetect(ConnectionStringOnDesing));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Data Seeding
            List<TblPermissionGroup> DefaultPermissionGroups = new List<TblPermissionGroup>()
            {
                new TblPermissionGroup(){Id = 1, Name = "大門" },
                new TblPermissionGroup(){Id = 2, Name = "Car教室" },
                new TblPermissionGroup(){Id = 3, Name = "Sunny教室" },
                new TblPermissionGroup(){Id = 4, Name = "儲藏室" },
            };


            TblUser DefaultAdmin = new TblUser()
            {
                Id = 51,
                Username = "admin",
                AccountType = LoginAccountType.LOCAL,
                Email = "",
                Phone = "0",
                Secret = "1qaz2wsx",
                IsDelete = false,
                IsEnable = true,
                locale = LocaleType.zh_tw,
                LastLoginIP = "",
                LastLoginTime = null,
                DisplayName = "Administrator",
                CreateTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
            };

            TblRole DefaultAdminRole = new TblRole()
            {
                Id = 1,
                Name = "Admin",
                Description = "管理者",
                CreatorUserId = 1,
                IsEnable = true,
                IsDelete = false,
                CanDelete = false,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
            };

            TblRole DefaultUserRole = new TblRole()
            {
                Id = 2,
                Name = "User",
                Description = "老師",
                CreatorUserId = 1,
                IsEnable = true,
                IsDelete = false,
                CanDelete = false,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
            };

            TblRole DefaultUserRole2 = new TblRole()
            {
                Id = 3,
                Name = "User",
                Description = "學生",
                CreatorUserId = 1,
                IsEnable = true,
                IsDelete = false,
                CanDelete = false,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
            };

            TblRole DefaultUserRole3 = new TblRole()
            {
                Id = 4,
                Name = "User",
                Description = "值班人員",
                CreatorUserId = 1,
                IsEnable = true,
                IsDelete = false,
                CanDelete = false,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
            };

            TblPermission DefaultAdminPermission = new TblPermission()
            {
                Id = 1,
                UserId = DefaultAdmin.Id,
                IsEnable = true,
                IsDelete = false,
                DateFrom = "2024/07/21",
                DateTo = "2124/07/21",
                TimeFrom = "00:00",
                TimeTo = "24:00",
                Days = "1,2,3,4,5,6,7",
                PermissionLevel = 1
            };


            modelBuilder.Entity<TblPermissionGroup>()
                .HasData(DefaultPermissionGroups);


            // 設置 TblUser 與 TblPermission 一對一關係
            modelBuilder.Entity<TblPermission>()
                .HasOne(p => p.User)
                .WithOne(pg => pg.Permission)
                .HasForeignKey<TblPermission>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 設置 TblPermissionGroup 與 TblPermission 多對多關係
            modelBuilder.Entity<TblPermission>()
                .HasMany(p => p.PermissionGroups)
                .WithMany(pg => pg.Permissions)
                .UsingEntity(j => j.HasData(
                    new { PermissionsId = DefaultAdminPermission.Id, PermissionGroupsId = 1 },
                    new { PermissionsId = DefaultAdminPermission.Id, PermissionGroupsId = 2 },
                    new { PermissionsId = DefaultAdminPermission.Id, PermissionGroupsId = 3 },
                    new { PermissionsId = DefaultAdminPermission.Id, PermissionGroupsId = 4 }
                    ));


            modelBuilder.Entity<TblUser>()
                .HasMany(x => x.Roles)
                .WithMany(x => x.Users)
                .UsingEntity(j => j.HasData(new { UsersId = DefaultAdmin.Id, RolesId = DefaultAdminRole.Id }));


            modelBuilder.Entity<TblRole>()
                .HasData(
                    DefaultAdminRole,
                    DefaultUserRole,
                    DefaultUserRole2,
                    DefaultUserRole3
                );

            modelBuilder.Entity<TblPermission>()
               .HasData(
                    DefaultAdminPermission
               );

            modelBuilder.Entity<TblUser>()
               .HasData(
                    DefaultAdmin
               );

            modelBuilder.Entity<TblUser>()
                .HasMany(x => x.QRCodes)
                .WithMany(x => x.Users);


            #endregion



        }
    }
}