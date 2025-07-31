using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GloHorizonApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingSystemWithServiceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingData",
                table: "BookingRequests");

            migrationBuilder.RenameColumn(
                name: "FinalPrice",
                table: "BookingRequests",
                newName: "QuotedAmount");

            migrationBuilder.RenameColumn(
                name: "EstimatedPrice",
                table: "BookingRequests",
                newName: "FinalAmount");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "BookingRequests",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "BookingRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "BookingRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlightDetails",
                table: "BookingRequests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotelDetails",
                table: "BookingRequests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageDetails",
                table: "BookingRequests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialRequests",
                table: "BookingRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TourDetails",
                table: "BookingRequests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TravelDate",
                table: "BookingRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisaDetails",
                table: "BookingRequests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingRequestId = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingDocuments_BookingRequests_BookingRequestId",
                        column: x => x.BookingRequestId,
                        principalTable: "BookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_ServiceType",
                table: "BookingRequests",
                column: "ServiceType");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_Status",
                table: "BookingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDocuments_BookingRequestId_DocumentType",
                table: "BookingDocuments",
                columns: new[] { "BookingRequestId", "DocumentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingDocuments");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_ServiceType",
                table: "BookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_Status",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "Destination",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "FlightDetails",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "HotelDetails",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "PackageDetails",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "SpecialRequests",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "TourDetails",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "TravelDate",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "VisaDetails",
                table: "BookingRequests");

            migrationBuilder.RenameColumn(
                name: "QuotedAmount",
                table: "BookingRequests",
                newName: "FinalPrice");

            migrationBuilder.RenameColumn(
                name: "FinalAmount",
                table: "BookingRequests",
                newName: "EstimatedPrice");

            migrationBuilder.AddColumn<string>(
                name: "BookingData",
                table: "BookingRequests",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}
