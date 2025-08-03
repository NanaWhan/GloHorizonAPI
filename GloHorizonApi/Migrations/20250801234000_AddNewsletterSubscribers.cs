using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GloHorizonApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterSubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsletterSubscribers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    SubscribedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UnsubscribedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscribers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_IsActive_SubscribedAt",
                table: "NewsletterSubscribers",
                columns: new[] { "IsActive", "SubscribedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_PhoneNumber",
                table: "NewsletterSubscribers",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsletterSubscribers");
        }
    }
}