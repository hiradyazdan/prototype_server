using Microsoft.EntityFrameworkCore.Migrations;

namespace prototype_server.DB.Migrations
{
    public partial class UpdatePlayerAttributes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Player",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Player",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
