using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class TransaccionPagoRepositoryADO
    {
        private readonly string _connectionString;

        public TransaccionPagoRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task AgregarTransaccionAsync(TransaccionPago transaccion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO TransaccionesPago (CarritoId, NumeroTarjeta, TipoTarjeta, MontoTotal, FechaTransaccion, CodigoAutorizacion, Estado, MensajeRespuesta, IsActive)
                         VALUES (@CarritoId, @NumeroTarjeta, @TipoTarjeta, @MontoTotal, @FechaTransaccion, @CodigoAutorizacion, @Estado, @MensajeRespuesta, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", transaccion.CarritoId);
            command.Parameters.AddWithValue("@NumeroTarjeta", transaccion.NumeroTarjeta);
            command.Parameters.AddWithValue("@TipoTarjeta", (object?)transaccion.TipoTarjeta ?? DBNull.Value);
            command.Parameters.AddWithValue("@MontoTotal", transaccion.MontoTotal);
            command.Parameters.AddWithValue("@FechaTransaccion", transaccion.FechaTransaccion);
            command.Parameters.AddWithValue("@CodigoAutorizacion", (object?)transaccion.CodigoAutorizacion ?? DBNull.Value);
            command.Parameters.AddWithValue("@Estado", transaccion.Estado);
            command.Parameters.AddWithValue("@MensajeRespuesta", (object?)transaccion.MensajeRespuesta ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", transaccion.IsActive);

            await command.ExecuteNonQueryAsync();
        }
        public async Task<int> CrearTransaccionAsync(TransaccionPago transaccion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO TransaccionesPago 
                 (CarritoId, MontoTotal, FechaTransaccion, PaymentTypeId, BankId, NumeroTarjeta, CodigoAutorizacion, Estado)
                 OUTPUT INSERTED.TransaccionId
                 VALUES (@CarritoId, @MontoTotal, @FechaTransaccion, @PaymentTypeId, @BankId, @NumeroTarjeta, @CodigoAutorizacion, @Estado)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", transaccion.CarritoId);
            command.Parameters.AddWithValue("@MontoTotal", transaccion.MontoTotal);
            command.Parameters.AddWithValue("@FechaTransaccion", transaccion.FechaTransaccion);
            command.Parameters.AddWithValue("@PaymentTypeId", transaccion.PaymentTypeId);
            command.Parameters.AddWithValue("@BankId", (object?)transaccion.BankId ?? DBNull.Value);
            command.Parameters.AddWithValue("@NumeroTarjeta", (object?)transaccion.NumeroTarjeta ?? DBNull.Value);
            command.Parameters.AddWithValue("@CodigoAutorizacion", (object?)transaccion.CodigoAutorizacion ?? DBNull.Value);
            command.Parameters.AddWithValue("@Estado", transaccion.Estado);

            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<TransaccionPago?> ObtenerUltimaTransaccionPorCarritoAsync(int carritoId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT TOP 1 TransaccionId, CarritoId, MontoTotal, FechaTransaccion, PaymentTypeId, 
                         BankId, NumeroTarjeta, CodigoAutorizacion, Estado
                  FROM TransaccionesPago 
                  WHERE CarritoId = @CarritoId 
                  ORDER BY TransaccionId DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", carritoId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TransaccionPago
                {
                    TransaccionId = reader.GetInt32(0),
                    CarritoId = reader.GetInt32(1),
                    MontoTotal = reader.GetDecimal(2),
                    FechaTransaccion = reader.GetDateTime(3),
                    PaymentTypeId = reader.GetInt32(4),
                    BankId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    NumeroTarjeta = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CodigoAutorizacion = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Estado = reader.GetString(8)
                };
            }

            return null;
        }

        public async Task<TransaccionPago?> ObtenerTransaccionPorIdAsync(int transaccionId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT TransaccionId, CarritoId, MontoTotal, FechaTransaccion, PaymentTypeId, 
                         BankId, NumeroTarjeta, CodigoAutorizacion, Estado
                  FROM TransaccionesPago 
                  WHERE TransaccionId = @TransaccionId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TransaccionId", transaccionId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TransaccionPago
                {
                    TransaccionId = reader.GetInt32(0),
                    CarritoId = reader.GetInt32(1),
                    MontoTotal = reader.GetDecimal(2),
                    FechaTransaccion = reader.GetDateTime(3),
                    PaymentTypeId = reader.GetInt32(4),
                    BankId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    NumeroTarjeta = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CodigoAutorizacion = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Estado = reader.GetString(8)
                };
            }

            return null;
        }
    }
}
