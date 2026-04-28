using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiosk.Migrations
{
    /// <inheritdoc />
    public partial class SqliteBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cassette",
                columns: table => new
                {
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    DEVICE_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    SLOT = table.Column<int>(type: "INTEGER", nullable: false),
                    CURRENCY_CODE = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DENOMINATION = table.Column<decimal>(type: "TEXT", nullable: false),
                    CAPACITY = table.Column<int>(type: "INTEGER", nullable: false),
                    CURRENT_COUNT = table.Column<int>(type: "INTEGER", nullable: false),
                    VLD = table.Column<bool>(type: "INTEGER", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cassette", x => new { x.KIOSK_ID, x.DEVICE_ID, x.SLOT });
                });

            migrationBuilder.CreateTable(
                name: "deposit_denom_attribute",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CURRENCY_CODE = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    VALUE = table.Column<decimal>(type: "TEXT", nullable: false),
                    ATTRIBUTE_CODE = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    VLD = table.Column<bool>(type: "INTEGER", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposit_denom_attribute", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "device_catalog",
                columns: table => new
                {
                    catalog_id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    vendor = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    model = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    driver_type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    device_type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_catalog", x => x.catalog_id);
                });

            migrationBuilder.CreateTable(
                name: "device_command_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    device_name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    command_name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    success = table.Column<bool>(type: "INTEGER", nullable: false),
                    error_code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    origin = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    finished_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    duration_ms = table.Column<long>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_command_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device_status_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    kiosk_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    device_name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    device_type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_status_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kiosk",
                columns: table => new
                {
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    KIOSK_PID = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VLD = table.Column<bool>(type: "INTEGER", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiosk", x => x.KIOSK_ID);
                });

            migrationBuilder.CreateTable(
                name: "kiosk_shop",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    INFO_LOCALE = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    INFO_KEY = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    INFO_VALUE = table.Column<string>(type: "TEXT", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiosk_shop", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "locale_info",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LANGUAGE_CODE = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    COUNTRY_CODE = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    CULTURE_CODE = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    LANGUAGE_NAME = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LANGUAGE_NAME_KO = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LANGUAGE_NAME_EN = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    COUNTRY_NAME_KO = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    COUNTRY_NAME_EN = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locale_info", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    SERVER_NAME = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SERVER_URL = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    SERVER_KEY = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    TIMEOUT_SECONDS = table.Column<int>(type: "INTEGER", nullable: true),
                    VLD = table.Column<bool>(type: "INTEGER", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "transaction_outbox",
                columns: table => new
                {
                    ID = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KIOSK_ID = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TRANSACTION_ID = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    MESSAGE_TYPE = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PAYLOAD_JSON = table.Column<string>(type: "TEXT", nullable: true),
                    STATUS = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false, defaultValue: "PENDING"),
                    RETRY_COUNT = table.Column<int>(type: "INTEGER", nullable: false),
                    NEXT_RETRY_AT = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LAST_TRIED_AT = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_outbox", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "device_instance",
                columns: table => new
                {
                    device_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    kiosk_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    device_name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    catalog_id = table.Column<long>(type: "INTEGER", nullable: false),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_instance", x => x.device_id);
                    table.ForeignKey(
                        name: "device_instance_ibfk_1",
                        column: x => x.catalog_id,
                        principalTable: "device_catalog",
                        principalColumn: "catalog_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_comm",
                columns: table => new
                {
                    device_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    comm_type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    comm_port = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    comm_params = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    polling_ms = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_comm", x => x.device_id);
                    table.ForeignKey(
                        name: "device_comm_ibfk_1",
                        column: x => x.device_id,
                        principalTable: "device_instance",
                        principalColumn: "device_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_DENOM_ATTR",
                table: "deposit_denom_attribute",
                columns: new[] { "KIOSK_ID", "CURRENCY_CODE", "VALUE", "ATTRIBUTE_CODE" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_vendor_model_driver",
                table: "device_catalog",
                columns: new[] { "vendor", "model", "driver_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_instance_catalog_id",
                table: "device_instance",
                column: "catalog_id");

            migrationBuilder.CreateIndex(
                name: "uq_instance_kiosk_name",
                table: "device_instance",
                columns: new[] { "kiosk_id", "device_id", "device_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "KIOSK_ID",
                table: "kiosk_shop",
                column: "KIOSK_ID");

            migrationBuilder.CreateIndex(
                name: "UQ_LOCALE_CODE",
                table: "locale_info",
                column: "CULTURE_CODE",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_LOCALE_NAMES",
                table: "locale_info",
                columns: new[] { "LANGUAGE_NAME_KO", "LANGUAGE_NAME_EN", "COUNTRY_NAME_KO", "COUNTRY_NAME_EN" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_outbox_transaction_id",
                table: "transaction_outbox",
                column: "TRANSACTION_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cassette");

            migrationBuilder.DropTable(
                name: "deposit_denom_attribute");

            migrationBuilder.DropTable(
                name: "device_comm");

            migrationBuilder.DropTable(
                name: "device_command_log");

            migrationBuilder.DropTable(
                name: "device_status_log");

            migrationBuilder.DropTable(
                name: "kiosk");

            migrationBuilder.DropTable(
                name: "kiosk_shop");

            migrationBuilder.DropTable(
                name: "locale_info");

            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.DropTable(
                name: "transaction_outbox");

            migrationBuilder.DropTable(
                name: "device_instance");

            migrationBuilder.DropTable(
                name: "device_catalog");
        }
    }
}
