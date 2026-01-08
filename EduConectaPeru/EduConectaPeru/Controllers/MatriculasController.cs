using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class MatriculasController : Controller
    {
        private readonly MatriculaRepositoryADO _matriculaRepo;
        private readonly StudentRepositoryADO _studentRepo;
        private readonly LegalGuardianRepositoryADO _guardianRepo;
        private readonly GradoSeccionRepositoryADO _gradoSeccionRepo;
        private readonly QuotaRepositoryADO _quotaRepo;

        public MatriculasController(
            MatriculaRepositoryADO matriculaRepo,
            StudentRepositoryADO studentRepo,
            LegalGuardianRepositoryADO guardianRepo,
            GradoSeccionRepositoryADO gradoSeccionRepo,
            QuotaRepositoryADO quotaRepo)
        {
            _matriculaRepo = matriculaRepo;
            _studentRepo = studentRepo;
            _guardianRepo = guardianRepo;
            _gradoSeccionRepo = gradoSeccionRepo;
            _quotaRepo = quotaRepo;
        }

        // GET: Matriculas
        public async Task<IActionResult> Index()
        {
            var matriculas = await _matriculaRepo.ObtenerMatriculasAsync();
            return View(matriculas);
        }

        // GET: Matriculas/Details/
        public async Task<IActionResult> Details(int id)
        {
            var matricula = await _matriculaRepo.ObtenerMatriculaPorIdAsync(id);
            if (matricula == null)
            {
                return NotFound();
            }

            matricula.Student = await _studentRepo.ObtenerEstudiantePorIdAsync(matricula.StudentId);
            matricula.LegalGuardian = await _guardianRepo.ObtenerApoderadoPorIdAsync(matricula.LegalGuardianId);
            matricula.GradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(matricula.GradoSeccionId);

            return View(matricula);
        }

        // GET: Matriculas/Create
        public async Task<IActionResult> Create()
        {
            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var apoderados = await _guardianRepo.ObtenerApoderadosAsync();
            var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();

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

            ViewBag.GradoSecciones = new SelectList(
                gradoSecciones.Where(g => g.IsActive),
                "GradoSeccionId",
                "GradoSeccionNombre"
            );

            return View();
        }

        // POST: Matriculas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Matricula matricula)
        {
            var tieneMatriculaDuplicada = await _matriculaRepo.TieneMatriculaActivaAsync(
                matricula.StudentId,
                matricula.AnioEscolar,
                null);

            if (tieneMatriculaDuplicada)
            {
                ModelState.AddModelError("", "El estudiante ya tiene una matrícula activa para el año escolar " + matricula.AnioEscolar);
            }

            if (matricula.MontoMatricula <= 0)
            {
                ModelState.AddModelError("MontoMatricula", "El monto de matrícula debe ser mayor a 0");
            }

            var estudiante = await _studentRepo.ObtenerEstudiantePorIdAsync(matricula.StudentId);
            if (estudiante == null || !estudiante.IsActive)
            {
                ModelState.AddModelError("StudentId", "El estudiante seleccionado no está activo");
            }

            if (!ModelState.IsValid)
            {
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var apoderados = await _guardianRepo.ObtenerApoderadosAsync();
                var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();

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

                ViewBag.GradoSecciones = new SelectList(
                    gradoSecciones.Where(g => g.IsActive),
                    "GradoSeccionId",
                    "GradoSeccionNombre"
                );

                return View(matricula);
            }

            try
            {
                matricula.FechaMatricula = DateTime.Now;
                matricula.Estado = "Activa";
                matricula.IsActive = true;

                int matriculaId = await _matriculaRepo.AgregarMatriculaAsync(matricula);

                decimal montoPension = await _matriculaRepo.ObtenerMontoPensionMensualAsync(
                    matricula.GradoSeccionId,
                    matricula.AnioEscolar);

                int cuotasGeneradas = await _quotaRepo.GenerarCuotasAutomaticasAsync(
                    matriculaId,
                    matricula.StudentId,
                    matricula.AnioEscolar,
                    matricula.FechaMatricula,
                    montoPension);

                var mesesRestantes = 12 - matricula.FechaMatricula.Month + 1;
                TempData["SuccessMessage"] = $"✅ ¡Matrícula registrada exitosamente! " +
                    $"📋 Estudiante: {estudiante?.NombreCompleto}. " +
                    $"💰 Monto matrícula: S/ {matricula.MontoMatricula:N2}. " +
                    $"📅 Se generaron automáticamente {cuotasGeneradas} cuotas mensuales de S/ {montoPension:N2} cada una " +
                    $"(desde {matricula.FechaMatricula:MMMM yyyy} hasta Diciembre {matricula.AnioEscolar}). " +
                    $"💵 Total pensiones del año: S/ {(montoPension * cuotasGeneradas):N2}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var apoderados = await _guardianRepo.ObtenerApoderadosAsync();
                var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();

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

                ViewBag.GradoSecciones = new SelectList(
                    gradoSecciones.Where(g => g.IsActive),
                    "GradoSeccionId",
                    "GradoSeccionNombre"
                );

                return View(matricula);
            }
        }

        // GET: Matriculas/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var matricula = await _matriculaRepo.ObtenerMatriculaPorIdAsync(id);
            if (matricula == null)
            {
                return NotFound();
            }

            if (!matricula.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar una matrícula desactivada. Debe reactivarla primero.";
                return RedirectToAction(nameof(Index));
            }

            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var apoderados = await _guardianRepo.ObtenerApoderadosAsync();
            var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();

            ViewBag.Estudiantes = new SelectList(
                estudiantes.Where(e => e.IsActive),
                "StudentId",
                "NombreCompleto",
                matricula.StudentId
            );

            ViewBag.Apoderados = new SelectList(
                apoderados.Where(a => a.IsActive),
                "LegalGuardianId",
                "NombreCompleto",
                matricula.LegalGuardianId
            );

            ViewBag.GradoSecciones = new SelectList(
                gradoSecciones.Where(g => g.IsActive),
                "GradoSeccionId",
                "GradoSeccionNombre",
                matricula.GradoSeccionId
            );

            return View(matricula);
        }

        // POST: Matriculas/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Matricula matricula)
        {
            if (id != matricula.MatriculaId)
            {
                return BadRequest();
            }

            var tieneMatriculaDuplicada = await _matriculaRepo.TieneMatriculaActivaAsync(
                matricula.StudentId,
                matricula.AnioEscolar,
                matricula.MatriculaId);

            if (tieneMatriculaDuplicada)
            {
                ModelState.AddModelError("", "El estudiante ya tiene otra matrícula activa para el año escolar " + matricula.AnioEscolar);
            }

            if (matricula.MontoMatricula <= 0)
            {
                ModelState.AddModelError("MontoMatricula", "El monto de matrícula debe ser mayor a 0");
            }

            var estudiante = await _studentRepo.ObtenerEstudiantePorIdAsync(matricula.StudentId);
            if (estudiante == null || !estudiante.IsActive)
            {
                ModelState.AddModelError("StudentId", "El estudiante seleccionado no está activo");
            }

            if (!ModelState.IsValid)
            {
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var apoderados = await _guardianRepo.ObtenerApoderadosAsync();
                var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto",
                    matricula.StudentId
                );

                ViewBag.Apoderados = new SelectList(
                    apoderados.Where(a => a.IsActive),
                    "LegalGuardianId",
                    "NombreCompleto",
                    matricula.LegalGuardianId
                );

                ViewBag.GradoSecciones = new SelectList(
                    gradoSecciones.Where(g => g.IsActive),
                    "GradoSeccionId",
                    "GradoSeccionNombre",
                    matricula.GradoSeccionId
                );

                return View(matricula);
            }

            try
            {
                await _matriculaRepo.ActualizarMatriculaAsync(id, matricula);
                TempData["SuccessMessage"] = "Matrícula actualizada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var apoderados = await _guardianRepo.ObtenerApoderadosAsync();
                var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto",
                    matricula.StudentId
                );

                ViewBag.Apoderados = new SelectList(
                    apoderados.Where(a => a.IsActive),
                    "LegalGuardianId",
                    "NombreCompleto",
                    matricula.LegalGuardianId
                );

                ViewBag.GradoSecciones = new SelectList(
                    gradoSecciones.Where(g => g.IsActive),
                    "GradoSeccionId",
                    "GradoSeccionNombre",
                    matricula.GradoSeccionId
                );

                return View(matricula);
            }
        }

        // GET: Matriculas/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var matricula = await _matriculaRepo.ObtenerMatriculaPorIdAsync(id);
            if (matricula == null)
            {
                return NotFound();
            }

            matricula.Student = await _studentRepo.ObtenerEstudiantePorIdAsync(matricula.StudentId);
            matricula.LegalGuardian = await _guardianRepo.ObtenerApoderadoPorIdAsync(matricula.LegalGuardianId);
            matricula.GradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(matricula.GradoSeccionId);

            return View(matricula);
        }

        // POST: Matriculas/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _matriculaRepo.EliminarMatriculaAsync(id);
                TempData["SuccessMessage"] = "Matrícula desactivada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: Matriculas/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var matricula = await _matriculaRepo.ObtenerMatriculaPorIdAsync(id);
                if (matricula == null)
                {
                    TempData["ErrorMessage"] = "Matrícula no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (matricula.IsActive)
                {
                    TempData["ErrorMessage"] = "La matrícula ya está activa";
                    return RedirectToAction(nameof(Index));
                }

                await _matriculaRepo.ReactivarMatriculaAsync(id);
                TempData["SuccessMessage"] = "Matrícula reactivada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al reactivar: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
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
    }
}