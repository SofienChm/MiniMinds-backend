using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaycareAPI.Migrations
{
    public partial class AddChildParentManyToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create ChildParents junction table
            migrationBuilder.CreateTable(
                name: "ChildParents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPrimaryContact = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildParents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildParents_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildParents_Parents_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Parents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildParents_ChildId_ParentId",
                table: "ChildParents",
                columns: new[] { "ChildId", "ParentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildParents_ParentId",
                table: "ChildParents",
                column: "ParentId");

            // Migrate existing data from Children table to ChildParents
            migrationBuilder.Sql(@"
                INSERT INTO ChildParents (ChildId, ParentId, IsPrimaryContact, CreatedAt)
                SELECT Id, ParentId, 1, GETUTCDATE()
                FROM Children
                WHERE ParentId IS NOT NULL
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildParents");
        }
    }
}
