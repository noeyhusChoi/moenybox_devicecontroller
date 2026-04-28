using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiosk.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalReferenceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currency",
                columns: table => new
                {
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CURRENCY_CODE = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    CULTURE_CODE = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    CURRENCY_DECIMAL = table.Column<int>(type: "INTEGER", nullable: false),
                    CURRENCY_SYMBOL = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    VLD = table.Column<bool>(type: "INTEGER", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency", x => new { x.KIOSK_ID, x.CURRENCY_CODE });
                });

            migrationBuilder.CreateTable(
                name: "deposit_denom",
                columns: table => new
                {
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CURRENCY_CODE = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    VALUE = table.Column<decimal>(type: "TEXT", nullable: false),
                    VLD = table.Column<bool>(type: "INTEGER", nullable: false),
                    UPDATED_BY = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposit_denom", x => new { x.KIOSK_ID, x.CURRENCY_CODE, x.VALUE });
                });

            migrationBuilder.CreateTable(
                name: "kiosk_update_history",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    UPDATE_NO = table.Column<int>(type: "INTEGER", nullable: false),
                    UPDATE_SOURCE = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    UPDATE_DATETIME = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiosk_update_history", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kiosk_update_history_kiosk_id",
                table: "kiosk_update_history",
                column: "KIOSK_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "currency");

            migrationBuilder.DropTable(
                name: "deposit_denom");

            migrationBuilder.DropTable(
                name: "kiosk_update_history");
        }
    }
}
