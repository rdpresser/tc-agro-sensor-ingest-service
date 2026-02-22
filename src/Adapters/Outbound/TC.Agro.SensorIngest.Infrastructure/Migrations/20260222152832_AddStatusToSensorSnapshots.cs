using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.SensorIngest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToSensorSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "public",
                table: "sensor_snapshots",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                schema: "public",
                table: "sensor_snapshots");
        }
    }
}
