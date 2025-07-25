using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fuelflux.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fuel_stations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuel_stations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fuel_tanks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<decimal>(type: "numeric(3,0)", nullable: false),
                    volume = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    fuel_station_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuel_tanks", x => x.id);
                    table.ForeignKey(
                        name: "FK_fuel_tanks_fuel_stations_fuel_station_id",
                        column: x => x.fuel_station_id,
                        principalTable: "fuel_stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pump_controllers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uid = table.Column<string>(type: "text", nullable: false),
                    fuel_station_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pump_controllers", x => x.id);
                    table.ForeignKey(
                        name: "FK_pump_controllers_fuel_stations_fuel_station_id",
                        column: x => x.fuel_station_id,
                        principalTable: "fuel_stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    patronymic = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    allowance = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    uid = table.Column<string>(type: "text", nullable: true),
                    role_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name", "role_id" },
                values: new object[,]
                {
                    { 1, "Администратор системы", 1000 },
                    { 2, "Оператор АЗС", 1 },
                    { 3, "Клиент", 2 },
                    { 4, "Контроллер ТРК", 3 }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "allowance", "email", "first_name", "last_name", "password", "patronymic", "role_id", "uid" },
                values: new object[] { 1, 0m, "maxirmx@sw.consulting", "Maxim", "Samsonov", "$2b$12$eOXzlwFzyGVERe0sNwFeJO5XnvwsjloUpL4o2AIQ8254RT88MnsDi", "", 1, "" });

            migrationBuilder.CreateIndex(
                name: "IX_fuel_tanks_fuel_station_id_number",
                table: "fuel_tanks",
                columns: new[] { "fuel_station_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pump_controllers_fuel_station_id",
                table: "pump_controllers",
                column: "fuel_station_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_uid",
                table: "users",
                column: "uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fuel_tanks");

            migrationBuilder.DropTable(
                name: "pump_controllers");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "fuel_stations");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
