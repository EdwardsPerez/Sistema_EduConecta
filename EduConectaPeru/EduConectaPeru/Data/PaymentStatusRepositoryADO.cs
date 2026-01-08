using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class PaymentStatusRepositoryADO
    {
        private readonly string _connectionString;

        public PaymentStatusRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<PaymentStatus>> ObtenerEstadosPagoAsync()
        {
            var estados = new List<PaymentStatus>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT StatusId, StatusName FROM PaymentStatus ORDER BY StatusName";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                estados.Add(new PaymentStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }

            return estados;
        }

        public async Task<PaymentStatus?> ObtenerEstadoPagoPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT StatusId, StatusName FROM PaymentStatus WHERE StatusId = @StatusId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StatusId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PaymentStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                };
            }

            return null;
        }

        public async Task AgregarEstadoPagoAsync(PaymentStatus estado)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "INSERT INTO PaymentStatus (StatusName) VALUES (@StatusName)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StatusName", estado.StatusName);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarEstadoPagoAsync(int id, PaymentStatus estado)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE PaymentStatus SET StatusName = @StatusName WHERE StatusId = @StatusId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StatusId", id);
            command.Parameters.AddWithValue("@StatusName", estado.StatusName);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarEstadoPagoAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM PaymentStatus WHERE StatusId = @StatusId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StatusId", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}