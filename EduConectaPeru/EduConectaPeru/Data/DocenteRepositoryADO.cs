using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class DocenteRepositoryADO
    {
        private readonly string _connectionString;

        public DocenteRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<Docente>> ObtenerDocentesAsync()
        {
            var docentes = new List<Docente>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT DocenteId, DNI, Nombre, Apellido, Especialidad, Telefono, Email, FechaContratacion, IsActive FROM Docentes ORDER BY IsActive DESC, FechaContratacion DESC";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                docentes.Add(new Docente
                {
                    DocenteId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    Especialidad = reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Email = reader.IsDBNull(6) ? null : reader.GetString(6),
                    FechaContratacion = reader.GetDateTime(7),
                    IsActive = reader.GetBoolean(8)
                });
            }

            return docentes;
        }
        public async Task ReactivarDocenteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Docentes SET IsActive = 1 WHERE DocenteId = @DocenteId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DocenteId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<Docente?> ObtenerDocentePorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT DocenteId, DNI, Nombre, Apellido, Especialidad, Telefono, Email, FechaContratacion, IsActive FROM Docentes WHERE DocenteId = @DocenteId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DocenteId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Docente
                {
                    DocenteId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    Especialidad = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Email = reader.IsDBNull(6) ? null : reader.GetString(6),
                    FechaContratacion = reader.GetDateTime(7),
                    IsActive = reader.GetBoolean(8)
                };
            }

            return null;
        }

        public async Task AgregarDocenteAsync(Docente docente)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO Docentes (DNI, Nombre, Apellido, Especialidad, Telefono, Email, FechaContratacion, IsActive)
                         VALUES (@DNI, @Nombre, @Apellido, @Especialidad, @Telefono, @Email, @FechaContratacion, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DNI", docente.DNI);
            command.Parameters.AddWithValue("@Nombre", docente.Nombre);
            command.Parameters.AddWithValue("@Apellido", docente.Apellido);
            command.Parameters.AddWithValue("@Especialidad", (object?)docente.Especialidad ?? DBNull.Value);
            command.Parameters.AddWithValue("@Telefono", (object?)docente.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)docente.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaContratacion", docente.FechaContratacion);
            command.Parameters.AddWithValue("@IsActive", docente.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarDocenteAsync(int id, Docente docente)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE Docentes 
                         SET DNI = @DNI, Nombre = @Nombre, Apellido = @Apellido, Especialidad = @Especialidad,
                             Telefono = @Telefono, Email = @Email
                         WHERE DocenteId = @DocenteId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DocenteId", id);
            command.Parameters.AddWithValue("@DNI", docente.DNI);
            command.Parameters.AddWithValue("@Nombre", docente.Nombre);
            command.Parameters.AddWithValue("@Apellido", docente.Apellido);
            command.Parameters.AddWithValue("@Especialidad", (object?)docente.Especialidad ?? DBNull.Value);
            command.Parameters.AddWithValue("@Telefono", (object?)docente.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)docente.Email ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarDocenteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Docentes SET IsActive = 0 WHERE DocenteId = @DocenteId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DocenteId", id);

            await command.ExecuteNonQueryAsync();
        }
        public async Task<bool> ExisteDNIAsync(string dni, int? docenteIdExcluir = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query;
            if (docenteIdExcluir.HasValue)
            {
                query = "SELECT COUNT(*) FROM Docentes WHERE DNI = @DNI AND DocenteId != @DocenteId";
            }
            else
            {
                query = "SELECT COUNT(*) FROM Docentes WHERE DNI = @DNI";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DNI", dni);

            if (docenteIdExcluir.HasValue)
            {
                command.Parameters.AddWithValue("@DocenteId", docenteIdExcluir.Value);
            }

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
    }
}
