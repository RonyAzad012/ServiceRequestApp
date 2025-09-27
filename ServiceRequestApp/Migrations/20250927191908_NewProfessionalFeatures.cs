using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceRequestApp.Migrations
{
    /// <inheritdoc />
    public partial class NewProfessionalFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "ServiceRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceRequestId1",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ApplicationUserId",
                table: "ServiceRequests",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ServiceRequestId1",
                table: "Messages",
                column: "ServiceRequestId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_ServiceRequests_ServiceRequestId1",
                table: "Messages",
                column: "ServiceRequestId1",
                principalTable: "ServiceRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_AspNetUsers_ApplicationUserId",
                table: "ServiceRequests",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_ServiceRequests_ServiceRequestId1",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_AspNetUsers_ApplicationUserId",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_ApplicationUserId",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ServiceRequestId1",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ServiceRequestId1",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
