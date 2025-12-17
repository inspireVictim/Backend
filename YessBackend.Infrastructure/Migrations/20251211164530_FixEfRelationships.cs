using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YessBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEfRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_transactions_TransactionId",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_partner_products_partners_PartnerId1",
                table: "partner_products");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_orders_OrderId",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_OrderId",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_partner_products_PartnerId1",
                table: "partner_products");

            migrationBuilder.DropIndex(
                name: "IX_orders_TransactionId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "PartnerId1",
                table: "partner_products");

            migrationBuilder.CreateIndex(
                name: "IX_orders_TransactionId",
                table: "orders",
                column: "TransactionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_transactions_TransactionId",
                table: "orders",
                column: "TransactionId",
                principalTable: "transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_transactions_TransactionId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_TransactionId",
                table: "orders");

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PartnerId1",
                table: "partner_products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_OrderId",
                table: "transactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_partner_products_PartnerId1",
                table: "partner_products",
                column: "PartnerId1");

            migrationBuilder.CreateIndex(
                name: "IX_orders_TransactionId",
                table: "orders",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_transactions_TransactionId",
                table: "orders",
                column: "TransactionId",
                principalTable: "transactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_partner_products_partners_PartnerId1",
                table: "partner_products",
                column: "PartnerId1",
                principalTable: "partners",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_orders_OrderId",
                table: "transactions",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
