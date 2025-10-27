using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaycareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToChildren_20250115140000 : Migration
    {
        /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Children",
                type: "bit",
                nullable: false,
                defaultValue: true);
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