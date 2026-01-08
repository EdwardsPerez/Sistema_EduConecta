using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class GradoSeccionRepositoryADO
    {
        private readonly string _connectionString;

        public GradoSeccionRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<GradoSeccion>> ObtenerGradoSeccionesAsync()
        {
            var gradoSecciones = new List<GradoSeccion>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT GradoSeccionId, Grado, Seccion, AnioEscolar, Capacidad, IsActive FROM GradoSecciones ORDER BY IsActive DESC, AnioEscolar DESC, Grado, Seccion";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                gradoSecciones.Add(new GradoSeccion
                {
                    GradoSeccionId = reader.GetInt32(0),
                    Grado = reader.GetString(1),
                    Seccion = reader.GetString(2),
                    AnioEscolar = reader.GetInt32(3),
                    Capacidad = reader.GetInt32(4),
                    IsActive = reader.GetBoolean(5)
                });
            }

            return gradoSecciones;
        }

        public async Task<GradoSeccion?> ObtenerGradoSeccionPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT GradoSeccionId, Grado, Seccion, AnioEscolar, Capacidad, IsActive FROM GradoSecciones WHERE GradoSeccionId = @GradoSeccionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@GradoSeccionId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new GradoSeccion
                {
                    GradoSeccionId = reader.GetInt32(0),
                    Grado = reader.GetString(1),
                    Seccion = reader.GetString(2),
                    AnioEscolar = reader.GetInt32(3),
                    Capacidad = reader.GetInt32(4),
                    IsActive = reader.GetBoolean(5)
                };
            }

            return null;
        }

        public async Task AgregarGradoSeccionAsync(GradoSeccion gradoSeccion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO GradoSecciones (Grado, Seccion, AnioEscolar, Capacidad, IsActive)
                         VALUES (@Grado, @Seccion, @AnioEscolar, @Capacidad, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Grado", gradoSeccion.Grado);
            command.Parameters.AddWithValue("@Seccion", gradoSeccion.Seccion);
            command.Parameters.AddWithValue("@AnioEscolar", gradoSeccion.AnioEscolar);
            command.Parameters.AddWithValue("@Capacidad", gradoSeccion.Capacidad);
            command.Parameters.AddWithValue("@IsActive", gradoSeccion.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarGradoSeccionAsync(int id, GradoSeccion gradoSeccion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE GradoSecciones 
                         SET Grado = @Grado, Seccion = @Seccion, AnioEscolar = @AnioEscolar, Capacidad = @Capacidad
                         WHERE GradoSeccionId = @GradoSeccionId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@GradoSeccionId", id);
            command.Parameters.AddWithValue("@Grado", gradoSeccion.Grado);
            command.Parameters.AddWithValue("@Seccion", gradoSeccion.Seccion);
            command.Parameters.AddWithValue("@AnioEscolar", gradoSeccion.AnioEscolar);
            command.Parameters.AddWithValue("@Capacidad", gradoSeccion.Capacidad);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarGradoSeccionAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE GradoSecciones SET IsActive = 0 WHERE GradoSeccionId = @GradoSeccionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@GradoSeccionId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ReactivarGradoSeccionAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE GradoSecciones SET IsActive = 1 WHERE GradoSeccionId = @GradoSeccionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@GradoSeccionId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExisteCombinacionAsync(string grado, string seccion, int anioEscolar, int? gradoSeccionIdExcluir = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT COUNT(*) FROM GradoSecciones 
                  WHERE Grado = @Grado 
                  AND Seccion = @Seccion 
                  AND AnioEscolar = @AnioEscolar 
                  AND IsActive = 1";

            if (gradoSeccionIdExcluir.HasValue)
            {
                query += " AND GradoSeccionId != @GradoSeccionIdExcluir";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Grado", grado);
            command.Parameters.AddWithValue("@Seccion", seccion);
            command.Parameters.AddWithValue("@AnioEscolar", anioEscolar);

            if (gradoSeccionIdExcluir.HasValue)
            {
                command.Parameters.AddWithValue("@GradoSeccionIdExcluir", gradoSeccionIdExcluir.Value);
            }

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
    }
}