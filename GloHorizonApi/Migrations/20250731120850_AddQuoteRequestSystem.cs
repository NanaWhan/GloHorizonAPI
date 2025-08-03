using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GloHorizonApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteRequestSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    ServiceType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TravelDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    QuotedAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    QuoteProvidedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    QuoteExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PaymentLinkUrl = table.Column<string>(type: "text", nullable: true),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BookingRequestId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SpecialRequests = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdminNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Urgency = table.Column<int>(type: "integer", nullable: false),
                    FlightDetails = table.Column<string>(type: "jsonb", nullable: true),
                    HotelDetails = table.Column<string>(type: "jsonb", nullable: true),
                    TourDetails = table.Column<string>(type: "jsonb", nullable: true),
                    VisaDetails = table.Column<string>(type: "jsonb", nullable: true),
                    PackageDetails = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteRequests_BookingRequests_BookingRequestId",
                        column: x => x.BookingRequestId,
                        principalTable: "BookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuoteRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuoteStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuoteRequestId = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteStatusHistories_QuoteRequests_QuoteRequestId",
                        column: x => x.QuoteRequestId,
                        principalTable: "QuoteRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_BookingRequestId",
                table: "QuoteRequests",
                column: "BookingRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ContactEmail",
                table: "QuoteRequests",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ReferenceNumber",
                table: "QuoteRequests",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ServiceType",
                table: "QuoteRequests",
                column: "ServiceType");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_Status",
                table: "QuoteRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_Status_CreatedAt",
                table: "QuoteRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_UserId_CreatedAt",
                table: "QuoteRequests",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteStatusHistories_QuoteRequestId",
                table: "QuoteStatusHistories",
                column: "QuoteRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteStatusHistories");

            migrationBuilder.DropTable(
                name: "QuoteRequests");
        }
    }
}
