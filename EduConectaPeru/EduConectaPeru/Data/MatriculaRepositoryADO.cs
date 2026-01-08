using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class MatriculaRepositoryADO
    {
        private readonly string _connectionString;

        public MatriculaRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<Matricula>> ObtenerMatriculasAsync()
        {
            var matriculas = new List<Matricula>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT m.MatriculaId, m.StudentId, m.LegalGuardianId, m.GradoSeccionId, 
                  m.AnioEscolar, m.FechaMatricula, m.MontoMatricula, m.Estado, m.IsActive,
                  s.Nombre + ' ' + s.Apellido AS NombreEstudiante,
                  l.Nombre + ' ' + l.Apellido AS NombreApoderado,
                  g.Grado + ' ' + g.Seccion AS GradoSeccion
                  FROM Matriculas m
                  INNER JOIN Students s ON m.StudentId = s.StudentId
                  INNER JOIN LegalGuardians l ON m.LegalGuardianId = l.LegalGuardianId
                  INNER JOIN GradoSecciones g ON m.GradoSeccionId = g.GradoSeccionId
                  ORDER BY m.IsActive DESC, m.FechaMatricula DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                matriculas.Add(new Matricula
                {
                    MatriculaId = reader.GetInt32(0),
                    StudentId = reader.GetInt32(1),
                    LegalGuardianId = reader.GetInt32(2),
                    GradoSeccionId = reader.GetInt32(3),
                    AnioEscolar = reader.GetInt32(4),
                    FechaMatricula = reader.GetDateTime(5),
                    MontoMatricula = reader.GetDecimal(6),
                    Estado = reader.GetString(7),
                    IsActive = reader.GetBoolean(8),
                    Student = new Student { Nombre = reader.GetString(9) },
                    LegalGuardian = new LegalGuardian { Nombre = reader.GetString(10) },
                    GradoSeccion = new GradoSeccion { Grado = reader.GetString(11) }
                });
            }

            return matriculas;
        }

        public async Task ReactivarMatriculaAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Matriculas SET IsActive = 1 WHERE MatriculaId = @MatriculaId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MatriculaId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<Matricula?> ObtenerMatriculaPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT MatriculaId, StudentId, LegalGuardianId, GradoSeccionId, AnioEscolar, FechaMatricula, MontoMatricula, Estado, IsActive FROM Matriculas WHERE MatriculaId = @MatriculaId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MatriculaId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Matricula
                {
                    MatriculaId = reader.GetInt32(0),
                    StudentId = reader.GetInt32(1),
                    LegalGuardianId = reader.GetInt32(2),
                    GradoSeccionId = reader.GetInt32(3),
                    AnioEscolar = reader.GetInt32(4),
                    FechaMatricula = reader.GetDateTime(5),
                    MontoMatricula = reader.GetDecimal(6),
                    Estado = reader.GetString(7),
                    IsActive = reader.GetBoolean(8)
                };
            }

            return null;
        }

        public async Task<int> AgregarMatriculaAsync(Matricula matricula)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO Matriculas (StudentId, LegalGuardianId, GradoSeccionId, AnioEscolar, FechaMatricula, MontoMatricula, Estado, IsActive)
                         VALUES (@StudentId, @LegalGuardianId, @GradoSeccionId, @AnioEscolar, @FechaMatricula, @MontoMatricula, @Estado, @IsActive);
                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", matricula.StudentId);
            command.Parameters.AddWithValue("@LegalGuardianId", matricula.LegalGuardianId);
            command.Parameters.AddWithValue("@GradoSeccionId", matricula.GradoSeccionId);
            command.Parameters.AddWithValue("@AnioEscolar", matricula.AnioEscolar);
            command.Parameters.AddWithValue("@FechaMatricula", matricula.FechaMatricula);
            command.Parameters.AddWithValue("@MontoMatricula", matricula.MontoMatricula);
            command.Parameters.AddWithValue("@Estado", matricula.Estado);
            command.Parameters.AddWithValue("@IsActive", matricula.IsActive);

            var matriculaId = (int)await command.ExecuteScalarAsync();
            return matriculaId;
        }

        public async Task<decimal> ObtenerMontoPensionMensualAsync(int gradoSeccionId, int anio)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT TOP 1 MontoPension 
                         FROM ConfiguracionCosto 
                         WHERE (GradoSeccionId = @GradoSeccionId OR GradoSeccionId IS NULL) 
                         AND Anio = @Anio 
                         AND IsActive = 1
                         ORDER BY GradoSeccionId DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@GradoSeccionId", gradoSeccionId);
            command.Parameters.AddWithValue("@Anio", anio);

            var result = await command.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                return (decimal)result;
            }

            return 300.00m;
        }

        public async Task ActualizarMatriculaAsync(int id, Matricula matricula)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE Matriculas 
                 SET StudentId = @StudentId, LegalGuardianId = @LegalGuardianId, 
                     GradoSeccionId = @GradoSeccionId, AnioEscolar = @AnioEscolar,
                     MontoMatricula = @MontoMatricula, Estado = @Estado
                 WHERE MatriculaId = @MatriculaId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MatriculaId", id);
            command.Parameters.AddWithValue("@StudentId", matricula.StudentId);
            command.Parameters.AddWithValue("@LegalGuardianId", matricula.LegalGuardianId);
            command.Parameters.AddWithValue("@GradoSeccionId", matricula.GradoSeccionId);
            command.Parameters.AddWithValue("@AnioEscolar", matricula.AnioEscolar);
            command.Parameters.AddWithValue("@MontoMatricula", matricula.MontoMatricula);
            command.Parameters.AddWithValue("@Estado", matricula.Estado);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarMatriculaAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Matriculas SET IsActive = 0 WHERE MatriculaId = @MatriculaId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@MatriculaId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> TieneMatriculaActivaAsync(int studentId, int anioEscolar, int? matriculaIdExcluir)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query;
            if (matriculaIdExcluir.HasValue)
            {
                query = @"SELECT COUNT(*) FROM Matriculas 
                  WHERE StudentId = @StudentId 
                  AND AnioEscolar = @AnioEscolar 
                  AND IsActive = 1 
                  AND MatriculaId != @MatriculaId";
            }
            else
            {
                query = @"SELECT COUNT(*) FROM Matriculas 
                  WHERE StudentId = @StudentId 
                  AND AnioEscolar = @AnioEscolar 
                  AND IsActive = 1";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);
            command.Parameters.AddWithValue("@AnioEscolar", anioEscolar);

            if (matriculaIdExcluir.HasValue)
            {
                command.Parameters.AddWithValue("@MatriculaId", matriculaIdExcluir.Value);
            }

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        public async Task<Matricula?> ObtenerUltimaMatriculaPorEstudianteAsync(int studentId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT TOP 1 MatriculaId, StudentId, LegalGuardianId, GradoSeccionId, 
                 AnioEscolar, FechaMatricula, MontoMatricula, Estado, IsActive 
                 FROM Matriculas 
                 WHERE StudentId = @StudentId 
                 ORDER BY MatriculaId DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Matricula
                {
                    MatriculaId = reader.GetInt32(0),
                    StudentId = reader.GetInt32(1),
                    LegalGuardianId = reader.GetInt32(2),
                    GradoSeccionId = reader.GetInt32(3),
                    AnioEscolar = reader.GetInt32(4),
                    FechaMatricula = reader.GetDateTime(5),
                    MontoMatricula = reader.GetDecimal(6),
                    Estado = reader.GetString(7),
                    IsActive = reader.GetBoolean(8)
                };
            }

            return null;
        }
    }
}