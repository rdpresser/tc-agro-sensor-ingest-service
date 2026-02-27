using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.SensorIngest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusChangeReasonFieldToSensorSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status_change_reason",
                schema: "public",
                table: "sensor_snapshots",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status_change_reason",
                schema: "public",
                table: "sensor_snapshots");
        }
    }
}
