using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuardianCapitalLLC.Migrations
{
    /// <inheritdoc />
    public partial class sourcesession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "UserSessions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "UserSessions");
        }
    }
}
