using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceRequestApp.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "ServiceRequestId1",
                table: "Messages",
                type: "int",
                nullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_ServiceRequests_ServiceRequestId1",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ServiceRequestId1",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ServiceRequestId1",
                table: "Messages");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
