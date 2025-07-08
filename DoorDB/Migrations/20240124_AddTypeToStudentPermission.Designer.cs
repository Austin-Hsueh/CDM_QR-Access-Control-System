using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DoorDB.Migrations
{
    [DbContext(typeof(DoorDbContext))]
    [Migration("20240124_AddTypeToStudentPermission")]
    partial class AddTypeToStudentPermission
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0");

            modelBuilder.Entity("DoorDB.TblStudentPermission", b =>
            {
                b.Property<int>("Type")
                    .HasColumnType("int")
                    .HasDefaultValue(1)
                    .HasComment("用途類型 1=上課 2=租借教室");
            });
        }
    }
}