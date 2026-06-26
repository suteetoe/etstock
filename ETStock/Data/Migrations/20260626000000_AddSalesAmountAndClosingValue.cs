using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETStock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesAmountAndClosingValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SalesAmount",
                table: "MonthlyStocks",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ClosingValue",
                table: "MonthlyStocks",
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
                name: "SalesAmount",
                table: "MonthlyStocks");

            migrationBuilder.DropColumn(
                name: "ClosingValue",
                table: "MonthlyStocks");
        }
    }
}
