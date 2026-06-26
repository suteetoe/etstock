using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETStock.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop FK: MonthlyStocks.ProductId → Products
            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyStocks_Products_ProductId",
                table: "MonthlyStocks");

            // 2. Drop FK: AbbrInvoiceItems.ProductId → Products
            migrationBuilder.DropForeignKey(
                name: "FK_AbbrInvoiceItems_Products_ProductId",
                table: "AbbrInvoiceItems");

            // 3. Drop unique index on MonthlyStocks (ProductId, Year, Month)
            migrationBuilder.DropIndex(
                name: "IX_MonthlyStocks_ProductId_Year_Month",
                table: "MonthlyStocks");

            // 4. Drop index on AbbrInvoiceItems.ProductId
            migrationBuilder.DropIndex(
                name: "IX_AbbrInvoiceItems_ProductId",
                table: "AbbrInvoiceItems");

            // 5. Add MonthlyStocks.ProductName
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "MonthlyStocks",
                type: "text",
                nullable: false,
                defaultValue: "");

            // 6. Add MonthlyStocks.Unit
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "MonthlyStocks",
                type: "text",
                nullable: false,
                defaultValue: "");

            // 7. Add MonthlyStocks.CostPrice
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "MonthlyStocks",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // 8. Add MonthlyStocks.SellPrice
            migrationBuilder.AddColumn<decimal>(
                name: "SellPrice",
                table: "MonthlyStocks",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // 9. Add AbbrInvoiceItems.ProductName
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "AbbrInvoiceItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            // 10. Drop MonthlyStocks.ProductId
            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "MonthlyStocks");

            // 11. Drop AbbrInvoiceItems.ProductId
            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "AbbrInvoiceItems");

            // 12. Drop Products table
            migrationBuilder.DropTable(
                name: "Products");

            // 13. Create unique index on (Year, Month, ProductName)
            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStocks_Year_Month_ProductName",
                table: "MonthlyStocks",
                columns: new[] { "Year", "Month", "ProductName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: drop new index
            migrationBuilder.DropIndex(
                name: "IX_MonthlyStocks_Year_Month_ProductName",
                table: "MonthlyStocks");

            // Re-create Products table
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SellPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            // Add back ProductId columns
            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "MonthlyStocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "AbbrInvoiceItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "MonthlyStocks");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "MonthlyStocks");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "MonthlyStocks");

            migrationBuilder.DropColumn(
                name: "SellPrice",
                table: "MonthlyStocks");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "AbbrInvoiceItems");

            // Re-create old unique index
            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStocks_ProductId_Year_Month",
                table: "MonthlyStocks",
                columns: new[] { "ProductId", "Year", "Month" },
                unique: true);

            // Re-create FK index on AbbrInvoiceItems.ProductId
            migrationBuilder.CreateIndex(
                name: "IX_AbbrInvoiceItems_ProductId",
                table: "AbbrInvoiceItems",
                column: "ProductId");

            // Restore FKs
            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyStocks_Products_ProductId",
                table: "MonthlyStocks",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AbbrInvoiceItems_Products_ProductId",
                table: "AbbrInvoiceItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
