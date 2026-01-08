using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class PaymentRepositoryADO
    {
        private readonly string _connectionString;

        public PaymentRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<Payment>> ObtenerPagosAsync()
        {
            var pagos = new List<Payment>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT p.PaymentId, p.QuotaId, p.StudentId, p.Monto, p.FechaPago, p.PaymentTypeId,
                 p.BankId, p.NumeroOperacion, p.Observaciones, p.IsActive,
                 s.Nombre, s.Apellido, s.LegalGuardianId, pt.TypeName
                 FROM Payments p
                 INNER JOIN Students s ON p.StudentId = s.StudentId
                 INNER JOIN PaymentTypes pt ON p.PaymentTypeId = pt.PaymentTypeId
                 WHERE p.IsActive = 1
                 ORDER BY p.FechaPago DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                pagos.Add(new Payment
                {
                    PaymentId = reader.GetInt32(0),
                    QuotaId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Monto = reader.GetDecimal(3),
                    FechaPago = reader.GetDateTime(4),
                    PaymentTypeId = reader.GetInt32(5),
                    BankId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    NumeroOperacion = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Observaciones = reader.IsDBNull(8) ? null : reader.GetString(8),
                    IsActive = reader.GetBoolean(9),
                    Student = new Student
                    {
                        StudentId = reader.GetInt32(2),
                        Nombre = reader.GetString(10),
                        Apellido = reader.GetString(11),
                        LegalGuardianId = reader.GetInt32(12)
                    },
                    PaymentType = new PaymentType
                    {
                        PaymentTypeId = reader.GetInt32(5),
                        TypeName = reader.GetString(13)
                    }
                });
            }

            return pagos;
        }

        public async Task<List<Payment>> ObtenerPagosPorEstudianteAsync(int studentId)
        {
            var pagos = new List<Payment>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT p.PaymentId, p.QuotaId, p.StudentId, p.Monto, p.FechaPago, p.PaymentTypeId,
                         p.BankId, p.NumeroOperacion, p.Observaciones, p.IsActive,
                         q.Month, q.Year,
                         pt.TypeName,
                         b.BankName
                         FROM Payments p
                         INNER JOIN Quotas q ON p.QuotaId = q.QuotaId
                         INNER JOIN PaymentTypes pt ON p.PaymentTypeId = pt.PaymentTypeId
                         LEFT JOIN Banks b ON p.BankId = b.BankId
                         WHERE p.StudentId = @StudentId AND p.IsActive = 1
                         ORDER BY p.FechaPago DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                pagos.Add(new Payment
                {
                    PaymentId = reader.GetInt32(0),
                    QuotaId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Monto = reader.GetDecimal(3),
                    FechaPago = reader.GetDateTime(4),
                    PaymentTypeId = reader.GetInt32(5),
                    BankId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    NumeroOperacion = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Observaciones = reader.IsDBNull(8) ? null : reader.GetString(8),
                    IsActive = reader.GetBoolean(9),
                    Quota = new Quota
                    {
                        QuotaId = reader.GetInt32(1),
                        Mes = reader.GetString(10),
                        Anio = reader.GetInt32(11)
                    },
                    PaymentType = new PaymentType
                    {
                        PaymentTypeId = reader.GetInt32(5),
                        TypeName = reader.GetString(12)
                    },
                    Bank = reader.IsDBNull(13) ? null : new Bank
                    {
                        BankId = reader.GetInt32(6),
                        BankName = reader.GetString(13)
                    }
                });
            }

            return pagos;
        }

        public async Task<Payment?> ObtenerPagoPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT PaymentId, QuotaId, StudentId, Monto, FechaPago, PaymentTypeId, BankId, NumeroOperacion, Observaciones, IsActive FROM Payments WHERE PaymentId = @PaymentId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PaymentId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Payment
                {
                    PaymentId = reader.GetInt32(0),
                    QuotaId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Monto = reader.GetDecimal(3),
                    FechaPago = reader.GetDateTime(4),
                    PaymentTypeId = reader.GetInt32(5),
                    BankId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    NumeroOperacion = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Observaciones = reader.IsDBNull(8) ? null : reader.GetString(8),
                    IsActive = reader.GetBoolean(9)
                };
            }

            return null;
        }

        public async Task AgregarPagoAsync(Payment pago)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO Payments (QuotaId, StudentId, Monto, FechaPago, PaymentTypeId, BankId, NumeroOperacion, Observaciones, IsActive)
                         VALUES (@QuotaId, @StudentId, @Monto, @FechaPago, @PaymentTypeId, @BankId, @NumeroOperacion, @Observaciones, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaId", pago.QuotaId);
            command.Parameters.AddWithValue("@StudentId", pago.StudentId);
            command.Parameters.AddWithValue("@Monto", pago.Monto);
            command.Parameters.AddWithValue("@FechaPago", pago.FechaPago);
            command.Parameters.AddWithValue("@PaymentTypeId", pago.PaymentTypeId);
            command.Parameters.AddWithValue("@BankId", (object?)pago.BankId ?? DBNull.Value);
            command.Parameters.AddWithValue("@NumeroOperacion", (object?)pago.NumeroOperacion ?? DBNull.Value);
            command.Parameters.AddWithValue("@Observaciones", (object?)pago.Observaciones ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", pago.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarPagoAsync(int id, Payment pago)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE Payments 
                         SET Monto = @Monto, FechaPago = @FechaPago, PaymentTypeId = @PaymentTypeId,
                             BankId = @BankId, NumeroOperacion = @NumeroOperacion, Observaciones = @Observaciones
                         WHERE PaymentId = @PaymentId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PaymentId", id);
            command.Parameters.AddWithValue("@Monto", pago.Monto);
            command.Parameters.AddWithValue("@FechaPago", pago.FechaPago);
            command.Parameters.AddWithValue("@PaymentTypeId", pago.PaymentTypeId);
            command.Parameters.AddWithValue("@BankId", (object?)pago.BankId ?? DBNull.Value);
            command.Parameters.AddWithValue("@NumeroOperacion", (object?)pago.NumeroOperacion ?? DBNull.Value);
            command.Parameters.AddWithValue("@Observaciones", (object?)pago.Observaciones ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarPagoAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Payments SET IsActive = 0 WHERE PaymentId = @PaymentId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PaymentId", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}