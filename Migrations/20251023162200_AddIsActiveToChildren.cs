using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaycareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Children",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Children");
        }
    }
}
