using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace EduConectaPeru.Data
{
    public class UserRepositoryADO
    {
        private readonly string _connectionString;

        public UserRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<User?> ObtenerUsuarioPorUsernameAsync(string username)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT UserId, Username, PasswordHash, Role, IsActive, CreatedAt FROM Users WHERE Username = @Username AND IsActive = 1";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    PasswordHash = reader.GetString(2),
                    Role = reader.GetString(3),
                    IsActive = reader.GetBoolean(4),
                    CreatedAt = reader.GetDateTime(5)
                };
            }

            return null;
        }

        public async Task<bool> ExisteUsernameAsync(string username)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task AgregarUsuarioAsync(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO Users (Username, PasswordHash, Role, IsActive, CreatedAt) 
                         VALUES (@Username, @PasswordHash, @Role, @IsActive, @CreatedAt)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@Role", user.Role);
            command.Parameters.AddWithValue("@IsActive", user.IsActive);
            command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);

            await command.ExecuteNonQueryAsync();
        }
        public async Task<User?> ValidarLoginAsync(string username, string password)
        {
            var user = await ObtenerUsuarioPorUsernameAsync(username);

            if (user == null)
            {
                return null; 
            }

            bool passwordValido = VerifyPassword(password, user.PasswordHash);

            if (!passwordValido)
            {
                return null; 
            }

            return user; 
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                if (storedHash.Contains(":"))
                {
                    var parts = storedHash.Split(':');
                    if (parts.Length != 2) return false;

                    string salt = parts[0];
                    string hash = parts[1];

                    string passwordWithSalt = salt + password;
                    byte[] hashBytes;
                    using (var sha256 = SHA256.Create())
                    {
                        hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
                    }
                    string computedHash = BitConverter.ToString(hashBytes).Replace("-", "");

                    return hash.Equals(computedHash, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    return storedHash == password;
                }
            }
            catch
            {
                return false;
            }
        }
        public string HashPassword(string password)
        {
           
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = BitConverter.ToString(saltBytes).Replace("-", "");

            
            string passwordWithSalt = salt + password;
            byte[] hashBytes;
            using (var sha256 = SHA256.Create())
            {
                hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
            }
            string hash = BitConverter.ToString(hashBytes).Replace("-", "");

           
            return $"{salt}:{hash}";
        }
    }
}