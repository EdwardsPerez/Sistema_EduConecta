using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class QuotaCursoVacacionalRepositoryADO
    {
        private readonly string _connectionString;

        public QuotaCursoVacacionalRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IEnumerable<QuotaCursoVacacional>> ObtenerCuotasPorEstudianteAsync(int studentId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT qcv.QuotaVacacionalId, qcv.InscripcionId, qcv.StudentId, 
                         qcv.Mes, qcv.Anio, qcv.Monto, qcv.FechaVencimiento, 
                         qcv.PaymentStatusId, qcv.FechaCreacion, qcv.IsActive,
                         ps.StatusId, ps.StatusName,
                         i.InscripcionId, i.CursoVacacionalId, i.LegalGuardianId,
                         cv.NombreCurso
                  FROM QuotasCursosVacacionales qcv
                  INNER JOIN PaymentStatus ps ON qcv.PaymentStatusId = ps.StatusId
                  LEFT JOIN InscripcionesCursosVacacionales i ON qcv.InscripcionId = i.InscripcionId
                  LEFT JOIN CursosVacacionales cv ON i.CursoVacacionalId = cv.CursoVacacionalId
                  WHERE qcv.StudentId = @StudentId AND qcv.IsActive = 1
                  ORDER BY qcv.FechaVencimiento";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            var cuotas = new List<QuotaCursoVacacional>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var cuota = new QuotaCursoVacacional
                {
                    QuotaVacacionalId = reader.GetInt32(0),
                    InscripcionId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Mes = reader.GetString(3), 
                    Anio = reader.GetInt32(4), 
                    Monto = reader.GetDecimal(5),
                    FechaVencimiento = reader.GetDateTime(6),
                    PaymentStatusId = reader.GetInt32(7),
                    FechaCreacion = reader.GetDateTime(8),
                    IsActive = reader.GetBoolean(9),
                    PaymentStatus = new PaymentStatus
                    {
                        StatusId = reader.GetInt32(10),
                        StatusName = reader.GetString(11)
                    }
                };

                if (!reader.IsDBNull(12))
                {
                    cuota.Inscripcion = new InscripcionCursoVacacional
                    {
                        InscripcionId = reader.GetInt32(12),
                        CursoVacacionalId = reader.GetInt32(13),
                        LegalGuardianId = reader.GetInt32(14),
                        CursoVacacional = new CursoVacacional
                        {
                            NombreCurso = reader.GetString(15)
                        }
                    };
                }

                cuotas.Add(cuota);
            }

            return cuotas;
        }

        public async Task<QuotaCursoVacacional> ObtenerCuotaPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT qcv.*, ps.StatusName,
                                 i.InscripcionId, i.CursoVacacionalId, i.LegalGuardianId,
                                 cv.NombreCurso
                          FROM QuotasCursosVacacionales qcv
                          INNER JOIN PaymentStatus ps ON qcv.PaymentStatusId = ps.StatusId
                          LEFT JOIN InscripcionesCursosVacacionales i ON qcv.InscripcionId = i.InscripcionId
                          LEFT JOIN CursosVacacionales cv ON i.CursoVacacionalId = cv.CursoVacacionalId
                          WHERE qcv.QuotaCursoVacacionalId = @Id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var cuota = new QuotaCursoVacacional
                {
                    QuotaVacacionalId = reader.GetInt32(0),
                    InscripcionId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Mes = $"Cuota {reader.GetInt32(3)}",
                    Anio = DateTime.Now.Year,
                    Monto = reader.GetDecimal(4),
                    FechaVencimiento = reader.GetDateTime(5),
                    PaymentStatusId = reader.GetInt32(6),
                    FechaCreacion = reader.GetDateTime(7),
                    IsActive = reader.GetBoolean(8),
                    PaymentStatus = new PaymentStatus
                    {
                        StatusId = reader.GetInt32(6),
                        StatusName = reader.GetString(9)
                    }
                };

                if (!reader.IsDBNull(10))
                {
                    cuota.Inscripcion = new InscripcionCursoVacacional
                    {
                        InscripcionId = reader.GetInt32(10),
                        CursoVacacionalId = reader.GetInt32(11),
                        LegalGuardianId = reader.GetInt32(12),
                        CursoVacacional = new CursoVacacional
                        {
                            NombreCurso = reader.GetString(13)
                        }
                    };
                }

                return cuota;
            }

            return null;
        }

        public async Task ActualizarEstadoPagoAsync(int quotaVacacionalId, int paymentStatusId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE QuotasCursosVacacionales SET PaymentStatusId = @PaymentStatusId WHERE QuotaCursoVacacionalId = @QuotaVacacionalId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PaymentStatusId", paymentStatusId);
            command.Parameters.AddWithValue("@QuotaVacacionalId", quotaVacacionalId);

            await command.ExecuteNonQueryAsync();
        }


        public async Task<int> GenerarCuotasAutomaticasAsync(int inscripcionId, int studentId, decimal montoTotal, int numeroCuotas = 3)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            decimal montoPorCuota = montoTotal / numeroCuotas;
            DateTime fechaBase = DateTime.Now;
            int cuotasGeneradas = 0;
            int paymentStatusPendiente = 1;

            string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                     "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

            for (int i = 1; i <= numeroCuotas; i++)
            {
                DateTime fechaVencimiento = fechaBase.AddMonths(i);
                int mesIndex = fechaVencimiento.Month - 1;
                string mes = meses[mesIndex];
                int anio = fechaVencimiento.Year;

                var query = @"INSERT INTO QuotasCursosVacacionales 
                     (InscripcionId, StudentId, Mes, Anio, Monto, FechaVencimiento, PaymentStatusId, FechaCreacion, IsActive)
                     VALUES (@InscripcionId, @StudentId, @Mes, @Anio, @Monto, @FechaVencimiento, @PaymentStatusId, @FechaCreacion, @IsActive)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@InscripcionId", inscripcionId);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@Mes", mes);
                command.Parameters.AddWithValue("@Anio", anio);
                command.Parameters.AddWithValue("@Monto", montoPorCuota);
                command.Parameters.AddWithValue("@FechaVencimiento", fechaVencimiento);
                command.Parameters.AddWithValue("@PaymentStatusId", paymentStatusPendiente);
                command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                command.Parameters.AddWithValue("@IsActive", true);

                await command.ExecuteNonQueryAsync();
                cuotasGeneradas++;
            }

            return cuotasGeneradas;
        }
        public async Task<List<QuotaCursoVacacional>> ObtenerTodasCuotasAsync()
        {
            var cuotas = new List<QuotaCursoVacacional>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT qcv.QuotaVacacionalId, qcv.InscripcionId, qcv.StudentId, 
                                 qcv.Mes, qcv.Anio, qcv.Monto, qcv.FechaVencimiento, 
                                 qcv.PaymentStatusId, qcv.FechaCreacion, qcv.IsActive,
                                 ps.StatusId, ps.StatusName,
                                 i.InscripcionId, i.CursoVacacionalId, i.LegalGuardianId,
                                 cv.NombreCurso, s.Nombre + ' ' + s.Apellido AS NombreEstudiante
                          FROM QuotasCursosVacacionales qcv
                          INNER JOIN PaymentStatus ps ON qcv.PaymentStatusId = ps.StatusId
                          LEFT JOIN InscripcionesCursosVacacionales i ON qcv.InscripcionId = i.InscripcionId
                          LEFT JOIN CursosVacacionales cv ON i.CursoVacacionalId = cv.CursoVacacionalId
                          LEFT JOIN Students s ON qcv.StudentId = s.StudentId
                          WHERE qcv.IsActive = 1
                          ORDER BY qcv.FechaVencimiento";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var cuota = new QuotaCursoVacacional
                {
                    QuotaVacacionalId = reader.GetInt32(0),
                    InscripcionId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    Mes = reader.GetString(3),
                    Anio = reader.GetInt32(4),
                    Monto = reader.GetDecimal(5),
                    FechaVencimiento = reader.GetDateTime(6),
                    PaymentStatusId = reader.GetInt32(7),
                    FechaCreacion = reader.GetDateTime(8),
                    IsActive = reader.GetBoolean(9),
                    PaymentStatus = new PaymentStatus
                    {
                        StatusId = reader.GetInt32(10),
                        StatusName = reader.GetString(11)
                    }
                };

                if (!reader.IsDBNull(12))
                {
                    cuota.Inscripcion = new InscripcionCursoVacacional
                    {
                        InscripcionId = reader.GetInt32(12),
                        CursoVacacionalId = reader.GetInt32(13),
                        LegalGuardianId = reader.GetInt32(14),
                        CursoVacacional = new CursoVacacional { NombreCurso = reader.GetString(15) }
                    };
                }

                if (!reader.IsDBNull(16))
                {
                    cuota.Student = new Student
                    {
                        StudentId = reader.GetInt32(2),
                        Nombre = reader.GetString(16)
                    };
                }

                cuotas.Add(cuota);
            }

            return cuotas;
        }

        public async Task AgregarCuotaAsync(QuotaCursoVacacional cuota)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO QuotasCursosVacacionales 
                         (InscripcionId, StudentId, Mes, Anio, Monto, FechaVencimiento, PaymentStatusId, FechaCreacion, IsActive)
                         VALUES (@InscripcionId, @StudentId, @Mes, @Anio, @Monto, @FechaVencimiento, @PaymentStatusId, @FechaCreacion, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@InscripcionId", cuota.InscripcionId);
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

        public async Task ActualizarCuotaAsync(int id, QuotaCursoVacacional cuota)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE QuotasCursosVacacionales 
                         SET InscripcionId = @InscripcionId, StudentId = @StudentId, 
                             Mes = @Mes, Anio = @Anio, Monto = @Monto, 
                             FechaVencimiento = @FechaVencimiento, PaymentStatusId = @PaymentStatusId
                         WHERE QuotaVacacionalId = @QuotaVacacionalId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaVacacionalId", id);
            command.Parameters.AddWithValue("@InscripcionId", cuota.InscripcionId);
            command.Parameters.AddWithValue("@StudentId", cuota.StudentId);
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

            var query = "UPDATE QuotasCursosVacacionales SET IsActive = 0 WHERE QuotaVacacionalId = @QuotaVacacionalId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaVacacionalId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ReactivarCuotaAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE QuotasCursosVacacionales SET IsActive = 1 WHERE QuotaVacacionalId = @QuotaVacacionalId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QuotaVacacionalId", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}