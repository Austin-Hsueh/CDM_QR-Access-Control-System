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
        public virtual DbSet<TblPermission> TblPermission { set; get; } = null!;
        public virtual DbSet<TblStudentPermission> TblStudentPermission { set; get; } = null!;
        public virtual DbSet<TblPermissionGroup> TblPermissionGroup { set; get; } = null!;
        public virtual DbSet<TblRole> TblRoles { set; get; } = null!;
        public virtual DbSet<TblUser> TblUsers { set; get; } = null!;
        public virtual DbSet<TblQRCodeStorage> TbQRCodeStorages { set; get; } = null!;

        public virtual DbSet<TblCourse> TbCourses { set; get; } = null!;
        public virtual DbSet<AccessEventLog> AccessEventLog { get; set; } = null!;
        public virtual DbSet<TblAttendance> TblAttendance { get; set; } = null!;
        public virtual DbSet<TblAttendanceFee> TblAttendanceFee { get; set; } = null!;
        public virtual DbSet<TblPayment> TblPayment { get; set; } = null!;
        public virtual DbSet<TblCourseType> TblCourseType { get; set; } = null!;
        public virtual DbSet<TblClassroom> TblClassroom { get; set; } = null!;
        public virtual DbSet<TblSchedule> TblSchedule { get; set; } = null!;
        public virtual DbSet<TblCourseFee> TblCourseFee { get; set; } = null!;
        public virtual DbSet<TblTeacherSettlement> TblTeacherSettlement { get; set; } = null!;
        public virtual DbSet<TblStudentPermissionFee> TblStudentPermissionFee { get; set; } = null!;


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

            TblUser DefaultDoor = new TblUser()
            {
                Id = 52,
                Username = "TemDoor",
                AccountType = LoginAccountType.LOCAL,
                Email = "",
                Phone = "0",
                Secret = "1qaz2wsx",
                IsDelete = false,
                IsEnable = true,
                locale = LocaleType.zh_tw,
                LastLoginIP = "",
                LastLoginTime = null,
                DisplayName = "臨時大門",
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

            TblPermission DefaultDoorPermission = new TblPermission()
            {
                Id = 2,
                UserId = DefaultDoor.Id,
                IsEnable = true,
                IsDelete = false,
                DateFrom = "2024/07/21",
                DateTo = "2124/07/21",
                TimeFrom = "00:00",
                TimeTo = "24:00",
                Days = "",
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
                    new { PermissionsId = DefaultAdminPermission.Id, PermissionGroupsId = 4 },
                    new { PermissionsId = DefaultDoorPermission.Id, PermissionGroupsId = 1 }
                    ));


            modelBuilder.Entity<TblUser>()
                .HasMany(x => x.Roles)
                .WithMany(x => x.Users)
                .UsingEntity(j => j.HasData(new { UsersId = DefaultAdmin.Id, RolesId = DefaultAdminRole.Id },
                                            new { UsersId = DefaultDoor.Id, RolesId = DefaultUserRole3.Id }
                ));


            modelBuilder.Entity<TblRole>()
                .HasData(
                    DefaultAdminRole,
                    DefaultUserRole,
                    DefaultUserRole2,
                    DefaultUserRole3
                );

            modelBuilder.Entity<TblPermission>()
               .HasData(
                    DefaultAdminPermission,
                    DefaultDoorPermission
               );

            modelBuilder.Entity<TblUser>()
               .HasData(
                    DefaultAdmin,
                    DefaultDoor
               );

            modelBuilder.Entity<TblUser>()
                .HasMany(x => x.QRCodes)
                .WithMany(x => x.Users);


            // 設置 TblUser 與 TblStudentPermission 一對多關係
            modelBuilder.Entity<TblUser>()
                .HasMany(p => p.StudentPermissions)
                .WithOne(pg => pg.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 設置 TblPermissionGroup 與 TblStudentPermission 多對多關係
            modelBuilder.Entity<TblStudentPermission>()
                .HasMany(p => p.PermissionGroups)
                .WithMany(pg => pg.StudentPermissions);


            // 設置 TblUser(Teacher) 與 TblStudentPermissions 一對多關係
            modelBuilder.Entity<TblUser>()
               .HasMany(p => p.TeacherStudentPermissions)
               .WithOne(pg => pg.Teacher)
               .HasForeignKey(p => p.TeacherId)
               .IsRequired(false);

            // 設置TblCourse 與 TblStudentPermissions 一對多關係
            modelBuilder.Entity<TblCourse>()
               .HasMany(p => p.CourseStudentPermissions)
               .WithOne(pg => pg.Course)
               .HasForeignKey(p => p.CourseId)
               .IsRequired(false);

            // 設置 TblTeacherSettlement 與 TblUser (TeacherId) 一對一關聯
            // TeacherId 是外鍵,在 TblTeacherSettlement 端,已設 UNIQUE 約束
            modelBuilder.Entity<TblTeacherSettlement>()
                .HasOne(ts => ts.Teacher)
                .WithOne()
                .HasForeignKey<TblTeacherSettlement>(ts => ts.TeacherId);

            // 設置 TblCourseFee 與 TblCourse (CourseId) 一對一關聯
            // CourseId 是外鍵,在 TblCourseFee 端,已設 UNIQUE 約束
            modelBuilder.Entity<TblCourseFee>()
                .HasOne(f => f.Course)
                .WithOne(c => c.CourseFee)
                .HasForeignKey<TblCourseFee>(f => f.CourseId);

            // 設置 TblStudentPermissionFee 與 TblStudentPermission (StudentPermissionId) 一對一關聯
            // StudentPermissionId 是外鍵,在 TblStudentPermissionFee 端,已設 UNIQUE 約束
            modelBuilder.Entity<TblStudentPermissionFee>()
                .HasOne(f => f.StudentPermission)
                .WithOne(sp => sp.StudentPermissionFee)
                .HasForeignKey<TblStudentPermissionFee>(f => f.StudentPermissionId);

            // 設置 TblAttendanceFee 與 TblAttendance (AttendanceId) 一對一關聯
            // AttendanceId 是外鍵,在 TblAttendanceFee 端,已設 UNIQUE 約束
            modelBuilder.Entity<TblAttendanceFee>()
                .HasOne(f => f.Attendance)
                .WithOne(a => a.AttendanceFee)
                .HasForeignKey<TblAttendanceFee>(f => f.AttendanceId);

            // 設置 TblStudentPermission 與 TblAttendance (StudentPermissionId) 一對多關聯
            modelBuilder.Entity<TblStudentPermission>()
                .HasMany(u => u.Attendances)
                .WithOne(a => a.StudentPermission)
                .HasForeignKey(a => a.StudentPermissionId);

            // 設置 TblStudentPermission 與 TblPayment (StudentPermissionId) 一對多關聯
            modelBuilder.Entity<TblStudentPermission>()
                .HasMany(c => c.Payments)
                .WithOne(a => a.StudentPermission)
                .HasForeignKey(a => a.StudentPermissionId);

            // 設置 TblCourseType 與 TblCourse (CourseTypeId) 一對多關聯
            modelBuilder.Entity<TblCourseType>()
                .HasMany(c => c.Courses)
                .WithOne(a => a.CourseType)
                .HasForeignKey(a => a.CourseTypeId);

            // 設置 TblStudentPermission 與 TblSchedule (StudentPermissionId) 一對多關聯
            modelBuilder.Entity<TblStudentPermission>()
                .HasMany(sp => sp.Schedules)
                .WithOne(s => s.StudentPermission)
                .HasForeignKey(s => s.StudentPermissionId);

            // 設置 TblClassroom 與 TblSchedule (ClassroomId) 一對多關聯
            modelBuilder.Entity<TblClassroom>()
                .HasMany(c => c.Schedules)
                .WithOne(s => s.Classroom)
                .HasForeignKey(s => s.ClassroomId);

            #endregion
        }
    }
}