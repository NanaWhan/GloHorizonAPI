-- Update Users table schema to match the current model

-- First, add the new columns that are missing
ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "FirstName" character varying(50),
ADD COLUMN IF NOT EXISTS "LastName" character varying(50),
ADD COLUMN IF NOT EXISTS "ProfileImageUrl" character varying(500),
ADD COLUMN IF NOT EXISTS "Address" character varying(200),
ADD COLUMN IF NOT EXISTS "City" character varying(100),
ADD COLUMN IF NOT EXISTS "Country" character varying(100),
ADD COLUMN IF NOT EXISTS "DateOfBirth" timestamp without time zone;

-- Update existing data: split FullName into FirstName and LastName where possible
UPDATE "Users" 
SET 
    "FirstName" = COALESCE(SPLIT_PART("FullName", ' ', 1), ''),
    "LastName" = COALESCE(CASE 
        WHEN ARRAY_LENGTH(STRING_TO_ARRAY("FullName", ' '), 1) > 1 
        THEN SUBSTRING("FullName" FROM POSITION(' ' IN "FullName") + 1)
        ELSE ''
    END, '')
WHERE "FirstName" IS NULL OR "LastName" IS NULL;

-- Set NOT NULL constraints for required fields
ALTER TABLE "Users" ALTER COLUMN "FirstName" SET NOT NULL;
ALTER TABLE "Users" ALTER COLUMN "LastName" SET NOT NULL;

-- Drop the FullName column if it exists
ALTER TABLE "Users" DROP COLUMN IF EXISTS "FullName";

-- Insert the migration record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250730120000_UpdateUserSchema', '7.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;