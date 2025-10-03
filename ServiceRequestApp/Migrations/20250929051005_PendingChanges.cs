using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceRequestApp.Migrations
{
    /// <inheritdoc/>
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentGatewayResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SSLCommerzBankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSLCommerzBankTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSLCommerzCardNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSLCommerzCardType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSLCommerzRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSLCommerzRiskTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSLCommerzSessionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SSLCommerzTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SSLCommerzValId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_AspNetUsers_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RequesterId",
                table: "Payments",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ServiceRequestId",
                table: "Payments",
                column: "ServiceRequestId");
        }
    }
}
