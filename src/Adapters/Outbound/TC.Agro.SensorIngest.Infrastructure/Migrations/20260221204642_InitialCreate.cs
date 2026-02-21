using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.SensorIngest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "owner_snapshots",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_owner_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sensor_snapshots",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plot_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    property_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_sensor_snapshots_owner_snapshots_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "owner_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sensor_readings",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    temperature = table.Column<double>(type: "double precision", nullable: true),
                    humidity = table.Column<double>(type: "double precision", nullable: true),
                    soil_moisture = table.Column<double>(type: "double precision", nullable: true),
                    rainfall = table.Column<double>(type: "double precision", nullable: true),
                    battery_level = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_readings", x => x.id);
                    table.ForeignKey(
                        name: "fk_sensor_readings_sensor_snapshots_sensor_id",
                        column: x => x.sensor_id,
                        principalSchema: "public",
                        principalTable: "sensor_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_owner_snapshots_email",
                schema: "public",
                table: "owner_snapshots",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensor_readings_sensor_id_time",
                schema: "public",
                table: "sensor_readings",
                columns: new[] { "sensor_id", "time" });

            migrationBuilder.CreateIndex(
                name: "ix_sensor_readings_time",
                schema: "public",
                table: "sensor_readings",
                column: "time");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_snapshots_owner_id",
                schema: "public",
                table: "sensor_snapshots",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_snapshots_owner_id_is_active",
                schema: "public",
                table: "sensor_snapshots",
                columns: new[] { "owner_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_sensor_snapshots_plot_id",
                schema: "public",
                table: "sensor_snapshots",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_snapshots_plot_id_is_active",
                schema: "public",
                table: "sensor_snapshots",
                columns: new[] { "plot_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sensor_readings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sensor_snapshots",
                schema: "public");

            migrationBuilder.DropTable(
                name: "owner_snapshots",
                schema: "public");
        }
    }
}
