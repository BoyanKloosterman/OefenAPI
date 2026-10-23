using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using OefenApi.Auth;
using OefenApi.Models;

namespace OefenApi.Data;

internal sealed record StoredUser(
    int Id,
    string Username,
    string Role,
    string DisplayName,
    string Email,
    string PasswordHash,
    string PasswordSalt);

internal sealed record ClientRow(int Id, int OwnerUserId, string Name, string Email, string Note);

internal sealed class AppDatabase
{
    private readonly string _path;

    public AppDatabase(string path)
    {
        _path = path;
    }

    public void ResetAndSeed()
    {
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Elke start begint met dezelfde oefendata.
        foreach (string extra in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            if (File.Exists(extra))
            {
                File.Delete(extra);
            }
        }

        using SqliteConnection connection = Open();
        using SqliteTransaction transaction = connection.BeginTransaction();
        Execute(connection, transaction, """
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                password_salt TEXT NOT NULL,
                role TEXT NOT NULL,
                display_name TEXT NOT NULL,
                email TEXT NOT NULL
            );

            CREATE TABLE clients (
                id INTEGER PRIMARY KEY,
                owner_user_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                note TEXT NOT NULL,
                FOREIGN KEY (owner_user_id) REFERENCES users (id) ON DELETE CASCADE
            );

            CREATE TABLE sessions (
                token_hash TEXT PRIMARY KEY,
                user_id INTEGER NOT NULL,
                expires_utc TEXT NOT NULL,
                FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
            );

            CREATE TABLE bulk_items (
                id INTEGER PRIMARY KEY,
                title TEXT NOT NULL,
                description TEXT NOT NULL
            );
            """);

        SeedUsers(connection, transaction);
        SeedClients(connection, transaction);
        SeedBulk(connection, transaction);
        transaction.Commit();
    }

