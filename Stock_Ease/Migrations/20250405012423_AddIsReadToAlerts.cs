using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stock_Ease.Migrations
{
    /// <inheritdoc />
    public partial class AddIsReadToAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Alerts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Alerts");
        }
    }
}
