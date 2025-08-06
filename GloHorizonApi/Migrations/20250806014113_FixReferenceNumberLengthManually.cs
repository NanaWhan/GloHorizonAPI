using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloHorizonApi.Migrations
{
    /// <inheritdoc />
    public partial class FixReferenceNumberLengthManually : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Force update ReferenceNumber column to 50 characters
            migrationBuilder.Sql("ALTER TABLE \"BookingRequests\" ALTER COLUMN \"ReferenceNumber\" TYPE character varying(50);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
