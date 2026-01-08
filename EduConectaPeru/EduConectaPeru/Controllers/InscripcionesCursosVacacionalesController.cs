using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class InscripcionesCursosVacacionalesController : Controller
    {
        private readonly InscripcionCursoVacacionalRepositoryADO _inscripcionRepo;
        private readonly CursoVacacionalRepositoryADO _cursoRepo;
        private readonly StudentRepositoryADO _studentRepo;
        private readonly LegalGuardianRepositoryADO _guardianRepo;
        private readonly QuotaCursoVacacionalRepositoryADO _quotaVacacionalRepo;

        public InscripcionesCursosVacacionalesController(
            InscripcionCursoVacacionalRepositoryADO inscripcionRepo,
            CursoVacacionalRepositoryADO cursoRepo,
            StudentRepositoryADO studentRepo,
            LegalGuardianRepositoryADO guardianRepo,
            QuotaCursoVacacionalRepositoryADO quotaVacacionalRepo)
        {
            _inscripcionRepo = inscripcionRepo;
            _cursoRepo = cursoRepo;
            _studentRepo = studentRepo;
            _guardianRepo = guardianRepo;
            _quotaVacacionalRepo = quotaVacacionalRepo;
        }

        // GET: InscripcionesCursosVacacionales
        public async Task<IActionResult> Index()
        {
            var inscripciones = await _inscripcionRepo.ObtenerTodasInscripcionesAsync();

            Console.WriteLine($"Total inscripciones obtenidas en Index: {inscripciones?.Count ?? 0}");

            return View(inscripciones);
        }

        // GET: InscripcionesCursosVacacionales/Details/
        public async Task<IActionResult> Details(int id)
        {
            var inscripcion = await _inscripcionRepo.ObtenerInscripcionPorIdAsync(id);
            if (inscripcion == null)
            {
                return NotFound();
            }

            return View(inscripcion);
        }

        // GET: InscripcionesCursosVacacionales/Create
        public async Task<IActionResult> Create()
        {
            var cursos = await _cursoRepo.ObtenerCursosVacacionalesAsync();
            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var apoderados = await _guardianRepo.ObtenerApoderadosAsync();

            ViewBag.Cursos = new SelectList(
                cursos.Where(c => c.IsActive && c.CuposDisponibles > 0),
                "CursoVacacionalId",
                "NombreCurso"
            );

            ViewBag.Estudiantes = new SelectList(
                estudiantes.Where(e => e.IsActive),
                "StudentId",
                "NombreCompleto"
            );

            ViewBag.Apoderados = new SelectList(
                apoderados.Where(a => a.IsActive),
                "LegalGuardianId",
                "NombreCompleto"
            );

            return View();
        }

        // POST: InscripcionesCursosVacacionales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InscripcionCursoVacacional inscripcion)
        {
            Console.WriteLine($"Creando inscripción - CursoId: {inscripcion.CursoVacacionalId}, StudentId: {inscripcion.StudentId}");

            var curso = await _cursoRepo.ObtenerCursoPorIdAsync(inscripcion.CursoVacacionalId);
            if (curso == null)
            {
                ModelState.AddModelError("CursoVacacionalId", "El curso seleccionado no existe");
            }
            else if (curso.CuposDisponibles <= 0)
            {
                ModelState.AddModelError("CursoVacacionalId", "El curso no tiene cupos disponibles");
            }

            var estudiante = await _studentRepo.ObtenerEstudiantePorIdAsync(inscripcion.StudentId);
            if (estudiante == null || !estudiante.IsActive)
            {
                ModelState.AddModelError("StudentId", "El estudiante seleccionado no está activo");
            }

            if (curso != null && inscripcion.Monto != curso.Costo)
            {
                ModelState.AddModelError("Monto", $"El monto debe ser S/ {curso.Costo:N2} (costo del curso)");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState no válido:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }

                var cursos = await _cursoRepo.ObtenerCursosVacacionalesAsync();
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var apoderados = await _guardianRepo.ObtenerApoderadosAsync();

                ViewBag.Cursos = new SelectList(
                    cursos.Where(c => c.IsActive && c.CuposDisponibles > 0),
                    "CursoVacacionalId",
                    "NombreCurso"
                );

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto"
                );

                ViewBag.Apoderados = new SelectList(
                    apoderados.Where(a => a.IsActive),
                    "LegalGuardianId",
                    "NombreCompleto"
                );

                return View(inscripcion);
            }

            try
            {
                inscripcion.FechaInscripcion = DateTime.Now;
                inscripcion.Estado = "Activa";
                inscripcion.IsActive = true;

                Console.WriteLine($"Insertando inscripción en BD...");
                int inscripcionId = await _inscripcionRepo.AgregarInscripcionAsync(inscripcion);
                Console.WriteLine($"Inscripción creada con ID: {inscripcionId}");

                var inscripcionVerificada = await _inscripcionRepo.ObtenerInscripcionPorIdAsync(inscripcionId);
                if (inscripcionVerificada == null)
                {
                    throw new Exception("La inscripción no se creó en la base de datos");
                }

                await _cursoRepo.ActualizarCuposDisponiblesAsync(inscripcion.CursoVacacionalId, -1);
                Console.WriteLine($"Cupos actualizados para curso {inscripcion.CursoVacacionalId}");

                int numeroCuotas = 3;
                int cuotasGeneradas = await _quotaVacacionalRepo.GenerarCuotasAutomaticasAsync(
                    inscripcionId,
                    inscripcion.StudentId,
                    inscripcion.Monto,
                    numeroCuotas);

                Console.WriteLine($"Se generaron {cuotasGeneradas} cuotas");

                TempData["SuccessMessage"] = $"Inscripción registrada exitosamente. " +
                    $"Se generaron {cuotasGeneradas} cuotas de S/ {(inscripcion.Monto / numeroCuotas):N2} cada una.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear inscripción: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", "Error al guardar: " + ex.Message);

                var cursos = await _cursoRepo.ObtenerCursosVacacionalesAsync();
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var apoderados = await _guardianRepo.ObtenerApoderadosAsync();

                ViewBag.Cursos = new SelectList(
                    cursos.Where(c => c.IsActive && c.CuposDisponibles > 0),
                    "CursoVacacionalId",
                    "NombreCurso"
                );

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto"
                );

                ViewBag.Apoderados = new SelectList(
                    apoderados.Where(a => a.IsActive),
                    "LegalGuardianId",
                    "NombreCompleto"
                );

                return View(inscripcion);
            }
        }

        // GET: InscripcionesCursosVacacionales/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var inscripcion = await _inscripcionRepo.ObtenerInscripcionPorIdAsync(id);
            if (inscripcion == null)
            {
                return NotFound();
            }

            if (!inscripcion.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar una inscripción desactivada. Debe reactivarla primero.";
                return RedirectToAction(nameof(Index));
            }

            var cursos = await _cursoRepo.ObtenerCursosVacacionalesAsync();
            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var apoderados = await _guardianRepo.ObtenerApoderadosAsync();

            ViewBag.Cursos = new SelectList(
                cursos.Where(c => c.IsActive),
                "CursoVacacionalId",
                "NombreCurso",
                inscripcion.CursoVacacionalId
            );

            ViewBag.Estudiantes = new SelectList(
                estudiantes.Where(e => e.IsActive),
                "StudentId",
                "NombreCompleto",
                inscripcion.StudentId
            );

            ViewBag.Apoderados = new SelectList(
                apoderados.Where(a => a.IsActive),
                "LegalGuardianId",
                "NombreCompleto",
                inscripcion.LegalGuardianId
            );

            return View(inscripcion);
        }

        // POST: InscripcionesCursosVacacionales/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InscripcionCursoVacacional inscripcion)
        {
            if (id != inscripcion.InscripcionId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var cursos = await _cursoRepo.ObtenerCursosVacacionalesAsync();
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var apoderados = await _guardianRepo.ObtenerApoderadosAsync();

                ViewBag.Cursos = new SelectList(
                    cursos.Where(c => c.IsActive),
                    "CursoVacacionalId",
                    "NombreCurso",
                    inscripcion.CursoVacacionalId
                );

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto",
                    inscripcion.StudentId
                );

                ViewBag.Apoderados = new SelectList(
                    apoderados.Where(a => a.IsActive),
                    "LegalGuardianId",
                    "NombreCompleto",
                    inscripcion.LegalGuardianId
                );

                return View(inscripcion);
            }

            try
            {
                await _inscripcionRepo.ActualizarInscripcionAsync(id, inscripcion);
                TempData["SuccessMessage"] = "Inscripción actualizada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);

                var cursos = await _cursoRepo.ObtenerCursosVacacionalesAsync();
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var apoderados = await _guardianRepo.ObtenerApoderadosAsync();

                ViewBag.Cursos = new SelectList(
                    cursos.Where(c => c.IsActive),
                    "CursoVacacionalId",
                    "NombreCurso",
                    inscripcion.CursoVacacionalId
                );

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto",
                    inscripcion.StudentId
                );

                ViewBag.Apoderados = new SelectList(
                    apoderados.Where(a => a.IsActive),
                    "LegalGuardianId",
                    "NombreCompleto",
                    inscripcion.LegalGuardianId
                );

                return View(inscripcion);
            }
        }

        // GET: InscripcionesCursosVacacionales/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var inscripcion = await _inscripcionRepo.ObtenerInscripcionPorIdAsync(id);
            if (inscripcion == null)
            {
                return NotFound();
            }

            return View(inscripcion);
        }

        // POST: InscripcionesCursosVacacionales/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var inscripcion = await _inscripcionRepo.ObtenerInscripcionPorIdAsync(id);
                if (inscripcion != null)
                {
                    await _cursoRepo.ActualizarCuposDisponiblesAsync(inscripcion.CursoVacacionalId, 1);
                }

                await _inscripcionRepo.EliminarInscripcionAsync(id);
                TempData["SuccessMessage"] = "Inscripción desactivada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: InscripcionesCursosVacacionales/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var inscripcion = await _inscripcionRepo.ObtenerInscripcionPorIdAsync(id);
                if (inscripcion == null)
                {
                    TempData["ErrorMessage"] = "Inscripción no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (inscripcion.IsActive)
                {
                    TempData["ErrorMessage"] = "La inscripción ya está activa";
                    return RedirectToAction(nameof(Index));
                }

                await _inscripcionRepo.ReactivarInscripcionAsync(id);
                TempData["SuccessMessage"] = "Inscripción reactivada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al reactivar: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: InscripcionesCursosVacacionales/GetLegalGuardianByStudent
        [HttpGet]
        public async Task<JsonResult> GetLegalGuardianByStudent(int studentId)
        {
            try
            {
                var student = await _studentRepo.ObtenerEstudiantePorIdAsync(studentId);
                if (student != null)
                {
                    return Json(new { success = true, legalGuardianId = student.LegalGuardianId });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpGet]
        [Route("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_inscripcionRepo.GetConnectionString());
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM InscripcionesCursosVacacionales";
                using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                var count = (int)await command.ExecuteScalarAsync();

                query = "SELECT TOP 5 * FROM InscripcionesCursosVacacionales ORDER BY InscripcionId DESC";
                command.CommandText = query;

                var lastRecords = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    lastRecords.Add($"ID: {reader["InscripcionId"]}, Fecha: {reader["FechaInscripcion"]}, Activo: {reader["IsActive"]}");
                }

                return Json(new
                {
                    success = true,
                    totalCount = count,
                    lastRecords = lastRecords
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}