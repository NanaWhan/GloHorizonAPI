-- First, ensure the ReferenceNumber column is properly updated
ALTER TABLE "BookingRequests" ALTER COLUMN "ReferenceNumber" TYPE character varying(50);

-- Then manually insert migration records to mark them as completed
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250806013128_IncreaseReferenceNumberLength', '8.0.7')
ON CONFLICT ("MigrationId") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")  
VALUES ('20250806014113_FixReferenceNumberLengthManually', '8.0.7')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verify the column constraint
SELECT 
    column_name, 
    data_type, 
    character_maximum_length,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'BookingRequests' 
AND column_name = 'ReferenceNumber';