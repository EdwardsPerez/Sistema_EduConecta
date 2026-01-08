using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class CursoVacacionalRepositoryADO
    {
        private readonly string _connectionString;

        public CursoVacacionalRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<CursoVacacional>> ObtenerCursosVacacionalesAsync()
        {
            var cursos = new List<CursoVacacional>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT CursoVacacionalId, NombreCurso, Descripcion, FechaInicio, FechaFin, 
                         Costo, CapacidadMaxima, CuposDisponibles, IsActive 
                         FROM CursosVacacionales 
                         ORDER BY IsActive DESC, CursoVacacionalId DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                cursos.Add(new CursoVacacional
                {
                    CursoVacacionalId = reader.GetInt32(0),
                    NombreCurso = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                    FechaInicio = reader.GetDateTime(3),
                    FechaFin = reader.GetDateTime(4),
                    Costo = reader.GetDecimal(5),
                    CapacidadMaxima = reader.GetInt32(6),
                    CuposDisponibles = reader.GetInt32(7),
                    IsActive = reader.GetBoolean(8),
                    FechaCreacion = DateTime.Now
                });
            }

            return cursos;
        }

        public async Task<CursoVacacional?> ObtenerCursoVacacionalPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT CursoVacacionalId, NombreCurso, Descripcion, FechaInicio, FechaFin, 
                         Costo, CapacidadMaxima, CuposDisponibles, IsActive 
                         FROM CursosVacacionales 
                         WHERE CursoVacacionalId = @CursoVacacionalId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CursoVacacionalId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CursoVacacional
                {
                    CursoVacacionalId = reader.GetInt32(0),
                    NombreCurso = reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                    FechaInicio = reader.GetDateTime(3),
                    FechaFin = reader.GetDateTime(4),
                    Costo = reader.GetDecimal(5),
                    CapacidadMaxima = reader.GetInt32(6),
                    CuposDisponibles = reader.GetInt32(7),
                    IsActive = reader.GetBoolean(8),
                    FechaCreacion = DateTime.Now
                };
            }

            return null;
        }

        public async Task AgregarCursoVacacionalAsync(CursoVacacional curso)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO CursosVacacionales (NombreCurso, Descripcion, FechaInicio, FechaFin, Costo, CapacidadMaxima, CuposDisponibles, IsActive)
                         VALUES (@NombreCurso, @Descripcion, @FechaInicio, @FechaFin, @Costo, @CapacidadMaxima, @CuposDisponibles, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@NombreCurso", curso.NombreCurso);
            command.Parameters.AddWithValue("@Descripcion", (object?)curso.Descripcion ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaInicio", curso.FechaInicio);
            command.Parameters.AddWithValue("@FechaFin", curso.FechaFin);
            command.Parameters.AddWithValue("@Costo", curso.Costo);
            command.Parameters.AddWithValue("@CapacidadMaxima", curso.CapacidadMaxima);
            command.Parameters.AddWithValue("@CuposDisponibles", curso.CapacidadMaxima);
            command.Parameters.AddWithValue("@IsActive", curso.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarCursoVacacionalAsync(int id, CursoVacacional curso)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE CursosVacacionales 
                         SET NombreCurso = @NombreCurso, Descripcion = @Descripcion, FechaInicio = @FechaInicio,
                             FechaFin = @FechaFin, Costo = @Costo, CapacidadMaxima = @CapacidadMaxima,
                             CuposDisponibles = @CuposDisponibles
                         WHERE CursoVacacionalId = @CursoVacacionalId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CursoVacacionalId", id);
            command.Parameters.AddWithValue("@NombreCurso", curso.NombreCurso);
            command.Parameters.AddWithValue("@Descripcion", (object?)curso.Descripcion ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaInicio", curso.FechaInicio);
            command.Parameters.AddWithValue("@FechaFin", curso.FechaFin);
            command.Parameters.AddWithValue("@Costo", curso.Costo);
            command.Parameters.AddWithValue("@CapacidadMaxima", curso.CapacidadMaxima);
            command.Parameters.AddWithValue("@CuposDisponibles", curso.CuposDisponibles);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarCursoVacacionalAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE CursosVacacionales SET IsActive = 0 WHERE CursoVacacionalId = @CursoVacacionalId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CursoVacacionalId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExisteNombreAsync(string nombreCurso, int? cursoIdExcluir = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query;
            if (cursoIdExcluir.HasValue)
            {
                query = @"SELECT COUNT(*) FROM CursosVacacionales 
                  WHERE NombreCurso = @NombreCurso AND CursoVacacionalId != @CursoId";
            }
            else
            {
                query = @"SELECT COUNT(*) FROM CursosVacacionales 
                  WHERE NombreCurso = @NombreCurso";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@NombreCurso", nombreCurso);

            if (cursoIdExcluir.HasValue)
            {
                command.Parameters.AddWithValue("@CursoId", cursoIdExcluir.Value);
            }

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task ReactivarCursoVacacionalAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE CursosVacacionales SET IsActive = 1 WHERE CursoVacacionalId = @CursoVacacionalId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CursoVacacionalId", id);

            await command.ExecuteNonQueryAsync();
        }
        public async Task<CursoVacacional?> ObtenerCursoPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT CursoVacacionalId, NombreCurso, Descripcion, Costo, 
                 FechaInicio, FechaFin, CuposDisponibles, IsActive
                 FROM CursosVacacionales
                 WHERE CursoVacacionalId = @CursoVacacionalId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CursoVacacionalId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CursoVacacional
                {
                    CursoVacacionalId = reader.GetInt32(0),
                    NombreCurso = reader.GetString(1),
                    Descripcion = reader.GetString(2),
                    Costo = reader.GetDecimal(3),
                    FechaInicio = reader.GetDateTime(4),
                    FechaFin = reader.GetDateTime(5),
                    CuposDisponibles = reader.GetInt32(6),
                    IsActive = reader.GetBoolean(7)
                };
            }

            return null;
        }

        public async Task ActualizarCuposDisponiblesAsync(int cursoId, int cambio)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE CursosVacacionales 
                 SET CuposDisponibles = CuposDisponibles + @Cambio
                 WHERE CursoVacacionalId = @CursoVacacionalId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@CursoVacacionalId", cursoId);
            command.Parameters.AddWithValue("@Cambio", cambio);

            await command.ExecuteNonQueryAsync();
        }
        public async Task<int> AgregarInscripcionAsync(InscripcionCursoVacacional inscripcion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

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
            command.Parameters.AddWithValue("@Estado", inscripcion.Estado);
            command.Parameters.AddWithValue("@IsActive", inscripcion.IsActive);

            var inscripcionId = (int)await command.ExecuteScalarAsync();
            return inscripcionId;
        }


    }
}