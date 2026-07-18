using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vss.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddErpConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErpConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SapBaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SapQuerySupplierPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SapManageSupplierPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SapUsername = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SapSampleSupplierId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BcBaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BcCompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BcTenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BcClientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BcScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BcSampleVendorNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErpConfigs");
        }
    }
}
