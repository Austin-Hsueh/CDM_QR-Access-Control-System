using Microsoft.EntityFrameworkCore;
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
        public virtual DbSet<TblPermission> TblPermissions { set; get; } = null!;
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
                new TblPermissionGroup(){Id = 51, Name = "大門", NameI18n= "" },
                new TblPermissionGroup(){Id = 52, Name = "Car教室", NameI18n= "" },
                new TblPermissionGroup(){Id = 53, Name = "Sunny教室", NameI18n= "" },
                new TblPermissionGroup(){Id = 54, Name = "儲藏室", NameI18n= "" },
            };

            List<TblPermission> DefaultPermissions = new List<TblPermission>()
            {
                //new TblPermission(){ Id = 110, Name = "查詢", NameI18n = "a", PermissionGroupId = 1 },
                //new TblPermission(){ Id = 120, Name = "修改", NameI18n = "a", PermissionGroupId = 1 },

                //new TblPermission(){ Id = 210, Name = "查詢", NameI18n = "a", PermissionGroupId = 2 },
                //new TblPermission(){ Id = 220, Name = "修改", NameI18n = "a", PermissionGroupId = 2 },

                //new TblPermission(){ Id = 310, Name = "查詢", NameI18n = "a", PermissionGroupId = 3 },
                //new TblPermission(){ Id = 320, Name = "修改", NameI18n = "a", PermissionGroupId = 3 },
                //new TblPermission(){ Id = 331, Name = "資料庫維護-修改(僅限自己)", NameI18n = "a", PermissionGroupId = 3 },
                //new TblPermission(){ Id = 332, Name = "資料庫維護-修改(不限使用者)", NameI18n = "a", PermissionGroupId = 3 },
                //new TblPermission(){ Id = 341, Name = "資料庫維護-刪除(僅限自己)", NameI18n = "a", PermissionGroupId = 3 },
                //new TblPermission(){ Id = 342, Name = "資料庫維護-刪除(不限使用者)", NameI18n = "a", PermissionGroupId = 3 },

                //new TblPermission(){ Id = 410, Name = "查詢", NameI18n = "a", PermissionGroupId = 4 },
                //new TblPermission(){ Id = 420, Name = "修改", NameI18n = "a", PermissionGroupId = 4 },
                //new TblPermission(){ Id = 420, Name = "系統設定-新增", NameI18n = "a", PermissionGroupId = 4 },
                //new TblPermission(){ Id = 430, Name = "系統設定-修改", NameI18n = "a", PermissionGroupId = 4 },
                //new TblPermission(){ Id = 440, Name = "系統設定-刪除", NameI18n = "a", PermissionGroupId = 4 },
            };


            //TblQRCodeStorage AdminQRCode1 = new TblQRCodeStorage()
            //{
            //    Id = 1, QRCodeData = "iVBORw0KGgoAAAANSUhEUgAAAMgAAADICAYAAACtWK6eAA",
            //    QRcodeType = QRcodeType.Common,
            //    IsEnable = true,
            //    DoorTime = DateTime.Now,
            //    ModifiedTime = DateTime.Now,
            //    CreateTime = DateTime.Now 
            //};

            //TblQRCodeStorage AdminQRCode2 = new TblQRCodeStorage()
            //{
            //    Id = 2,
            //    QRCodeData = "Ars4c6QAADEpJREFUeF7tneF287gKRSfv/9C9K+1Mb",
            //    QRcodeType = QRcodeType.Temporary,
            //    IsEnable = true,
            //    DoorTime = DateTime.Now,
            //    ModifiedTime = DateTime.Now,
            //    CreateTime = DateTime.Now
            //};


            TblUser DefaultAdmin = new TblUser()
            {
                Id = 51,
                Username = "admin",
                AccountType = LoginAccountType.LOCAL,
                Email = "",
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


            modelBuilder.Entity<TblPermissionGroup>()
                .HasData(DefaultPermissionGroups);

            //modelBuilder.Entity<TblPermission>()
            //    .HasData(DefaultPermissions);

            //List<int> UserPermissionList = new List<int> { 110, 210, 310, 410 };
            //var UserRolePermissions = DefaultPermissions
            //    .Where(x => UserPermissionList.Contains(x.Id))
            //    .Select(x => new { RolesId = DefaultUserRole.Id, PermissionsId = x.Id })
            //    .ToList();

            //var AdminRolePermissions = DefaultPermissions
            //    .Select(x => new { RolesId = DefaultAdminRole.Id, PermissionsId = x.Id })
            //    .ToList();

            //var DefaultRolePermissionRelation = UserRolePermissions;
            //DefaultRolePermissionRelation.AddRange(AdminRolePermissions);

            modelBuilder.Entity<TblUser>()
               .HasMany(x => x.Permissions)
               .WithMany(x => x.Users);
            //.UsingEntity(j => j.HasData(DefaultRolePermissionRelation));


            modelBuilder.Entity<TblUser>()
                .HasMany(x => x.Roles)
                .WithMany(x => x.Users);
                //.UsingEntity(j => j.HasData(new { UsersId = DefaultAdmin.Id, RolesId = DefaultAdminRole.Id }));


            modelBuilder.Entity<TblRole>()
                .HasData(
                    DefaultAdminRole,
                    DefaultUserRole,
                    DefaultUserRole2,
                    DefaultUserRole3
                );

            modelBuilder.Entity<TblUser>()
               .HasData(
                    DefaultAdmin
               );

            modelBuilder.Entity<TblUser>()
                .HasMany(x => x.QRCodes)
                .WithMany(x => x.Users);
                //.UsingEntity(j => j.HasData(
                //    new { UsersId = 51, QRCodesId = 1 },
                //    new { UsersId = 51, QRCodesId = 2 }
                // ));

            //modelBuilder.Entity<TblQRCodeStorage>()
            //    .HasData(
            //        AdminQRCode1,
            //        AdminQRCode2
            //    );


            #endregion



        }
    }
}