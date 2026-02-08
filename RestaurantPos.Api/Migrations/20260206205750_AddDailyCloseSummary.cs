using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPos.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyCloseSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyCloseSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OrdersToday = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TopItemsJson = table.Column<string>(type: "TEXT", nullable: false),
                    RevenueByMethodJson = table.Column<string>(type: "TEXT", nullable: false),
                    BusiestHour = table.Column<int>(type: "INTEGER", nullable: true),
                    BusiestHourOrders = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyCloseSummaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyCloseSummaries_DateUtc",
                table: "DailyCloseSummaries",
                column: "DateUtc",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyCloseSummaries");
        }
    }
}
