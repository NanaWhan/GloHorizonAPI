using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloHorizonApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserModelForAuthSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns first
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptMarketing",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User");

            // Migrate data from FullName to FirstName/LastName if FullName exists
            migrationBuilder.Sql(@"
                UPDATE ""Users"" 
                SET ""FirstName"" = CASE 
                    WHEN ""FullName"" IS NOT NULL AND position(' ' in ""FullName"") > 0 
                    THEN substring(""FullName"" from 1 for position(' ' in ""FullName"") - 1)
                    ELSE COALESCE(""FullName"", 'User')
                END,
                ""LastName"" = CASE 
                    WHEN ""FullName"" IS NOT NULL AND position(' ' in ""FullName"") > 0 
                    THEN substring(""FullName"" from position(' ' in ""FullName"") + 1)
                    ELSE ''
                END
                WHERE ""FullName"" IS NOT NULL;
            ");

            // Drop FullName column if it exists
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'FullName') THEN
                        ALTER TABLE ""Users"" DROP COLUMN ""FullName"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add FullName column back
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Migrate data back from FirstName/LastName to FullName
            migrationBuilder.Sql(@"
                UPDATE ""Users"" 
                SET ""FullName"" = CONCAT(""FirstName"", ' ', ""LastName"")
                WHERE ""FirstName"" IS NOT NULL;
            ");

            // Drop the new columns
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AcceptMarketing",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
