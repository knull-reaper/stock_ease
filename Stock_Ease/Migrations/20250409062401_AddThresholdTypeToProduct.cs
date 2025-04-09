using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stock_Ease.Migrations
{
    /// <inheritdoc />
    public partial class AddThresholdTypeToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThresholdType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThresholdType",
                table: "Products");
        }
    }
}