    public StoredUser? FindUserByUsername(string username)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, username, role, display_name, email, password_hash, password_salt
            FROM users
            WHERE username = $username
            """;
        command.Parameters.AddWithValue("$username", username);

        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return ReadStoredUser(reader);
    }

    public string CreateSession(int userId)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        string tokenHash = HashToken(token);
        string expires = DateTimeOffset.UtcNow.AddHours(8).ToString("O", CultureInfo.InvariantCulture);

        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sessions (token_hash, user_id, expires_utc)
            VALUES ($tokenHash, $userId, $expires)
            """;
        command.Parameters.AddWithValue("$tokenHash", tokenHash);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$expires", expires);
        command.ExecuteNonQuery();
        return token;
    }

    public Account? FindAccountByToken(string token)
    {
        string tokenHash = HashToken(token);
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT users.id, users.username, users.role, users.display_name, users.email, sessions.expires_utc
            FROM sessions
            INNER JOIN users ON users.id = sessions.user_id
            WHERE sessions.token_hash = $tokenHash
            """;
        command.Parameters.AddWithValue("$tokenHash", tokenHash);

        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        string expiresText = reader.GetString(5);
        DateTimeOffset expires = DateTimeOffset.Parse(expiresText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (expires <= DateTimeOffset.UtcNow)
        {
            reader.Close();
            DeleteSession(connection, tokenHash);
            return null;
        }

        return new Account(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4));
    }

    public ClientRow? GetClient(int id)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, owner_user_id, name, email, note
            FROM clients
            WHERE id = $id
            """;
        command.Parameters.AddWithValue("$id", id);

        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return ReadClient(reader);
    }

    public IReadOnlyList<ClientRow> SearchClients(int ownerId, string? search, bool secureMode)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();

        if (string.IsNullOrEmpty(search))
        {
            command.CommandText = """
                SELECT id, owner_user_id, name, email, note
                FROM clients
                WHERE owner_user_id = $owner
                ORDER BY id
                """;
            command.Parameters.AddWithValue("$owner", ownerId);
        }
        else if (secureMode)
        {
            command.CommandText = """
                SELECT id, owner_user_id, name, email, note
                FROM clients
                WHERE owner_user_id = $owner
                  AND name LIKE $term ESCAPE '\'
                ORDER BY id
                """;
            command.Parameters.AddWithValue("$owner", ownerId);
            command.Parameters.AddWithValue("$term", "%" + EscapeLike(search) + "%");
        }
        else
        {
            command.CommandText =
                "SELECT id, owner_user_id, name, email, note FROM clients WHERE owner_user_id = "
                + ownerId.ToString(CultureInfo.InvariantCulture)
                + " AND name LIKE '%"
                + search
                + "%' ORDER BY id";
        }

        return ReadClients(command);
    }

    public StoredUser? GetUser(int id)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, username, role, display_name, email, password_hash, password_salt
            FROM users
            WHERE id = $id
            """;
        command.Parameters.AddWithValue("$id", id);

        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return ReadStoredUser(reader);
    }

    public bool UpdateUser(int id, string? displayName, string? email, string? role)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE users
            SET display_name = COALESCE($displayName, display_name),
                email = COALESCE($email, email),
                role = COALESCE($role, role)
            WHERE id = $id
            """;
        command.Parameters.AddWithValue("$displayName", (object?)displayName ?? DBNull.Value);
        command.Parameters.AddWithValue("$email", (object?)email ?? DBNull.Value);
        command.Parameters.AddWithValue("$role", (object?)role ?? DBNull.Value);
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    public bool DeleteUser(int id)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM users WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    public (int Total, IReadOnlyList<BulkItemResponse> Items) GetBulk(int pageSize, long offset)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand count = connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM bulk_items";
        int total = Convert.ToInt32(count.ExecuteScalar(), CultureInfo.InvariantCulture);

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, title, description
            FROM bulk_items
            ORDER BY id
            LIMIT $limit OFFSET $offset
            """;
        command.Parameters.AddWithValue("$limit", pageSize);
        command.Parameters.AddWithValue("$offset", offset);

        var items = new List<BulkItemResponse>();
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new BulkItemResponse(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        return (total, items);
    }

    private static void SeedUsers(SqliteConnection connection, SqliteTransaction transaction)
    {
        (int Id, string Username, string Password, string Role, string DisplayName, string Email)[] users =
        [
            (1, DemoAccounts.AccountAUsername, DemoAccounts.AccountAPassword, "user", "Account A", "account-a@example.test"),
            (2, DemoAccounts.AccountBUsername, DemoAccounts.AccountBPassword, "user", "Account B", "account-b@example.test"),
            (3, DemoAccounts.AdminUsername, DemoAccounts.AdminPassword, "admin", "Admin", "admin@example.test")
        ];

        foreach ((int id, string username, string password, string role, string displayName, string email) in users)
        {
            string salt = PasswordHasher.CreateSalt();
            string hash = PasswordHasher.Hash(password, salt);
            Execute(
                connection,
                transaction,
                """
                INSERT INTO users (id, username, password_hash, password_salt, role, display_name, email)
                VALUES ($id, $username, $hash, $salt, $role, $displayName, $email)
                """,
                ("$id", id),
                ("$username", username),
                ("$hash", hash),
                ("$salt", salt),
                ("$role", role),
                ("$displayName", displayName),
                ("$email", email));
        }
    }

    private static void SeedClients(SqliteConnection connection, SqliteTransaction transaction)
    {
        (int Id, int OwnerId, string Name, string Email, string Note)[] clients =
        [
            (1, 1, "Kaasboerderij De Holle Bol", "hollebol@example.test", "Nepklant van account A."),
            (2, 1, "Atelier Linde", "linde@example.test", "Nepklant van account A."),
            (3, 2, "Fietsenwerkplaats Het Wiel", "wiel@example.test", "Nepklant van account B."),
            (4, 2, "Boekhandel Pagina", "pagina@example.test", "Nepklant van account B.")
        ];

        foreach ((int id, int ownerId, string name, string email, string note) in clients)
        {
            Execute(
                connection,
                transaction,
                """
                INSERT INTO clients (id, owner_user_id, name, email, note)
                VALUES ($id, $ownerId, $name, $email, $note)
                """,
                ("$id", id),
                ("$ownerId", ownerId),
                ("$name", name),
                ("$email", email),
                ("$note", note));
        }
    }

    private static void SeedBulk(SqliteConnection connection, SqliteTransaction transaction)
    {
        for (int i = 1; i <= 250; i++)
        {
            Execute(
                connection,
                transaction,
                """
                INSERT INTO bulk_items (id, title, description)
                VALUES ($id, $title, $description)
                """,
                ("$id", i),
                ("$title", "Regel " + i.ToString("000", CultureInfo.InvariantCulture)),
                ("$description", "Oefenregel " + i.ToString(CultureInfo.InvariantCulture) + " voor de paginering."));
        }
    }

    private static void DeleteSession(SqliteConnection connection, string tokenHash)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM sessions WHERE token_hash = $tokenHash";
        command.Parameters.AddWithValue("$tokenHash", tokenHash);
        command.ExecuteNonQuery();
    }

    private static string HashToken(string token)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private static StoredUser ReadStoredUser(SqliteDataReader reader)
    {
        return new StoredUser(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6));
    }

    private static ClientRow ReadClient(SqliteDataReader reader)
    {
        return new ClientRow(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4));
    }

    private static List<ClientRow> ReadClients(SqliteCommand command)
    {
        var rows = new List<ClientRow>();
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(ReadClient(reader));
        }

        return rows;
    }

    private static void Execute(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection($"Data Source={_path}");
        connection.Open();
        using SqliteCommand pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return connection;
    }
}
