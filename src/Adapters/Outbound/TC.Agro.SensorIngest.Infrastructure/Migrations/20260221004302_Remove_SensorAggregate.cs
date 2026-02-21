using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.SensorIngest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Remove_SensorAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sensors",
                schema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sensors",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    battery = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_humidity = table.Column<double>(type: "double precision", nullable: true),
                    last_reading_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_soil_moisture = table.Column<double>(type: "double precision", nullable: true),
                    last_temperature = table.Column<double>(type: "double precision", nullable: true),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensors", x => x.id);
                });

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
    }
}
