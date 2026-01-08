using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class PaymentTypeRepositoryADO
    {
        private readonly string _connectionString;

        public PaymentTypeRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<PaymentType>> ObtenerTiposPagoAsync()
        {
            var tipos = new List<PaymentType>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT PaymentTypeId, TypeName, IsActive FROM PaymentTypes ORDER BY IsActive DESC, TypeName";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tipos.Add(new PaymentType
                {
                    PaymentTypeId = reader.GetInt32(0),
                    TypeName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
            return tipos;
        }

        public async Task<PaymentType?> ObtenerTipoPagoPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT PaymentTypeId, TypeName, IsActive FROM PaymentTypes WHERE PaymentTypeId = @PaymentTypeId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PaymentTypeId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PaymentType
                {
                    PaymentTypeId = reader.GetInt32(0),
                    TypeName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                };
            }
            return null;
        }

        public async Task AgregarTipoPagoAsync(PaymentType tipo)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "INSERT INTO PaymentTypes (TypeName, IsActive) VALUES (@TypeName, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TypeName", tipo.TypeName ?? "");
            command.Parameters.AddWithValue("@IsActive", tipo.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarTipoPagoAsync(int id, PaymentType tipo)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE PaymentTypes SET TypeName = @TypeName WHERE PaymentTypeId = @PaymentTypeId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PaymentTypeId", id);
            command.Parameters.AddWithValue("@TypeName", tipo.TypeName ?? "");

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarTipoPagoAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE PaymentTypes SET IsActive = 0 WHERE PaymentTypeId = @PaymentTypeId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PaymentTypeId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ReactivarTipoPagoAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE PaymentTypes SET IsActive = 1 WHERE PaymentTypeId = @PaymentTypeId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PaymentTypeId", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}
