using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = "User Id=postgres.gkwzymyjlxmlmabjzlid;Password=mezttr8q3x9OuBhQ;Server=aws-0-us-east-2.pooler.supabase.com;Port=5432;Database=postgres";
        
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("🔗 Connected to database");
            
            // Fix ReferenceNumber column constraint
            var fixColumnSql = @"ALTER TABLE ""BookingRequests"" ALTER COLUMN ""ReferenceNumber"" TYPE character varying(50);";
            using (var cmd = new NpgsqlCommand(fixColumnSql, connection))
            {
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("✅ ReferenceNumber column updated to 50 characters");
            }
            
            // Mark first migration as completed
            var insertMigration1 = @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                   VALUES ('20250806013128_IncreaseReferenceNumberLength', '8.0.7')
                                   ON CONFLICT (""MigrationId"") DO NOTHING;";
            using (var cmd = new NpgsqlCommand(insertMigration1, connection))
            {
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("✅ First migration marked as completed");
            }
            
            // Mark second migration as completed
            var insertMigration2 = @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                   VALUES ('20250806014113_FixReferenceNumberLengthManually', '8.0.7')
                                   ON CONFLICT (""MigrationId"") DO NOTHING;";
            using (var cmd = new NpgsqlCommand(insertMigration2, connection))
            {
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Second migration marked as completed");
            }
            
            // Verify column constraint
            var verifySql = @"SELECT column_name, data_type, character_maximum_length, is_nullable
                            FROM information_schema.columns 
                            WHERE table_name = 'BookingRequests' 
                            AND column_name = 'ReferenceNumber';";
            using (var cmd = new NpgsqlCommand(verifySql, connection))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    Console.WriteLine($"🔍 Column: {reader["column_name"]}, Type: {reader["data_type"]}, Max Length: {reader["character_maximum_length"]}");
                }
            }
            
            Console.WriteLine("🎉 Database fix completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}