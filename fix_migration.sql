-- Mark migration as completed since ReferenceNumber update was successful
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250806013128_IncreaseReferenceNumberLength', '8.0.7')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verify the ReferenceNumber column constraint
SELECT 
    column_name, 
    data_type, 
    character_maximum_length,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'BookingRequests' 
AND column_name = 'ReferenceNumber';

-- Check migration history
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
ORDER BY "MigrationId" DESC 
LIMIT 5;