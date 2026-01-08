using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class BankRepositoryADO
    {
        private readonly string _connectionString;

        public BankRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<Bank>> ObtenerBancosAsync()
        {
            var bancos = new List<Bank>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT BankId, BankName, IsActive FROM Banks ORDER BY IsActive DESC, BankName";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                bancos.Add(new Bank
                {
                    BankId = reader.GetInt32(0),
                    BankName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }

            return bancos;
        }

        public async Task<Bank?> ObtenerBancoPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT BankId, BankName, IsActive FROM Banks WHERE BankId = @BankId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BankId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Bank
                {
                    BankId = reader.GetInt32(0),
                    BankName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                };
            }

            return null;
        }
        public async Task ReactivarAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Banks SET IsActive = 1 WHERE BankId = @BankId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BankId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task AgregarBancoAsync(Bank banco)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "INSERT INTO Banks (BankName, IsActive) VALUES (@BankName, @IsActive)";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BankName", banco.BankName);
            command.Parameters.AddWithValue("@IsActive", banco.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarBancoAsync(int id, Bank banco)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Banks SET BankName = @BankName WHERE BankId = @BankId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BankId", id);
            command.Parameters.AddWithValue("@BankName", banco.BankName);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarBancoAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Banks SET IsActive = 0 WHERE BankId = @BankId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BankId", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}
