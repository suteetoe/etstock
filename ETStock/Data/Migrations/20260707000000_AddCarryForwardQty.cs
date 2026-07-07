using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETStock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCarryForwardQty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "carry_forward_qty",
                table: "monthly_stocks",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "carry_forward_qty",
                table: "monthly_stocks");
        }
    }
}
