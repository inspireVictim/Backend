using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace YessBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DeviceTokens",
                table: "users",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Promotions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Promotions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Promotions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "Promotions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderAmount",
                table: "Promotions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Promotions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Promotions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "Promotions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsageLimit",
                table: "Promotions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageLimitPerUser",
                table: "Promotions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReconciliationReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    PaymentCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerEmployees_UserId",
                table: "PartnerEmployees",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartnerEmployees_users_UserId",
                table: "PartnerEmployees",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartnerEmployees_users_UserId",
                table: "PartnerEmployees");

            migrationBuilder.DropTable(
                name: "ReconciliationReports");

            migrationBuilder.DropIndex(
                name: "IX_PartnerEmployees_UserId",
                table: "PartnerEmployees");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MinOrderAmount",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "UsageLimit",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "UsageLimitPerUser",
                table: "Promotions");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceTokens",
                table: "users",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "[]");
        }
    }
}
