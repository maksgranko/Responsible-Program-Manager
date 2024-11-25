
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Responsible_Program_Manager
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string databaseFilePath)
        {
            _connectionString = $"Data Source={databaseFilePath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS FileSystemItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CodeName TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        Publisher TEXT,
                        InstalledVersion TEXT,
                        Version TEXT,
                        IconPath TEXT,
                        InstallArguments TEXT NOT NULL
                    );
                ";

                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddFileSystemItem(string codeName, string name, string publisher, string installedVersion, string version, string iconPath, string installArguments)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string insertQuery = @"
                    INSERT INTO FileSystemItems (CodeName, Name, Publisher, InstalledVersion, Version, IconPath, InstallArguments)
                    VALUES (@CodeName, @Name, @Publisher, @InstalledVersion, @Version, @IconPath, @InstallArguments);
                ";

                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@CodeName", codeName);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Publisher", publisher ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@InstalledVersion", installedVersion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Version", version ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IconPath", iconPath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@InstallArguments", installArguments);

                    command.ExecuteNonQuery();
                }
            }
        }

        public List<FileSystemItem> GetAllFileSystemItems()
        {
            var items = new List<FileSystemItem>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string selectQuery = "SELECT * FROM FileSystemItems;";

                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new FileSystemItem
                            {
                                CodeName = reader["CodeName"].ToString(),
                                Name = reader["Name"].ToString(),
                                Publisher = reader["Publisher"]?.ToString(),
                                InstalledVersion = reader["InstalledVersion"]?.ToString(),
                                Version = reader["Version"]?.ToString(),
                                IconPath = reader["IconPath"]?.ToString(),
                                InstallArguments = reader["InstallArguments"].ToString()
                            });
                        }
                    }
                }
            }

            return items;
        }

        public List<FileSystemItem> GetFileSystemItemsBatch(int offset, int limit)
        {
            var items = new List<FileSystemItem>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string selectQuery = $@"
                    SELECT * FROM FileSystemItems
                    LIMIT {limit} OFFSET {offset};
                ";

                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new FileSystemItem
                            {
                                CodeName = reader["CodeName"].ToString(),
                                Name = reader["Name"].ToString(),
                                Publisher = reader["Publisher"]?.ToString(),
                                InstalledVersion = reader["InstalledVersion"]?.ToString(),
                                Version = reader["Version"]?.ToString(),
                                IconPath = reader["IconPath"]?.ToString(),
                                InstallArguments = reader["InstallArguments"].ToString()
                            });
                        }
                    }
                }
            }

            return items;
        }
    }
}
