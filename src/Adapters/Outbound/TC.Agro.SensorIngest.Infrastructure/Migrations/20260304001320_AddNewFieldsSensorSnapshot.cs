using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.SensorIngest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsSensorSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "plot_boundary_geo_json",
                schema: "public",
                table: "sensor_snapshots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "plot_latitude",
                schema: "public",
                table: "sensor_snapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "plot_longitude",
                schema: "public",
                table: "sensor_snapshots",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "plot_boundary_geo_json",
                schema: "public",
                table: "sensor_snapshots");

            migrationBuilder.DropColumn(
                name: "plot_latitude",
                schema: "public",
                table: "sensor_snapshots");

            migrationBuilder.DropColumn(
                name: "plot_longitude",
                schema: "public",
                table: "sensor_snapshots");
        }
    }
}
