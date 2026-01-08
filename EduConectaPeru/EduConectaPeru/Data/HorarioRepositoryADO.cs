using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class HorarioRepositoryADO
    {
        private readonly string _connectionString;

        public HorarioRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<Horario>> ObtenerHorariosAsync()
        {
            var horarios = new List<Horario>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT HorarioId, GradoSeccionId, DiaSemana, HoraInicio, HoraFin, Curso, IsActive 
                         FROM Horarios 
                         ORDER BY IsActive DESC, DiaSemana, HoraInicio";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                horarios.Add(new Horario
                {
                    HorarioId = reader.GetInt32(0),
                    GradoSeccionId = reader.GetInt32(1),
                    DocenteId = null,
                    DiaSemana = reader.GetString(2),
                    HoraInicio = reader.GetTimeSpan(3),
                    HoraFin = reader.GetTimeSpan(4),
                    Curso = reader.GetString(5),
                    IsActive = reader.GetBoolean(6)
                });
            }

            return horarios;
        }

        public async Task<Horario?> ObtenerHorarioPorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT HorarioId, GradoSeccionId, DiaSemana, HoraInicio, HoraFin, Curso, IsActive 
                         FROM Horarios WHERE HorarioId = @HorarioId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HorarioId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Horario
                {
                    HorarioId = reader.GetInt32(0),
                    GradoSeccionId = reader.GetInt32(1),
                    DocenteId = null,
                    DiaSemana = reader.GetString(2),
                    HoraInicio = reader.GetTimeSpan(3),
                    HoraFin = reader.GetTimeSpan(4),
                    Curso = reader.GetString(5),
                    IsActive = reader.GetBoolean(6)
                };
            }

            return null;
        }

        public async Task AgregarHorarioAsync(Horario horario)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO Horarios (GradoSeccionId, DiaSemana, HoraInicio, HoraFin, Curso, IsActive)
                         VALUES (@GradoSeccionId, @DiaSemana, @HoraInicio, @HoraFin, @Curso, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@GradoSeccionId", horario.GradoSeccionId);
            command.Parameters.AddWithValue("@DiaSemana", horario.DiaSemana);
            command.Parameters.AddWithValue("@HoraInicio", horario.HoraInicio);
            command.Parameters.AddWithValue("@HoraFin", horario.HoraFin);
            command.Parameters.AddWithValue("@Curso", horario.Curso);
            command.Parameters.AddWithValue("@IsActive", horario.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarHorarioAsync(int id, Horario horario)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE Horarios 
                         SET GradoSeccionId = @GradoSeccionId, DiaSemana = @DiaSemana, 
                             HoraInicio = @HoraInicio, HoraFin = @HoraFin, Curso = @Curso
                         WHERE HorarioId = @HorarioId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HorarioId", id);
            command.Parameters.AddWithValue("@GradoSeccionId", horario.GradoSeccionId);
            command.Parameters.AddWithValue("@DiaSemana", horario.DiaSemana);
            command.Parameters.AddWithValue("@HoraInicio", horario.HoraInicio);
            command.Parameters.AddWithValue("@HoraFin", horario.HoraFin);
            command.Parameters.AddWithValue("@Curso", horario.Curso);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarHorarioAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Horarios SET IsActive = 0 WHERE HorarioId = @HorarioId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HorarioId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ReactivarHorarioAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Horarios SET IsActive = 1 WHERE HorarioId = @HorarioId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HorarioId", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}