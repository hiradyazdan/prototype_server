using Microsoft.EntityFrameworkCore.Migrations;

namespace prototype_server.DB.Migrations
{
    public partial class AddGuidToPlayer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GUID",
                table: "Players",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Players_GUID",
                table: "Players",
                column: "GUID",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_GUID",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "GUID",
                table: "Players");
        }
    }
}
