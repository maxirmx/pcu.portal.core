using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fuelflux.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceUid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "allowance",
                table: "users",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "uid",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_users_uid",
                table: "users",
                column: "uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_uid",
                table: "users");

            migrationBuilder.DropColumn(
                name: "allowance",
                table: "users");

            migrationBuilder.DropColumn(
                name: "uid",
                table: "users");
        }
    }
}

