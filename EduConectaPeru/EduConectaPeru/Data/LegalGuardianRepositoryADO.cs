using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace EduConectaPeru.Data
{
    public class LegalGuardianRepositoryADO
    {
        private readonly string _connectionString;

        public LegalGuardianRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<LegalGuardian>> ObtenerApoderadosAsync()
        {
            var apoderados = new List<LegalGuardian>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT LegalGuardianId, DNI, Nombre, Apellido, Direccion, Telefono, Email, FechaRegistro, IsActive FROM LegalGuardians ORDER BY IsActive DESC, FechaRegistro DESC";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                apoderados.Add(new LegalGuardian
                {
                    LegalGuardianId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    Direccion = reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Email = reader.IsDBNull(6) ? null : reader.GetString(6),
                    FechaRegistro = reader.GetDateTime(7),
                    IsActive = reader.GetBoolean(8)
                });
            }

            return apoderados;
        }

        public async Task ReactivarApoderadoAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE LegalGuardians SET IsActive = 1 WHERE LegalGuardianId = @LegalGuardianId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LegalGuardianId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExisteDNIAsync(string dni, int? legalGuardianIdExcluir = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query;
            if (legalGuardianIdExcluir.HasValue)
            {
                query = "SELECT COUNT(*) FROM LegalGuardians WHERE DNI = @DNI AND LegalGuardianId != @LegalGuardianId";
            }
            else
            {
                query = "SELECT COUNT(*) FROM LegalGuardians WHERE DNI = @DNI";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DNI", dni);

            if (legalGuardianIdExcluir.HasValue)
            {
                command.Parameters.AddWithValue("@LegalGuardianId", legalGuardianIdExcluir.Value);
            }

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<LegalGuardian?> ObtenerApoderadoPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT LegalGuardianId, DNI, Nombre, Apellido, Direccion, Telefono, Email, FechaRegistro, IsActive FROM LegalGuardians WHERE LegalGuardianId = @LegalGuardianId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LegalGuardianId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new LegalGuardian
                {
                    LegalGuardianId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    Direccion = reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Email = reader.IsDBNull(6) ? null : reader.GetString(6),
                    FechaRegistro = reader.GetDateTime(7),
                    IsActive = reader.GetBoolean(8)
                };
            }

            return null;
        }

        public async Task<LegalGuardian?> ObtenerApoderadoPorDNIAsync(string dni)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT LegalGuardianId, DNI, Nombre, Apellido, Direccion, Telefono, Email, FechaRegistro, IsActive FROM LegalGuardians WHERE DNI = @DNI";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DNI", dni);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new LegalGuardian
                {
                    LegalGuardianId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    Direccion = reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Email = reader.IsDBNull(6) ? null : reader.GetString(6),
                    FechaRegistro = reader.GetDateTime(7),
                    IsActive = reader.GetBoolean(8)
                };
            }

            return null;
        }

        public async Task<int> AgregarApoderadoAsync(LegalGuardian apoderado)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO LegalGuardians (DNI, Nombre, Apellido, Direccion, Telefono, Email, FechaRegistro, IsActive)
                         VALUES (@DNI, @Nombre, @Apellido, @Direccion, @Telefono, @Email, @FechaRegistro, @IsActive);
                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DNI", apoderado.DNI);
            command.Parameters.AddWithValue("@Nombre", apoderado.Nombre);
            command.Parameters.AddWithValue("@Apellido", apoderado.Apellido);
            command.Parameters.AddWithValue("@Direccion", apoderado.Direccion);
            command.Parameters.AddWithValue("@Telefono", (object?)apoderado.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)apoderado.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaRegistro", apoderado.FechaRegistro);
            command.Parameters.AddWithValue("@IsActive", apoderado.IsActive);

            var id = (int)await command.ExecuteScalarAsync();
            return id;
        }

        public async Task ActualizarApoderadoAsync(int id, LegalGuardian apoderado)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE LegalGuardians 
                         SET DNI = @DNI, Nombre = @Nombre, Apellido = @Apellido, Direccion = @Direccion,
                             Telefono = @Telefono, Email = @Email
                         WHERE LegalGuardianId = @LegalGuardianId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LegalGuardianId", id);
            command.Parameters.AddWithValue("@DNI", apoderado.DNI);
            command.Parameters.AddWithValue("@Nombre", apoderado.Nombre);
            command.Parameters.AddWithValue("@Apellido", apoderado.Apellido);
            command.Parameters.AddWithValue("@Direccion", apoderado.Direccion);
            command.Parameters.AddWithValue("@Telefono", (object?)apoderado.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)apoderado.Email ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarApoderadoAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE LegalGuardians SET IsActive = 0 WHERE LegalGuardianId = @LegalGuardianId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LegalGuardianId", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}