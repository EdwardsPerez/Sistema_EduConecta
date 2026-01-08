using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class InscripcionCursoVacacionalRepositoryADO
    {
        private readonly string _connectionString;

        public InscripcionCursoVacacionalRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        public async Task<List<InscripcionCursoVacacional>> ObtenerInscripcionesAsync()
        {
            var inscripciones = new List<InscripcionCursoVacacional>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT i.InscripcionId, i.CursoVacacionalId, i.StudentId, i.LegalGuardianId,
                         i.FechaInscripcion, i.Monto, i.Estado, i.IsActive,
                         cv.NombreCurso, s.Nombre, s.Apellido, lg.Nombre, lg.Apellido
                         FROM InscripcionesCursosVacacionales i
                         INNER JOIN CursosVacacionales cv ON i.CursoVacacionalId = cv.CursoVacacionalId
                         INNER JOIN Students s ON i.StudentId = s.StudentId
                         INNER JOIN LegalGuardians lg ON i.LegalGuardianId = lg.LegalGuardianId
                         WHERE i.IsActive = 1";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                inscripciones.Add(new InscripcionCursoVacacional
                {
                    InscripcionId = reader.GetInt32(0),
                    CursoVacacionalId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    LegalGuardianId = reader.GetInt32(3),
                    FechaInscripcion = reader.GetDateTime(4),
                    Monto = reader.GetDecimal(5),
                    Estado = reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    CursoVacacional = new CursoVacacional
                    {
                        CursoVacacionalId = reader.GetInt32(1),
                        NombreCurso = reader.GetString(8)
                    },
                    Student = new Student
                    {
                        StudentId = reader.GetInt32(2),
                        Nombre = reader.GetString(9),
                        Apellido = reader.GetString(10)
                    },
                    LegalGuardian = new LegalGuardian
                    {
                        LegalGuardianId = reader.GetInt32(3),
                        Nombre = reader.GetString(11),
                        Apellido = reader.GetString(12)
                    }
                });
            }

            return inscripciones;
        }

        public async Task<InscripcionCursoVacacional?> ObtenerInscripcionPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT i.InscripcionId, i.CursoVacacionalId, i.StudentId, i.LegalGuardianId,
                         i.FechaInscripcion, i.Monto, i.Estado, i.IsActive,
                         c.NombreCurso, c.Costo, c.FechaInicio, c.FechaFin,
                         s.Nombre + ' ' + s.Apellido AS NombreEstudiante, s.DNI,
                         l.Nombre + ' ' + l.Apellido AS NombreApoderado, l.DNI AS DNIApoderado
                  FROM InscripcionesCursosVacacionales i
                  INNER JOIN CursosVacacionales c ON i.CursoVacacionalId = c.CursoVacacionalId
                  INNER JOIN Students s ON i.StudentId = s.StudentId
                  INNER JOIN LegalGuardians l ON i.LegalGuardianId = l.LegalGuardianId
                  WHERE i.InscripcionId = @InscripcionId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@InscripcionId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string nombreCompletoEstudiante = reader.GetString(12);
                string nombreCompletoApoderado = reader.GetString(14);

                var partesEstudiante = nombreCompletoEstudiante.Split(' ', 2);
                var partesApoderado = nombreCompletoApoderado.Split(' ', 2);

                return new InscripcionCursoVacacional
                {
                    InscripcionId = reader.GetInt32(0),
                    CursoVacacionalId = reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    LegalGuardianId = reader.GetInt32(3),
                    FechaInscripcion = reader.GetDateTime(4),
                    Monto = reader.GetDecimal(5),
                    Estado = reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    CursoVacacional = new CursoVacacional
                    {
                        NombreCurso = reader.GetString(8),
                        Costo = reader.GetDecimal(9),
                        FechaInicio = reader.GetDateTime(10),
                        FechaFin = reader.GetDateTime(11)
                    },
                    Student = new Student
                    {
                        Nombre = partesEstudiante.Length > 0 ? partesEstudiante[0] : "",
                        Apellido = partesEstudiante.Length > 1 ? partesEstudiante[1] : "",
                        DNI = reader.GetString(13)
                    },
                    LegalGuardian = new LegalGuardian
                    {
                        Nombre = partesApoderado.Length > 0 ? partesApoderado[0] : "",
                        Apellido = partesApoderado.Length > 1 ? partesApoderado[1] : "",
                        DNI = reader.GetString(15)
                    }
                };
            }

            return null;
        }

        public async Task<int> AgregarInscripcionAsync(InscripcionCursoVacacional inscripcion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            Console.WriteLine($"Conectado a BD: {connection.Database}");
            Console.WriteLine($"Insertando: Curso={inscripcion.CursoVacacionalId}, Estudiante={inscripcion.StudentId}, Monto={inscripcion.Monto}");

            var query = @"INSERT INTO InscripcionesCursosVacacionales 
                 (CursoVacacionalId, StudentId, LegalGuardianId, FechaInscripcion, Monto, Estado, IsActive)
                 OUTPUT INSERTED.InscripcionId
                 VALUES (@CursoVacacionalId, @StudentId, @LegalGuardianId, @FechaInscripcion, @Monto, @Estado, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CursoVacacionalId", inscripcion.CursoVacacionalId);
            command.Parameters.AddWithValue("@StudentId", inscripcion.StudentId);
            command.Parameters.AddWithValue("@LegalGuardianId", inscripcion.LegalGuardianId);
            command.Parameters.AddWithValue("@FechaInscripcion", inscripcion.FechaInscripcion);
            command.Parameters.AddWithValue("@Monto", inscripcion.Monto);
            command.Parameters.AddWithValue("@Estado", inscripcion.Estado ?? "Activa");
            command.Parameters.AddWithValue("@IsActive", inscripcion.IsActive);

            try
            {
                // Mostrar la consulta SQL para debugging
                Console.WriteLine($"SQL: {command.CommandText}");

                var inscripcionId = (int)await command.ExecuteScalarAsync();
                Console.WriteLine($"Inscripción insertada con ID: {inscripcionId}");
                return inscripcionId;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error SQL al insertar: {ex.Message}");
                Console.WriteLine($"Error Number: {ex.Number}");
                Console.WriteLine($"Procedure: {ex.Procedure}");
                Console.WriteLine($"Line Number: {ex.LineNumber}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general al insertar: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ExisteInscripcionAsync(int cursoVacacionalId, int studentId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT COUNT(*) FROM InscripcionesCursosVacacionales 
                  WHERE CursoVacacionalId = @CursoVacacionalId 
                  AND StudentId = @StudentId 
                  AND IsActive = 1";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CursoVacacionalId", cursoVacacionalId);
            command.Parameters.AddWithValue("@StudentId", studentId);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task ReactivarInscripcionAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE InscripcionesCursosVacacionales SET IsActive = 1, Estado = 'Activa' WHERE InscripcionId = @InscripcionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@InscripcionId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<InscripcionCursoVacacional>> ObtenerTodasInscripcionesAsync()
        {
            var inscripciones = new List<InscripcionCursoVacacional>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            Console.WriteLine($"Conectado a BD para obtener inscripciones: {connection.Database}");

            var query = @"SELECT i.InscripcionId, i.CursoVacacionalId, i.StudentId, i.LegalGuardianId,
                         i.FechaInscripcion, i.Monto, i.Estado, i.IsActive,
                         c.NombreCurso,
                         s.Nombre + ' ' + s.Apellido AS NombreEstudiante,
                         l.Nombre + ' ' + l.Apellido AS NombreApoderado
                  FROM InscripcionesCursosVacacionales i
                  LEFT JOIN CursosVacacionales c ON i.CursoVacacionalId = c.CursoVacacionalId
                  LEFT JOIN Students s ON i.StudentId = s.StudentId
                  LEFT JOIN LegalGuardians l ON i.LegalGuardianId = l.LegalGuardianId
                  ORDER BY i.IsActive DESC, i.FechaInscripcion DESC";

            using var command = new SqlCommand(query, connection);
            Console.WriteLine($"Ejecutando query para obtener inscripciones...");

            try
            {
                using var reader = await command.ExecuteReaderAsync();

                int count = 0;
                while (await reader.ReadAsync())
                {
                    count++;
                    string nombreCompletoEstudiante = reader.IsDBNull(9) ? "" : reader.GetString(9);
                    string nombreCompletoApoderado = reader.IsDBNull(10) ? "" : reader.GetString(10);

                    var partesEstudiante = nombreCompletoEstudiante.Split(' ', 2);
                    var partesApoderado = nombreCompletoApoderado.Split(' ', 2);

                    var inscripcion = new InscripcionCursoVacacional
                    {
                        InscripcionId = reader.GetInt32(0),
                        CursoVacacionalId = reader.GetInt32(1),
                        StudentId = reader.GetInt32(2),
                        LegalGuardianId = reader.GetInt32(3),
                        FechaInscripcion = reader.GetDateTime(4),
                        Monto = reader.GetDecimal(5),
                        Estado = reader.GetString(6),
                        IsActive = reader.GetBoolean(7),
                        CursoVacacional = new CursoVacacional
                        {
                            NombreCurso = reader.IsDBNull(8) ? "N/A" : reader.GetString(8)
                        },
                        Student = new Student
                        {
                            Nombre = partesEstudiante.Length > 0 ? partesEstudiante[0] : "",
                            Apellido = partesEstudiante.Length > 1 ? partesEstudiante[1] : ""
                        },
                        LegalGuardian = new LegalGuardian
                        {
                            Nombre = partesApoderado.Length > 0 ? partesApoderado[0] : "",
                            Apellido = partesApoderado.Length > 1 ? partesApoderado[1] : ""
                        }
                    };

                    Console.WriteLine($"Encontrada inscripción ID: {inscripcion.InscripcionId}, Activa: {inscripcion.IsActive}");
                    inscripciones.Add(inscripcion);
                }

                Console.WriteLine($"Total inscripciones encontradas: {count}");
                return inscripciones;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener inscripciones: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task ActualizarInscripcionAsync(int id, InscripcionCursoVacacional inscripcion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE InscripcionesCursosVacacionales 
                 SET CursoVacacionalId = @CursoVacacionalId,
                     StudentId = @StudentId,
                     LegalGuardianId = @LegalGuardianId,
                     Monto = @Monto,
                     Estado = @Estado
                 WHERE InscripcionId = @InscripcionId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@InscripcionId", id);
            command.Parameters.AddWithValue("@CursoVacacionalId", inscripcion.CursoVacacionalId);
            command.Parameters.AddWithValue("@StudentId", inscripcion.StudentId);
            command.Parameters.AddWithValue("@LegalGuardianId", inscripcion.LegalGuardianId);
            command.Parameters.AddWithValue("@Monto", inscripcion.Monto);
            command.Parameters.AddWithValue("@Estado", inscripcion.Estado);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarInscripcionAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE InscripcionesCursosVacacionales SET IsActive = 0, Estado = 'Cancelada' WHERE InscripcionId = @InscripcionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@InscripcionId", id);

            await command.ExecuteNonQueryAsync();
        }

        // Método adicional para debugging
        public async Task<int> ContarInscripcionesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM InscripcionesCursosVacacionales";
            using var command = new SqlCommand(query, connection);

            return (int)await command.ExecuteScalarAsync();
        }
    }
}