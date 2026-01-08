using EduConectaPeru.Models;
using Microsoft.Data.SqlClient;

namespace EduConectaPeru.Data
{
    public class StudentRepositoryADO
    {
        private readonly string _connectionString;

        public StudentRepositoryADO(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<Student>> ObtenerEstudiantesAsync()
        {
            var estudiantes = new List<Student>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT s.StudentId, s.DNI, s.Nombre, s.Apellido, s.FechaNacimiento, 
                  s.Direccion, s.Telefono, s.Email, s.LegalGuardianId, s.FechaRegistro, s.IsActive,
                  l.Nombre + ' ' + l.Apellido AS NombreApoderado
                  FROM Students s
                  LEFT JOIN LegalGuardians l ON s.LegalGuardianId = l.LegalGuardianId
                  ORDER BY s.IsActive DESC, s.FechaRegistro DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                estudiantes.Add(new Student
                {
                    StudentId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    FechaNacimiento = reader.GetDateTime(4),
                    Direccion = reader.GetString(5),
                    Telefono = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Email = reader.IsDBNull(7) ? null : reader.GetString(7),
                    LegalGuardianId = reader.GetInt32(8),
                    FechaRegistro = reader.GetDateTime(9),
                    IsActive = reader.GetBoolean(10),
                    LegalGuardian = reader.IsDBNull(11) ? null : new LegalGuardian
                    {
                        Nombre = reader.GetString(11)
                    }
                });
            }

            return estudiantes;
        }

        public async Task<List<Student>> ObtenerEstudiantesConApoderadoAsync()
        {
            var estudiantes = new List<Student>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT s.StudentId, s.DNI, s.Nombre, s.Apellido, s.FechaNacimiento, s.Direccion, 
                         s.Telefono, s.Email, s.LegalGuardianId, s.FechaRegistro, s.IsActive,
                         lg.LegalGuardianId, lg.DNI, lg.Nombre, lg.Apellido, lg.Telefono, lg.Email
                         FROM Students s
                         INNER JOIN LegalGuardians lg ON s.LegalGuardianId = lg.LegalGuardianId
                         WHERE s.IsActive = 1";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                estudiantes.Add(new Student
                {
                    StudentId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    FechaNacimiento = reader.GetDateTime(4),
                    Direccion = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Telefono = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Email = reader.IsDBNull(7) ? null : reader.GetString(7),
                    LegalGuardianId = reader.GetInt32(8),
                    FechaRegistro = reader.GetDateTime(9),
                    IsActive = reader.GetBoolean(10),
                    LegalGuardian = new LegalGuardian
                    {
                        LegalGuardianId = reader.GetInt32(11),
                        DNI = reader.GetString(12),
                        Nombre = reader.GetString(13),
                        Apellido = reader.GetString(14),
                        Telefono = reader.IsDBNull(15) ? null : reader.GetString(15),
                        Email = reader.IsDBNull(16) ? null : reader.GetString(16)
                    }
                });
            }

            return estudiantes;
        }

        public async Task<Student?> ObtenerEstudiantePorIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT StudentId, DNI, Nombre, Apellido, FechaNacimiento, Direccion, Telefono, Email, LegalGuardianId, FechaRegistro, IsActive FROM Students WHERE StudentId = @StudentId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Student
                {
                    StudentId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    FechaNacimiento = reader.GetDateTime(4),
                    Direccion = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Telefono = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Email = reader.IsDBNull(7) ? null : reader.GetString(7),
                    LegalGuardianId = reader.GetInt32(8),
                    FechaRegistro = reader.GetDateTime(9),
                    IsActive = reader.GetBoolean(10)
                };
            }

            return null;
        }

        public async Task AgregarEstudianteAsync(Student student)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO Students (DNI, Nombre, Apellido, FechaNacimiento, Direccion, Telefono, Email, LegalGuardianId, FechaRegistro, IsActive)
                         VALUES (@DNI, @Nombre, @Apellido, @FechaNacimiento, @Direccion, @Telefono, @Email, @LegalGuardianId, @FechaRegistro, @IsActive)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DNI", student.DNI);
            command.Parameters.AddWithValue("@Nombre", student.Nombre);
            command.Parameters.AddWithValue("@Apellido", student.Apellido);
            command.Parameters.AddWithValue("@FechaNacimiento", student.FechaNacimiento);
            command.Parameters.AddWithValue("@Direccion", (object?)student.Direccion ?? DBNull.Value);
            command.Parameters.AddWithValue("@Telefono", (object?)student.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)student.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@LegalGuardianId", student.LegalGuardianId);
            command.Parameters.AddWithValue("@FechaRegistro", student.FechaRegistro);
            command.Parameters.AddWithValue("@IsActive", student.IsActive);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ActualizarEstudianteAsync(int id, Student student)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE Students 
                         SET DNI = @DNI, Nombre = @Nombre, Apellido = @Apellido, FechaNacimiento = @FechaNacimiento,
                             Direccion = @Direccion, Telefono = @Telefono, Email = @Email, LegalGuardianId = @LegalGuardianId
                         WHERE StudentId = @StudentId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", id);
            command.Parameters.AddWithValue("@DNI", student.DNI);
            command.Parameters.AddWithValue("@Nombre", student.Nombre);
            command.Parameters.AddWithValue("@Apellido", student.Apellido);
            command.Parameters.AddWithValue("@FechaNacimiento", student.FechaNacimiento);
            command.Parameters.AddWithValue("@Direccion", (object?)student.Direccion ?? DBNull.Value);
            command.Parameters.AddWithValue("@Telefono", (object?)student.Telefono ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)student.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@LegalGuardianId", student.LegalGuardianId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EliminarEstudianteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Students SET IsActive = 0 WHERE StudentId = @StudentId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Student>> ObtenerEstudiantesPorApoderadoAsync(int legalGuardianId)
        {
            var estudiantes = new List<Student>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT StudentId, DNI, Nombre, Apellido, FechaNacimiento, Direccion, Telefono, Email, LegalGuardianId, FechaRegistro, IsActive FROM Students WHERE LegalGuardianId = @LegalGuardianId AND IsActive = 1";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@LegalGuardianId", legalGuardianId);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                estudiantes.Add(new Student
                {
                    StudentId = reader.GetInt32(0),
                    DNI = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    FechaNacimiento = reader.GetDateTime(4),
                    Direccion = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Telefono = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Email = reader.IsDBNull(7) ? null : reader.GetString(7),
                    LegalGuardianId = reader.GetInt32(8),
                    FechaRegistro = reader.GetDateTime(9),
                    IsActive = reader.GetBoolean(10)
                });
            }

            return estudiantes;
        }
        public async Task ReactivarEstudianteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Students SET IsActive = 1 WHERE StudentId = @StudentId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", id);

            await command.ExecuteNonQueryAsync();
        }
        public async Task<bool> ExisteDNIAsync(string dni, int? studentIdExcluir = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string query;
            if (studentIdExcluir.HasValue)
            {
                query = "SELECT COUNT(*) FROM Students WHERE DNI = @DNI AND StudentId != @StudentId";
            }
            else
            {
                query = "SELECT COUNT(*) FROM Students WHERE DNI = @DNI";
            }

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DNI", dni);

            if (studentIdExcluir.HasValue)
            {
                command.Parameters.AddWithValue("@StudentId", studentIdExcluir.Value);
            }

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
    }

}
