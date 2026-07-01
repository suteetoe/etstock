using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETStock.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbbrInvoiceItems_AbbrInvoices_AbbrInvoiceId",
                table: "AbbrInvoiceItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Companies",
                table: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MonthlyStocks",
                table: "MonthlyStocks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbbrInvoices",
                table: "AbbrInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbbrInvoiceItems",
                table: "AbbrInvoiceItems");

            migrationBuilder.RenameTable(
                name: "Companies",
                newName: "companies");

            migrationBuilder.RenameTable(
                name: "MonthlyStocks",
                newName: "monthly_stocks");

            migrationBuilder.RenameTable(
                name: "AbbrInvoices",
                newName: "abbr_invoices");

            migrationBuilder.RenameTable(
                name: "AbbrInvoiceItems",
                newName: "abbr_invoice_items");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "companies",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "companies",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "companies",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VatRate",
                table: "companies",
                newName: "vat_rate");

            migrationBuilder.RenameColumn(
                name: "TaxId",
                table: "companies",
                newName: "tax_id");

            migrationBuilder.RenameColumn(
                name: "InvoicePrefix",
                table: "companies",
                newName: "invoice_prefix");

            migrationBuilder.RenameColumn(
                name: "BranchName",
                table: "companies",
                newName: "branch_name");

            migrationBuilder.RenameColumn(
                name: "BranchCode",
                table: "companies",
                newName: "branch_code");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "monthly_stocks",
                newName: "year");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "monthly_stocks",
                newName: "unit");

            migrationBuilder.RenameColumn(
                name: "Month",
                table: "monthly_stocks",
                newName: "month");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "monthly_stocks",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SellPrice",
                table: "monthly_stocks",
                newName: "sell_price");

            migrationBuilder.RenameColumn(
                name: "SellPosQty",
                table: "monthly_stocks",
                newName: "sell_pos_qty");

            migrationBuilder.RenameColumn(
                name: "SellFullQty",
                table: "monthly_stocks",
                newName: "sell_full_qty");

            migrationBuilder.RenameColumn(
                name: "SalesAmount",
                table: "monthly_stocks",
                newName: "sales_amount");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "monthly_stocks",
                newName: "product_name");

            migrationBuilder.RenameColumn(
                name: "OpeningQty",
                table: "monthly_stocks",
                newName: "opening_qty");

            migrationBuilder.RenameColumn(
                name: "CostPrice",
                table: "monthly_stocks",
                newName: "cost_price");

            migrationBuilder.RenameColumn(
                name: "ClosingValue",
                table: "monthly_stocks",
                newName: "closing_value");

            migrationBuilder.RenameColumn(
                name: "BuyQty",
                table: "monthly_stocks",
                newName: "buy_qty");

            migrationBuilder.RenameIndex(
                name: "IX_MonthlyStocks_Year_Month_ProductName",
                table: "monthly_stocks",
                newName: "ix_monthly_stocks_year_month_product_name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "abbr_invoices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "abbr_invoices",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "abbr_invoices",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "TaxYear",
                table: "abbr_invoices",
                newName: "tax_year");

            migrationBuilder.RenameColumn(
                name: "TaxMonth",
                table: "abbr_invoices",
                newName: "tax_month");

            migrationBuilder.RenameColumn(
                name: "RunningNo",
                table: "abbr_invoices",
                newName: "running_no");

            migrationBuilder.RenameColumn(
                name: "InvoiceNo",
                table: "abbr_invoices",
                newName: "invoice_no");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "abbr_invoices",
                newName: "invoice_date");

            migrationBuilder.RenameColumn(
                name: "BookNo",
                table: "abbr_invoices",
                newName: "book_no");

            migrationBuilder.RenameColumn(
                name: "Qty",
                table: "abbr_invoice_items",
                newName: "qty");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "abbr_invoice_items",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "abbr_invoice_items",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "abbr_invoice_items",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "abbr_invoice_items",
                newName: "product_name");

            migrationBuilder.RenameColumn(
                name: "AbbrInvoiceId",
                table: "abbr_invoice_items",
                newName: "abbr_invoice_id");

            migrationBuilder.RenameIndex(
                name: "IX_AbbrInvoiceItems_AbbrInvoiceId",
                table: "abbr_invoice_items",
                newName: "ix_abbr_invoice_items_abbr_invoice_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_companies",
                table: "companies",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_monthly_stocks",
                table: "monthly_stocks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_abbr_invoices",
                table: "abbr_invoices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_abbr_invoice_items",
                table: "abbr_invoice_items",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_abbr_invoice_items_abbr_invoices_abbr_invoice_id",
                table: "abbr_invoice_items",
                column: "abbr_invoice_id",
                principalTable: "abbr_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_abbr_invoice_items_abbr_invoices_abbr_invoice_id",
                table: "abbr_invoice_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_companies",
                table: "companies");

            migrationBuilder.DropPrimaryKey(
                name: "pk_monthly_stocks",
                table: "monthly_stocks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_abbr_invoices",
                table: "abbr_invoices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_abbr_invoice_items",
                table: "abbr_invoice_items");

            migrationBuilder.RenameTable(
                name: "companies",
                newName: "Companies");

            migrationBuilder.RenameTable(
                name: "monthly_stocks",
                newName: "MonthlyStocks");

            migrationBuilder.RenameTable(
                name: "abbr_invoices",
                newName: "AbbrInvoices");

            migrationBuilder.RenameTable(
                name: "abbr_invoice_items",
                newName: "AbbrInvoiceItems");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Companies",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "Companies",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Companies",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "vat_rate",
                table: "Companies",
                newName: "VatRate");

            migrationBuilder.RenameColumn(
                name: "tax_id",
                table: "Companies",
                newName: "TaxId");

            migrationBuilder.RenameColumn(
                name: "invoice_prefix",
                table: "Companies",
                newName: "InvoicePrefix");

            migrationBuilder.RenameColumn(
                name: "branch_name",
                table: "Companies",
                newName: "BranchName");

            migrationBuilder.RenameColumn(
                name: "branch_code",
                table: "Companies",
                newName: "BranchCode");

            migrationBuilder.RenameColumn(
                name: "year",
                table: "MonthlyStocks",
                newName: "Year");

            migrationBuilder.RenameColumn(
                name: "unit",
                table: "MonthlyStocks",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "month",
                table: "MonthlyStocks",
                newName: "Month");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "MonthlyStocks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "sell_price",
                table: "MonthlyStocks",
                newName: "SellPrice");

            migrationBuilder.RenameColumn(
                name: "sell_pos_qty",
                table: "MonthlyStocks",
                newName: "SellPosQty");

            migrationBuilder.RenameColumn(
                name: "sell_full_qty",
                table: "MonthlyStocks",
                newName: "SellFullQty");

            migrationBuilder.RenameColumn(
                name: "sales_amount",
                table: "MonthlyStocks",
                newName: "SalesAmount");

            migrationBuilder.RenameColumn(
                name: "product_name",
                table: "MonthlyStocks",
                newName: "ProductName");

            migrationBuilder.RenameColumn(
                name: "opening_qty",
                table: "MonthlyStocks",
                newName: "OpeningQty");

            migrationBuilder.RenameColumn(
                name: "cost_price",
                table: "MonthlyStocks",
                newName: "CostPrice");

            migrationBuilder.RenameColumn(
                name: "closing_value",
                table: "MonthlyStocks",
                newName: "ClosingValue");

            migrationBuilder.RenameColumn(
                name: "buy_qty",
                table: "MonthlyStocks",
                newName: "BuyQty");

            migrationBuilder.RenameIndex(
                name: "ix_monthly_stocks_year_month_product_name",
                table: "MonthlyStocks",
                newName: "IX_MonthlyStocks_Year_Month_ProductName");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AbbrInvoices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "AbbrInvoices",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "AbbrInvoices",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "tax_year",
                table: "AbbrInvoices",
                newName: "TaxYear");

            migrationBuilder.RenameColumn(
                name: "tax_month",
                table: "AbbrInvoices",
                newName: "TaxMonth");

            migrationBuilder.RenameColumn(
                name: "running_no",
                table: "AbbrInvoices",
                newName: "RunningNo");

            migrationBuilder.RenameColumn(
                name: "invoice_no",
                table: "AbbrInvoices",
                newName: "InvoiceNo");

            migrationBuilder.RenameColumn(
                name: "invoice_date",
                table: "AbbrInvoices",
                newName: "InvoiceDate");

            migrationBuilder.RenameColumn(
                name: "book_no",
                table: "AbbrInvoices",
                newName: "BookNo");

            migrationBuilder.RenameColumn(
                name: "qty",
                table: "AbbrInvoiceItems",
                newName: "Qty");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "AbbrInvoiceItems",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AbbrInvoiceItems",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "AbbrInvoiceItems",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "product_name",
                table: "AbbrInvoiceItems",
                newName: "ProductName");

            migrationBuilder.RenameColumn(
                name: "abbr_invoice_id",
                table: "AbbrInvoiceItems",
                newName: "AbbrInvoiceId");

            migrationBuilder.RenameIndex(
                name: "ix_abbr_invoice_items_abbr_invoice_id",
                table: "AbbrInvoiceItems",
                newName: "IX_AbbrInvoiceItems_AbbrInvoiceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Companies",
                table: "Companies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MonthlyStocks",
                table: "MonthlyStocks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbbrInvoices",
                table: "AbbrInvoices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbbrInvoiceItems",
                table: "AbbrInvoiceItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AbbrInvoiceItems_AbbrInvoices_AbbrInvoiceId",
                table: "AbbrInvoiceItems",
                column: "AbbrInvoiceId",
                principalTable: "AbbrInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
