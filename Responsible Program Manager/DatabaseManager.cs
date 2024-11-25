
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Security.Policy;
using System.Xml.Linq;

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
                        Categories TEXT,
                        InstallArguments TEXT NOT NULL,
                        DownloadPath TEXT
                    );
                ";

                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }


    

        public void AddFileSystemItem(string codeName, string name, string publisher, string installedVersion, string version, string iconPath, string installArguments, string downloadPath)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string insertQuery = @"
                    INSERT INTO FileSystemItems (CodeName, Name, Publisher, InstalledVersion, Version, IconPath, Categories, InstallArguments, DownloadPath)
                    VALUES (@CodeName, @Name, @Publisher, @InstalledVersion, @Version, @IconPath, @Categories, @InstallArguments, @DownloadPath);
                ";

                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@CodeName", codeName);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Publisher", publisher ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@InstalledVersion", installedVersion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Version", version ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IconPath", iconPath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Categories", DBNull.Value); // Adjusted for simplicity
                    command.Parameters.AddWithValue("@InstallArguments", installArguments);
                    command.Parameters.AddWithValue("@DownloadPath", downloadPath ?? (object)DBNull.Value);

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
                                InstallArguments = reader["InstallArguments"].ToString(),
                                DownloadPath = reader["DownloadPath"]?.ToString() // Новое поле
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
                                InstallArguments = reader["InstallArguments"].ToString(),
                                Categories = reader["Categories"]?.ToString().Split(';'),
                                DownloadPath = reader["DownloadPath"]?.ToString() // Новое поле
                            });
                        }
                    }
                }
            }

            return items;
        }
    }
}
