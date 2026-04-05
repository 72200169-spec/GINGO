using MySqlConnector;

namespace GinGo.Data;

public sealed class UserRepository
{
    public async Task<CreateUserResult> CreateUserAsync(string username, string email, string? phone, string password)
    {
        username = username.Trim();
        email = email.Trim();
        phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            return new CreateUserResult(false, "El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return new CreateUserResult(false, "El correo electrónico es obligatorio.");
        }

        if (!IsStrongPassword(password))
        {
            return new CreateUserResult(false, "La contraseña no cumple los requisitos de seguridad.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        await using var connection = await DbConnectionFactory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO users (username, email, phone, password_hash)
            VALUES (@username, @email, @phone, @password_hash);
            """;
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@phone", (object?)phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@password_hash", passwordHash);

        try
        {
            var affected = await command.ExecuteNonQueryAsync();
            return affected == 1
                ? new CreateUserResult(true, "OK")
                : new CreateUserResult(false, "No se pudo guardar el usuario.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return new CreateUserResult(false, "El usuario o correo ya existe.");
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string identifier, string password)
    {
        identifier = identifier.Trim();
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        await using var connection = await DbConnectionFactory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT password_hash
            FROM users
            WHERE username = @identifier OR email = @identifier
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@identifier", identifier);

        var passwordHashObj = await command.ExecuteScalarAsync();
        if (passwordHashObj is not string passwordHash || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    private static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            return false;
        }

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));
        return hasLetter && hasDigit && hasSpecial;
    }
}

public readonly record struct CreateUserResult(bool Success, string Message);
