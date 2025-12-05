using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VzOverFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticatorKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticatorKey",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticatorKey",
                table: "Users");
        }
    }
}
