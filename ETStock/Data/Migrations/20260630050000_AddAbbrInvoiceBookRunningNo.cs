using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETStock.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAbbrInvoiceBookRunningNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookNo",
                table: "AbbrInvoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RunningNo",
                table: "AbbrInvoices",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookNo",
                table: "AbbrInvoices");

            migrationBuilder.DropColumn(
                name: "RunningNo",
                table: "AbbrInvoices");
        }
    }
}
