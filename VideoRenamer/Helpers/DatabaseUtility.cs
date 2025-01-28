using Drishya.Properties;
using System;
using System.Collections.Generic;
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
            const string createTableQuery = "CREATE TABLE IF NOT EXISTS tags (id INTEGER PRIMARY KEY AUTOINCREMENT, text TEXT UNIQUE, regex TEXT);";
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            using var command = new SQLiteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
        }

        public bool InsertTag(string tagText, string regexText)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();
                const string query = "INSERT INTO tags (text, regex) VALUES (@text, @regex)";
                using var command = new SQLiteCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@text", tagText);
                command.Parameters.AddWithValue("@regex", regexText);
                int result = command.ExecuteNonQuery();
                transaction.Commit();
                return result > 0;
            }
            catch (SQLiteException ex) when (ex.ErrorCode == (int)SQLiteErrorCode.Constraint)
            {
                Console.WriteLine($"Error: Duplicate tag entry - {ex.Message}");
                return false;
            }
        }

        public bool UpdateTag(string oldText, string newText, string regexText)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();
                const string query = "UPDATE tags SET text = @newText, regex = @regexText WHERE text = @oldText";
                using var command = new SQLiteCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@oldText", oldText);
                command.Parameters.AddWithValue("@newText", newText);
                command.Parameters.AddWithValue("@regexText", regexText);
                int result = command.ExecuteNonQuery();
                transaction.Commit();
                return result > 0;
            }
            catch (SQLiteException ex) when (ex.ErrorCode == (int)SQLiteErrorCode.Constraint)
            {
                Console.WriteLine($"Error: Duplicate tag entry - {ex.Message}");
                return false;
            }
        }

        public bool DeleteTag(string tagText)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();
                const string query = "DELETE FROM tags WHERE text = @text";
                using var command = new SQLiteCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@text", tagText);
                int result = command.ExecuteNonQuery();
                transaction.Commit();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting tag: {ex.Message}");
                return false;
            }
        }

        public List<(string text, string regex)> GetAllTags()
        {
            var tags = new List<(string, string)>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT text, regex FROM tags ORDER BY text";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tags.Add((reader.GetString(0), reader.GetString(1)));
            }
            return tags;
        }

        public List<(string text, string regex)> SearchTags(string searchText)
        {
            var tags = new List<(string, string)>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            const string query = "SELECT text, regex FROM tags WHERE text LIKE @searchText ORDER BY text";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@searchText", $"%{searchText}%");
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tags.Add((reader.GetString(0), reader.GetString(1)));
            }
            return tags;
        }
    }
}
