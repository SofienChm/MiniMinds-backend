using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaycareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureAndEnrollmentDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePicture",
                table: "Parents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentDate",
                table: "Children",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "EnrollmentDate",
                table: "Children");
        }
    }
}