using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class QuotaRepositoryADO
    {
        private readonly string _connectionString;

        public QuotaRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<Quota>> ObtenerCuotasAsync()
        {
            var cuotas = new List<Quota>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT q.QuotaId, q.MatriculaId, q.StudentId, q.Mes, q.Anio, q.Monto, q.FechaVencimiento,
                         q.PaymentStatusId, q.FechaCreacion, q.IsActive,
                         s.Nombre, s.Apellido, s.LegalGuardianId, ps.StatusName
                         FROM Quotas q
                         INNER JOIN Students s ON q.StudentId = s.StudentId
                         INNER JOIN PaymentStatus ps ON q.PaymentStatusId = ps.StatusId
                         WHERE q.IsActive = 1
                         ORDER BY q.FechaVencimiento ASC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                cuotas.Add(new Quota
                {
                    QuotaId = reader.GetInt32(0),
                    MatriculaId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Mes = reader.GetString(3),
                    Anio = reader.GetInt32(4),
                    Monto = reader.GetDecimal(5),
                    FechaVencimiento = reader.GetDateTime(6),
                    PaymentStatusId = reader.GetInt32(7),
                    FechaCreacion = reader.GetDateTime(8),
                    IsActive = reader.GetBoolean(9),
                    Student = new Student
                    {
                        StudentId = reader.GetInt32(2),
                        Nombre = reader.GetString(10),
                        Apellido = reader.GetString(11),
                        LegalGuardianId = reader.GetInt32(12)
                    },
                    PaymentStatus = new PaymentStatus
                    {
                        StatusId = reader.GetInt32(7),
                        StatusName = reader.GetString(13)
                    }
                });
            }

            return cuotas;
        }

        public async Task<Quota?> ObtenerCuotaPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT 
                    q.QuotaId, q.MatriculaId, q.StudentId, q.Mes, q.Anio, q.Monto, 
                    q.FechaVencimiento, q.PaymentStatusId, q.FechaCreacion, q.IsActive,
                    s.Nombre, s.Apellido, s.DNI, s.LegalGuardianId,
                    ps.StatusName
                  FROM Quotas q
                  INNER JOIN Students s ON q.StudentId = s.StudentId
                  INNER JOIN PaymentStatus ps ON q.PaymentStatusId = ps.StatusId
                  WHERE q.QuotaId = @QuotaId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Quota
                {
                    QuotaId = reader.GetInt32(0),
                    MatriculaId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Mes = reader.GetString(3),
                    Anio = reader.GetInt32(4),
                    Monto = reader.GetDecimal(5),
                    FechaVencimiento = reader.GetDateTime(6),
                    PaymentStatusId = reader.GetInt32(7),
                    FechaCreacion = reader.GetDateTime(8),
                    IsActive = reader.GetBoolean(9),
                    Student = new Student
                    {
                        StudentId = reader.GetInt32(2),
                        Nombre = reader.GetString(10),
                        Apellido = reader.GetString(11),
                        DNI = reader.GetString(12),
                        LegalGuardianId = reader.GetInt32(13)
                    },
                    PaymentStatus = new PaymentStatus
                    {
                        StatusId = reader.GetInt32(7),
                        StatusName = reader.GetString(14)
                    }
                };
            }

            return null;
        }

        public async Task AgregarCuotaAsync(Quota cuota)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO Quotas (MatriculaId, StudentId, Mes, Anio, Monto, FechaVencimiento, PaymentStatusId, FechaCreacion, IsActive)
                         VALUES (@MatriculaId, @StudentId, @Mes, @Anio, @Monto, @FechaVencimiento, @PaymentStatusId, @FechaCreacion, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MatriculaId", cuota.MatriculaId);
            command.Parameters.AddWithValue("@StudentId", cuota.StudentId);
            command.Parameters.AddWithValue("@Mes", cuota.Mes);
            command.Parameters.AddWithValue("@Anio", cuota.Anio);
            command.Parameters.AddWithValue("@Monto", cuota.Monto);
            command.Parameters.AddWithValue("@FechaVencimiento", cuota.FechaVencimiento);
            command.Parameters.AddWithValue("@PaymentStatusId", cuota.PaymentStatusId);
            command.Parameters.AddWithValue("@FechaCreacion", cuota.FechaCreacion);
            command.Parameters.AddWithValue("@IsActive", cuota.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarCuotaAsync(int id, Quota cuota)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE Quotas 
                         SET Mes = @Mes, Anio = @Anio, Monto = @Monto, FechaVencimiento = @FechaVencimiento, PaymentStatusId = @PaymentStatusId
                         WHERE QuotaId = @QuotaId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaId", id);
            command.Parameters.AddWithValue("@Mes", cuota.Mes);
            command.Parameters.AddWithValue("@Anio", cuota.Anio);
            command.Parameters.AddWithValue("@Monto", cuota.Monto);
            command.Parameters.AddWithValue("@FechaVencimiento", cuota.FechaVencimiento);
            command.Parameters.AddWithValue("@PaymentStatusId", cuota.PaymentStatusId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarCuotaAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Quotas SET IsActive = 0 WHERE QuotaId = @QuotaId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Quota>> ObtenerCuotasPorEstudianteAsync(int studentId)
        {
            var cuotas = new List<Quota>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT q.QuotaId, q.StudentId, q.MatriculaId, q.Mes, q.Anio, 
                         q.Monto, q.FechaVencimiento, q.PaymentStatusId, q.FechaCreacion, q.IsActive,
                         ps.StatusName
                  FROM Quotas q
                  LEFT JOIN PaymentStatus ps ON q.PaymentStatusId = ps.StatusId
                  WHERE q.StudentId = @StudentId AND q.IsActive = 1
                  ORDER BY q.Anio DESC, q.FechaVencimiento DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cuotas.Add(new Quota
                {
                    QuotaId = reader.GetInt32(0),
                    StudentId = reader.GetInt32(1),
                    MatriculaId = reader.GetInt32(2),
                    Mes = reader.GetString(3),
                    Anio = reader.GetInt32(4),
                    Monto = reader.GetDecimal(5),
                    FechaVencimiento = reader.GetDateTime(6),
                    PaymentStatusId = reader.GetInt32(7),
                    FechaCreacion = reader.GetDateTime(8),
                    IsActive = reader.GetBoolean(9),
                    PaymentStatus = new PaymentStatus
                    {
                        StatusId = reader.GetInt32(7),
                        StatusName = reader.IsDBNull(10) ? "N/A" : reader.GetString(10)
                    }
                });
            }

            return cuotas;
        }

        public async Task ActualizarEstadoPagoAsync(int quotaId, int paymentStatusId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Quotas SET PaymentStatusId = @PaymentStatusId WHERE QuotaId = @QuotaId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaId", quotaId);
            command.Parameters.AddWithValue("@PaymentStatusId", paymentStatusId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Quota>> ObtenerCuotasPendientesPorApoderadoAsync(int legalGuardianId)
        {
            var cuotas = new List<Quota>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT q.QuotaId, q.MatriculaId, q.StudentId, q.Mes, q.Anio, q.Monto, q.FechaVencimiento,
                         q.PaymentStatusId, q.FechaCreacion, q.IsActive,
                         s.Nombre, s.Apellido, s.DNI, s.LegalGuardianId,
                         ps.StatusName
                         FROM Quotas q
                         INNER JOIN Students s ON q.StudentId = s.StudentId
                         INNER JOIN PaymentStatus ps ON q.PaymentStatusId = ps.StatusId
                         WHERE q.IsActive = 1 
                         AND s.LegalGuardianId = @LegalGuardianId
                         AND ps.StatusName != 'Pagado'
                         ORDER BY q.FechaVencimiento ASC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LegalGuardianId", legalGuardianId);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                cuotas.Add(new Quota
                {
                    QuotaId = reader.GetInt32(0),
                    MatriculaId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Mes = reader.GetString(3),
                    Anio = reader.GetInt32(4),
                    Monto = reader.GetDecimal(5),
                    FechaVencimiento = reader.GetDateTime(6),
                    PaymentStatusId = reader.GetInt32(7),
                    FechaCreacion = reader.GetDateTime(8),
                    IsActive = reader.GetBoolean(9),
                    Student = new Student
                    {
                        StudentId = reader.GetInt32(2),
                        Nombre = reader.GetString(10),
                        Apellido = reader.GetString(11),
                        DNI = reader.GetString(12),
                        LegalGuardianId = reader.GetInt32(13)
                    },
                    PaymentStatus = new PaymentStatus
                    {
                        StatusId = reader.GetInt32(7),
                        StatusName = reader.GetString(14)
                    }
                });
            }

            return cuotas;
        }

        public async Task<int> GenerarCuotasAutomaticasAsync(int matriculaId, int studentId, int anioEscolar, DateTime fechaMatricula, decimal montoPension)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int mesInicio = fechaMatricula.Month;
            int mesFin = 12;
            int cuotasGeneradas = 0;

            int paymentStatusPendiente = 1;

            string[] nombresMeses = { "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                                     "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

            for (int mes = mesInicio; mes <= mesFin; mes++)
            {
                DateTime fechaVencimiento = new DateTime(anioEscolar, mes, 10);

                var query = @"INSERT INTO Quotas (MatriculaId, StudentId, Mes, Anio, Monto, FechaVencimiento, PaymentStatusId, FechaCreacion, IsActive)
                             VALUES (@MatriculaId, @StudentId, @Mes, @Anio, @Monto, @FechaVencimiento, @PaymentStatusId, @FechaCreacion, @IsActive)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@MatriculaId", matriculaId);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@Mes", nombresMeses[mes]);
                command.Parameters.AddWithValue("@Anio", anioEscolar);
                command.Parameters.AddWithValue("@Monto", montoPension);
                command.Parameters.AddWithValue("@FechaVencimiento", fechaVencimiento);
                command.Parameters.AddWithValue("@PaymentStatusId", paymentStatusPendiente);
                command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                command.Parameters.AddWithValue("@IsActive", true);

                await command.ExecuteNonQueryAsync();
                cuotasGeneradas++;
            }

            return cuotasGeneradas;
        }
        public async Task EliminarCuotasPorMatriculaAsync(int matriculaId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "DELETE FROM Quotas WHERE MatriculaId = @MatriculaId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MatriculaId", matriculaId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistenCuotasParaMatriculaAsync(int matriculaId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Quotas WHERE MatriculaId = @MatriculaId AND IsActive = 1";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MatriculaId", matriculaId);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task ReactivarCuotaAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Quotas SET IsActive = 1 WHERE QuotaId = @QuotaId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaId", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}