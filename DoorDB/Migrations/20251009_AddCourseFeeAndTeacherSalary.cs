using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DoorWebDB.Migrations
{
    public partial class AddCourseFeeAndTeacherSalary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 建立 tblCourseFee 表
            migrationBuilder.CreateTable(
                name: "tblCourseFee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CourseTypeId = table.Column<int>(type: "int", nullable: false, comment: "課程分類ID ([tblCourseType].[Id])"),
                    FeeName = table.Column<string>(type: "longtext", nullable: false, comment: "收費名稱"),
                    FeeAmount = table.Column<int>(type: "int", nullable: false, comment: "收費金額"),
                    SplitRatio = table.Column<decimal>(type: "decimal(5,2)", nullable: false, comment: "拆帳比例 (0-1)"),
                    LessonCount = table.Column<int>(type: "int", nullable: false, comment: "課堂數"),
                    IsStudentAbsenceNotDeduct = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "學生請假不扣堂"),
                    IsCountTransaction = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "是否計算成交"),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "是否下架"),
                    Sequence = table.Column<int>(type: "int", nullable: false, comment: "堂序"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCourseFee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblCourseFee_tblCourseType_CourseTypeId",
                        column: x => x.CourseTypeId,
                        principalTable: "tblCourseType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // 建立 tblTeacherSalaryDetail 表
            migrationBuilder.CreateTable(
                name: "tblTeacherSalaryDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScheduleId = table.Column<int>(type: "int", nullable: false, comment: "課程排程ID ([tblSchedule].[Id])"),
                    TeacherId = table.Column<int>(type: "int", nullable: false, comment: "老師ID ([tblUser].[Id])"),
                    StudentId = table.Column<int>(type: "int", nullable: false, comment: "學生ID ([tblUser].[Id])"),
                    CourseFeeId = table.Column<int>(type: "int", nullable: false, comment: "收費設定ID ([tblCourseFee].[Id])"),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "單堂價格"),
                    BaseSplitAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "基礎拆帳金額"),
                    FlexibleSplitAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "靈活拆帳金額"),
                    Bonus = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "獎勵金額"),
                    Deduction = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "扣薪金額"),
                    ActualAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "實際薪資"),
                    Discount = table.Column<string>(type: "varchar(50)", nullable: false, comment: "折數"),
                    FlexiblePoints = table.Column<int>(type: "int", nullable: false, comment: "靈活種點"),
                    Points = table.Column<int>(type: "int", nullable: false, comment: "種點"),
                    Notes = table.Column<string>(type: "longtext", nullable: false, comment: "備註"),
                    CreatedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDelete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTeacherSalaryDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblTeacherSalaryDetail_tblSchedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "tblSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblTeacherSalaryDetail_tblUser_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblTeacherSalaryDetail_tblUser_StudentId",
                        column: x => x.StudentId,
                        principalTable: "tblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblTeacherSalaryDetail_tblCourseFee_CourseFeeId",
                        column: x => x.CourseFeeId,
                        principalTable: "tblCourseFee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // 建立索引
            migrationBuilder.CreateIndex(
                name: "IX_tblCourseFee_CourseTypeId",
                table: "tblCourseFee",
                column: "CourseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSalaryDetail_ScheduleId",
                table: "tblTeacherSalaryDetail",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSalaryDetail_TeacherId",
                table: "tblTeacherSalaryDetail",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSalaryDetail_StudentId",
                table: "tblTeacherSalaryDetail",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTeacherSalaryDetail_CourseFeeId",
                table: "tblTeacherSalaryDetail",
                column: "CourseFeeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblTeacherSalaryDetail");

            migrationBuilder.DropTable(
                name: "tblCourseFee");
        }
    }
}
