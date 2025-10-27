using Microsoft.EntityFrameworkCore.Migrations;
using Pomelo.EntityFrameworkCore.MySql.Metadata;

#nullable disable

namespace DaycareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidaysTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsRecurring = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RecurrenceType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Color = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays");
        }
    }
}