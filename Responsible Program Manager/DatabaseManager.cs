
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net.Http;
using System.Windows;

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
                CodeName TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Publisher TEXT,
                InstalledVersion TEXT,
                Version TEXT,
                IconPath TEXT,
                IconUrl TEXT,
                Categories TEXT,
                InstallArguments TEXT NOT NULL,
                DownloadPath TEXT,
                CachedPath TEXT,
                MD5_hash TEXT
            );
        ";

                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddOrUpdateFileSystemItem(
    string codeName,
    string name,
    string publisher,
    string installedVersion,
    string version,
    string iconUrl,
    string categories,
    string installArguments,
    string downloadPath,
    string MD5_hash
    )
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = @"
        INSERT INTO FileSystemItems 
        (CodeName, Name, Publisher, InstalledVersion, Version, IconUrl, Categories, InstallArguments, DownloadPath, MD5_hash)
        VALUES 
        (
            @CodeName, 
            @Name, 
            @Publisher, 
            @InstalledVersion, 
            @Version,
            @IconUrl, 
            @Categories, 
            @InstallArguments, 
            @DownloadPath,
            @MD5_hash
        )
        ON CONFLICT(CodeName) DO UPDATE SET 
            Name = excluded.Name,
            Publisher = excluded.Publisher,
            InstalledVersion = excluded.InstalledVersion,
            Version = CASE 
                        WHEN FileSystemItems.Version != excluded.Version THEN excluded.Version
                        ELSE FileSystemItems.Version
                     END,
            IconUrl = excluded.IconUrl,
            Categories = excluded.Categories,
            InstallArguments = excluded.InstallArguments,
            DownloadPath = excluded.DownloadPath,
            MD5_hash = excluded.MD5_hash
        WHERE
            FileSystemItems.Version != excluded.Version OR FileSystemItems.MD5_hash != excluded.MD5_hash;
    ";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CodeName", codeName);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Publisher", publisher ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@InstalledVersion", installedVersion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Version", version ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IconUrl", iconUrl ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Categories", categories != null ? string.Join(";", categories) : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@InstallArguments", installArguments ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@DownloadPath", downloadPath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MD5_hash", MD5_hash ?? (object)DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateIconPath(int id, string iconPath)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string updateQuery = @"
                    UPDATE FileSystemItems
                    SET IconPath = @IconPath
                    WHERE Id = @Id;
                ";

                using (var command = new SQLiteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@IconPath", iconPath);
                    command.Parameters.AddWithValue("@Id", id);

                    command.ExecuteNonQuery();
                }
            }
        }

        public List<FileSystemItem> GetAllCachedFileSystemItems()
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
                            var cachedPath = reader["CachedPath"]?.ToString();
                            if (!string.IsNullOrEmpty(cachedPath) && File.Exists(cachedPath))
                            {
                                items.Add(new FileSystemItem
                                {
                                    CodeName = reader["CodeName"].ToString(),
                                    Name = reader["Name"].ToString(),
                                    Publisher = reader["Publisher"]?.ToString(),
                                    InstalledVersion = reader["InstalledVersion"]?.ToString(),
                                    Version = reader["Version"]?.ToString(),
                                    IconPath = reader["IconPath"]?.ToString(),
                                    IconUrl = reader["IconUrl"]?.ToString(),
                                    Categories = reader["Categories"]?.ToString(),
                                    InstallArguments = reader["InstallArguments"]?.ToString(),
                                    DownloadPath = reader["DownloadPath"]?.ToString(),
                                    CachedPath = cachedPath,
                                    MD5_hash = reader["MD5_hash"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }

            return items;
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
                                IconUrl = reader["IconUrl"]?.ToString(),
                                Categories = reader["Categories"]?.ToString(),
                                InstallArguments = reader["InstallArguments"]?.ToString(),
                                DownloadPath = reader["DownloadPath"]?.ToString(),
                                CachedPath = reader["CachedPath"]?.ToString(),
                                MD5_hash = reader["MD5_hash"]?.ToString()
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
                                IconUrl = reader["IconUrl"]?.ToString(),
                                Categories = reader["Categories"]?.ToString(),
                                InstallArguments = reader["InstallArguments"]?.ToString(),
                                DownloadPath = reader["DownloadPath"]?.ToString(),
                                CachedPath = reader["CachedPath"]?.ToString(),
                                MD5_hash = reader["MD5_hash"]?.ToString()
                            });
                        }
                    }
                }
            }

            return items;
        }

        public void UpdateCachedPath(string codeName, string cachedPath)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string updateQuery = @"
            UPDATE FileSystemItems
            SET CachedPath = @CachedPath
            WHERE CodeName = @CodeName;
        ";

                using (var command = new SQLiteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@CachedPath", cachedPath ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CodeName", codeName);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
