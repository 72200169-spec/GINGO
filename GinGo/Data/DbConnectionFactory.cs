using MySqlConnector;

namespace GinGo.Data;

public static class DbConnectionFactory
{
    public static async Task<MySqlConnection> CreateOpenConnectionAsync()
    {
        var host = GetEnv("GINGO_DB_HOST", "127.0.0.1");
        var port = GetEnvInt("GINGO_DB_PORT", 3306);
        var database = GetEnv("GINGO_DB_NAME", "gingo");
        var user = GetEnv("GINGO_DB_USER", "gingo_app");
        var password = GetEnv("GINGO_DB_PASSWORD", "gingo_password");

        var builder = new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = (uint)port,
            Database = database,
            UserID = user,
            Password = password,
            SslMode = MySqlSslMode.Preferred,
            AllowUserVariables = true
        };

        var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static string GetEnv(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static int GetEnvInt(string key, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var n) ? n : fallback;
    }
}
