using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceRequestApp.Migrations
{
    /// <inheritdoc />
    public partial class NewFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePaths",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmount",
                table: "ServiceRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTransactionId",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ServiceRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Zipcode",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "Reviews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessCredentials",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessImagePath",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Zipcode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceOffered",
                table: "AcceptedRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceTime",
                table: "AcceptedRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ImagePaths",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "PaymentAmount",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "Zipcode",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "BusinessCredentials",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BusinessImagePath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Zipcode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PriceOffered",
                table: "AcceptedRequests");

            migrationBuilder.DropColumn(
                name: "ServiceTime",
                table: "AcceptedRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
