using Microsoft.Data.Sqlite;

namespace Nager.FileCompressService.Services
{
    public class SqliteFileCompressionHistoryService : IFileCompressionHistoryService
    {
        private readonly string _connectionString;

        public SqliteFileCompressionHistoryService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dbPath = Path.Combine(baseDir, "compression_history.db");

            this._connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath
            }.ToString();

            this.InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(this._connectionString);
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS CompressionHistory (
                FilePath TEXT PRIMARY KEY,
                CompressedAt TEXT NOT NULL
            );";

            command.ExecuteNonQuery();
        }

        public async Task<bool> IsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(this._connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM CompressionHistory WHERE FilePath = $filePath;";
            command.Parameters.AddWithValue("$filePath", GetNormalizedPath(filePath));

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result) > 0;
        }

        public async Task MarkAsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(this._connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            // INSERT OR REPLACE verhindert Fehler, falls die Datei bereits als komprimiert markiert ist
            command.CommandText = @"
            INSERT OR REPLACE INTO CompressionHistory (FilePath, CompressedAt)
            VALUES ($filePath, $compressedAt);";

            command.Parameters.AddWithValue("$filePath", GetNormalizedPath(filePath));
            command.Parameters.AddWithValue("$compressedAt", DateTime.UtcNow.ToString("o"));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task UnmarkAsCompressedAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(this._connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CompressionHistory WHERE FilePath = $filePath;";
            command.Parameters.AddWithValue("$filePath", GetNormalizedPath(filePath));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Normalisiert den Dateipfad (z. B. absolute Pfade), damit Treffer eindeutig bleiben.
        /// </summary>
        private static string GetNormalizedPath(string filePath)
        {
            return Path.GetFullPath(filePath).ToLowerInvariant();
        }
    }
}
