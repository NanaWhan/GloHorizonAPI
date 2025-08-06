using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GloHorizonApi.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseReferenceNumberLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "BookingRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "BookingRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            // Skip NewsletterSubscribers table creation - already exists
            // migrationBuilder.CreateTable(
            //     name: "NewsletterSubscribers",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "integer", nullable: false)
            //             .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //         PhoneNumber = table.Column<string>(type: "text", nullable: false),
            //         SubscribedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
            //         IsActive = table.Column<bool>(type: "boolean", nullable: false),
            //         UnsubscribedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
            //         Source = table.Column<string>(type: "text", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_NewsletterSubscribers", x => x.Id);
            //     });

            // migrationBuilder.CreateIndex(
            //     name: "IX_NewsletterSubscribers_IsActive_SubscribedAt",
            //     table: "NewsletterSubscribers",
            //     columns: new[] { "IsActive", "SubscribedAt" });

            // migrationBuilder.CreateIndex(
            //     name: "IX_NewsletterSubscribers_PhoneNumber",
            //     table: "NewsletterSubscribers",
            //     column: "PhoneNumber",
            //     unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Skip dropping NewsletterSubscribers table - it was already there
            // migrationBuilder.DropTable(
            //     name: "NewsletterSubscribers");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "BookingRequests",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "BookingRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
