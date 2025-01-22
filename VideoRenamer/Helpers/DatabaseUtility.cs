using Drishya.Properties;
using System.Data.SQLite;

namespace Drishya.Helpers
{
    public class DatabaseUtility
    {
        private readonly string _connectionString;

        public DatabaseUtility()
        {
            _connectionString = Settings.Default.DatabaseConnectionString;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            string createTableQuery = "CREATE TABLE IF NOT EXISTS tags (id INTEGER PRIMARY KEY AUTOINCREMENT, text TEXT UNIQUE);";
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            using var command = new SQLiteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
        }

        public bool InsertTag(string tagText)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                string query = "INSERT INTO tags (text) VALUES (@text)";
                using var command = new SQLiteCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@text", tagText);

                try
                {
                    int result = command.ExecuteNonQuery();
                    transaction.Commit();
                    return result > 0;
                }
                catch (SQLiteException ex) when (ex.ErrorCode == (int)SQLiteErrorCode.Constraint)
                {
                    transaction.Rollback();
                    return false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool UpdateTag(string oldText, string newText)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                string query = "UPDATE tags SET text = @newText WHERE text = @oldText";
                using var command = new SQLiteCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@oldText", oldText);
                command.Parameters.AddWithValue("@newText", newText);

                try
                {
                    int result = command.ExecuteNonQuery();
                    transaction.Commit();
                    return result > 0;
                }
                catch (SQLiteException ex) when (ex.ErrorCode == (int)SQLiteErrorCode.Constraint)
                {
                    transaction.Rollback();
                    return false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool DeleteTag(string tagText)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            string query = "DELETE FROM tags WHERE text = @text";
            using var command = new SQLiteCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@text", tagText);

            try
            {
                int result = command.ExecuteNonQuery();
                transaction.Commit();
                return result > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<string> GetAllTags()
        {
            var tags = new List<string>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            string query = "SELECT text FROM tags ORDER BY text";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                tags.Add(reader.GetString(0));
            }

            return tags;
        }

        public List<string> SearchTags(string searchText)
        {
            var tags = new List<string>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            string query = "SELECT text FROM tags WHERE text LIKE @searchText ORDER BY text";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@searchText", $"%{searchText}%");
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                tags.Add(reader.GetString(0));
            }

            return tags;
        }
    }
}