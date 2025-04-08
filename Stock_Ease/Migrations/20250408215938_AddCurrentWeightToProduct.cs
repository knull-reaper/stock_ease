using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stock_Ease.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentWeightToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CurrentWeight",
                table: "Products",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentWeight",
                table: "Products");
        }
    }
}
