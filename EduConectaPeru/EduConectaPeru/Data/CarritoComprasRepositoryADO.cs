using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class CarritoComprasRepositoryADO
    {
        private readonly string _connectionString;

        public CarritoComprasRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<CarritoCompras?> ObtenerCarritoActivoAsync(int legalGuardianId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT CarritoId, LegalGuardianId, FechaCreacion, MontoTotal, Estado, IsActive 
                         FROM CarritoCompras 
                         WHERE LegalGuardianId = @LegalGuardianId AND Estado = 'Activo' AND IsActive = 1";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LegalGuardianId", legalGuardianId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CarritoCompras
                {
                    CarritoId = reader.GetInt32(0),
                    LegalGuardianId = reader.GetInt32(1),
                    FechaCreacion = reader.GetDateTime(2),
                    MontoTotal = reader.GetDecimal(3),
                    Estado = reader.GetString(4),
                    IsActive = reader.GetBoolean(5)
                };
            }

            return null;
        }

        public async Task<CarritoCompras?> ObtenerCarritoPorIdAsync(int carritoId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT CarritoId, LegalGuardianId, FechaCreacion, MontoTotal, Estado, IsActive 
                         FROM CarritoCompras 
                         WHERE CarritoId = @CarritoId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", carritoId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CarritoCompras
                {
                    CarritoId = reader.GetInt32(0),
                    LegalGuardianId = reader.GetInt32(1),
                    FechaCreacion = reader.GetDateTime(2),
                    MontoTotal = reader.GetDecimal(3),
                    Estado = reader.GetString(4),
                    IsActive = reader.GetBoolean(5)
                };
            }

            return null;
        }

        public async Task<int> CrearCarritoAsync(CarritoCompras carrito)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO CarritoCompras (LegalGuardianId, FechaCreacion, MontoTotal, Estado, IsActive)
                         OUTPUT INSERTED.CarritoId
                         VALUES (@LegalGuardianId, @FechaCreacion, @MontoTotal, @Estado, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LegalGuardianId", carrito.LegalGuardianId);
            command.Parameters.AddWithValue("@FechaCreacion", carrito.FechaCreacion);
            command.Parameters.AddWithValue("@MontoTotal", carrito.MontoTotal);
            command.Parameters.AddWithValue("@Estado", carrito.Estado);
            command.Parameters.AddWithValue("@IsActive", carrito.IsActive);

            var carritoId = (int)await command.ExecuteScalarAsync();
            return carritoId;
        }

        public async Task<List<DetalleCarrito>> ObtenerDetallesCarritoAsync(int carritoId)
        {
            var detalles = new List<DetalleCarrito>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT DetalleId, CarritoId, QuotaId, QuotaVacacionalId , Concepto, Monto 
                         FROM DetallesCarrito 
                         WHERE CarritoId = @CarritoId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", carritoId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                detalles.Add(new DetalleCarrito
                {
                    DetalleId = reader.GetInt32(0),
                    CarritoId = reader.GetInt32(1),
                    QuotaId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    QuotaVacacionalId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Concepto = reader.GetString(4),
                    Monto = reader.GetDecimal(5)
                });
            }

            return detalles;
        }

        public async Task AgregarDetalleAsync(DetalleCarrito detalle)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO DetallesCarrito (CarritoId, QuotaId, QuotaVacacionalId , Concepto, Monto)
                         VALUES (@CarritoId, @QuotaId, @QuotaCursoVacacionalId, @Concepto, @Monto)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", detalle.CarritoId);
            command.Parameters.AddWithValue("@QuotaId", (object?)detalle.QuotaId ?? DBNull.Value);
            command.Parameters.AddWithValue("@QuotaCursoVacacionalId", (object?)detalle.QuotaVacacionalId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Concepto", detalle.Concepto);
            command.Parameters.AddWithValue("@Monto", detalle.Monto);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarDetalleAsync(int detalleId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM DetallesCarrito WHERE DetalleId = @DetalleId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DetalleId", detalleId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task VaciarCarritoAsync(int carritoId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM DetallesCarrito WHERE CarritoId = @CarritoId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", carritoId);

            await command.ExecuteNonQueryAsync();

            await ActualizarMontoTotalAsync(carritoId);
        }

        public async Task ActualizarMontoTotalAsync(int carritoId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE CarritoCompras 
                         SET MontoTotal = (SELECT ISNULL(SUM(Monto), 0) FROM DetallesCarrito WHERE CarritoId = @CarritoId)
                         WHERE CarritoId = @CarritoId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", carritoId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarEstadoCarritoAsync(int carritoId, string estado)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE CarritoCompras SET Estado = @Estado WHERE CarritoId = @CarritoId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CarritoId", carritoId);
            command.Parameters.AddWithValue("@Estado", estado);

            await command.ExecuteNonQueryAsync();
        }
    }
}