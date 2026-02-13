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
                name: "alerts",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sensor_readings",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    temperature = table.Column<double>(type: "double precision", nullable: true),
                    humidity = table.Column<double>(type: "double precision", nullable: true),
                    soil_moisture = table.Column<double>(type: "double precision", nullable: true),
                    rainfall = table.Column<double>(type: "double precision", nullable: true),
                    battery_level = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_readings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sensors",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    battery = table.Column<double>(type: "double precision", nullable: false),
                    last_reading_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_temperature = table.Column<double>(type: "double precision", nullable: true),
                    last_humidity = table.Column<double>(type: "double precision", nullable: true),
                    last_soil_moisture = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensors", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_sensor_created",
                schema: "public",
                table: "alerts",
                columns: new[] { "sensor_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_status",
                schema: "public",
                table: "alerts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_readings_plot_id_time",
                schema: "public",
                table: "sensor_readings",
                columns: new[] { "plot_id", "time" });

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
                name: "ix_sensors_plot_id",
                schema: "public",
                table: "sensors",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_sensor_id",
                schema: "public",
                table: "sensors",
                column: "sensor_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sensor_readings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sensors",
                schema: "public");
        }
    }
}
