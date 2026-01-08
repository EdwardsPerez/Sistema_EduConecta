using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class ConfiguracionCostoRepositoryADO
    {
        private readonly string _connectionString;

        public ConfiguracionCostoRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<ConfiguracionCosto>> ObtenerTodasConfiguracionesAsync()
        {
            var configuraciones = new List<ConfiguracionCosto>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT ConfiguracionId, TipoCosto, Grado, Monto, AnioEscolar, FechaCreacion, IsActive
                  FROM ConfiguracionCostos
                  ORDER BY IsActive DESC, AnioEscolar DESC, TipoCosto, Grado";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                configuraciones.Add(new ConfiguracionCosto
                {
                    ConfigId = reader.GetInt32(0),
                    TipoCosto = reader.GetString(1),
                    GradoSeccionId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Monto = reader.GetDecimal(3),
                    AnioEscolar = reader.GetInt32(4),
                    FechaVigencia = reader.GetDateTime(5),
                    IsActive = reader.GetBoolean(6)
                });
            }

            return configuraciones;
        }

        public async Task<ConfiguracionCosto?> ObtenerConfiguracionPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT ConfiguracionId, TipoCosto, Grado, Monto, AnioEscolar, FechaCreacion, IsActive
                  FROM ConfiguracionCostos
                  WHERE ConfiguracionId = @ConfiguracionId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ConfiguracionId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ConfiguracionCosto
                {
                    ConfigId = reader.GetInt32(0),
                    TipoCosto = reader.GetString(1),
                    GradoSeccionId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Monto = reader.GetDecimal(3),
                    AnioEscolar = reader.GetInt32(4),
                    FechaVigencia = reader.GetDateTime(5),
                    IsActive = reader.GetBoolean(6)
                };
            }

            return null;
        }

        public async Task AgregarConfiguracionAsync(ConfiguracionCosto configuracion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO ConfiguracionCostos (TipoCosto, Grado, Monto, AnioEscolar, FechaCreacion, IsActive)
                  VALUES (@TipoCosto, @Grado, @Monto, @AnioEscolar, @FechaCreacion, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TipoCosto", configuracion.TipoCosto);
            command.Parameters.AddWithValue("@Grado", (object?)configuracion.GradoSeccionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Monto", configuracion.Monto);
            command.Parameters.AddWithValue("@AnioEscolar", configuracion.AnioEscolar);
            command.Parameters.AddWithValue("@FechaCreacion", configuracion.FechaVigencia);
            command.Parameters.AddWithValue("@IsActive", configuracion.IsActive);

            await command.ExecuteNonQueryAsync();
        }
        public async Task ActualizarConfiguracionAsync(int id, ConfiguracionCosto configuracion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE ConfiguracionCostos 
                  SET TipoCosto = @TipoCosto,
                      Grado = @Grado,
                      Monto = @Monto,
                      AnioEscolar = @AnioEscolar
                  WHERE ConfiguracionId = @ConfiguracionId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ConfiguracionId", id);
            command.Parameters.AddWithValue("@TipoCosto", configuracion.TipoCosto);
            command.Parameters.AddWithValue("@Grado", (object?)configuracion.GradoSeccionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Monto", configuracion.Monto);
            command.Parameters.AddWithValue("@AnioEscolar", configuracion.AnioEscolar);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DesactivarConfiguracionAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE ConfiguracionCostos SET IsActive = 0 WHERE ConfiguracionId = @ConfiguracionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ConfiguracionId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ReactivarConfiguracionAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE ConfiguracionCostos SET IsActive = 1 WHERE ConfiguracionId = @ConfiguracionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ConfiguracionId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExisteConfiguracionAsync(string tipoCosto, int? grado, int anioEscolar, int? configuracionIdExcluir = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT COUNT(*) FROM ConfiguracionCostos 
                  WHERE TipoCosto = @TipoCosto 
                  AND Grado = @Grado 
                  AND AnioEscolar = @AnioEscolar 
                  AND IsActive = 1";

            if (configuracionIdExcluir.HasValue)
            {
                query += " AND ConfiguracionId != @ConfiguracionIdExcluir";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TipoCosto", tipoCosto);
            command.Parameters.AddWithValue("@Grado", (object?)grado ?? DBNull.Value);
            command.Parameters.AddWithValue("@AnioEscolar", anioEscolar);

            if (configuracionIdExcluir.HasValue)
            {
                command.Parameters.AddWithValue("@ConfiguracionIdExcluir", configuracionIdExcluir.Value);
            }

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
    }
}