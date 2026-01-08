using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class QuotasController : Controller
    {
        private readonly QuotaRepositoryADO _quotaRepo;
        private readonly StudentRepositoryADO _studentRepo;
        private readonly MatriculaRepositoryADO _matriculaRepo;
        private readonly PaymentStatusRepositoryADO _paymentStatusRepo;

        public QuotasController(
            QuotaRepositoryADO quotaRepo,
            StudentRepositoryADO studentRepo,
            MatriculaRepositoryADO matriculaRepo,
            PaymentStatusRepositoryADO paymentStatusRepo)
        {
            _quotaRepo = quotaRepo;
            _studentRepo = studentRepo;
            _matriculaRepo = matriculaRepo;
            _paymentStatusRepo = paymentStatusRepo;
        }
        // GET: Quotas
        public async Task<IActionResult> Index(int? estudianteId)
        {
            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            ViewBag.Estudiantes = new SelectList(
                estudiantes.Where(e => e.IsActive),
                "StudentId",
                "NombreCompleto",
                estudianteId
            );

            IEnumerable<Quota> cuotas;

            if (estudianteId.HasValue && estudianteId.Value > 0)
            {
                cuotas = await _quotaRepo.ObtenerCuotasPorEstudianteAsync(estudianteId.Value);
                var estudiante = await _studentRepo.ObtenerEstudiantePorIdAsync(estudianteId.Value);
                ViewBag.EstudianteSeleccionado = estudiante;

                if (cuotas.Any())
                {
                    ViewBag.MensajeFiltro = $"Mostrando {cuotas.Count()} cuota(s) de {estudiante?.NombreCompleto}";
                }
                else
                {
                    ViewBag.MensajeFiltro = $"{estudiante?.NombreCompleto} no tiene cuotas registradas";
                }
            }
            else
            {
                cuotas = await _quotaRepo.ObtenerCuotasAsync();
                ViewBag.MensajeFiltro = $"Mostrando todas las cuotas ({cuotas.Count()} total)";
            }

            var meses = new[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                       "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

            cuotas = cuotas.OrderBy(c => c.Anio)
                          .ThenBy(c => Array.IndexOf(meses, c.Mes))
                          .ToList();

            return View(cuotas);
        }


        // GET: Quotas/Details/
        public async Task<IActionResult> Details(int id)
        {
            var cuota = await _quotaRepo.ObtenerCuotaPorIdAsync(id);
            if (cuota == null)
            {
                return NotFound();
            }

            cuota.Student = await _studentRepo.ObtenerEstudiantePorIdAsync(cuota.StudentId);
            cuota.Matricula = await _matriculaRepo.ObtenerMatriculaPorIdAsync(cuota.MatriculaId);
            cuota.PaymentStatus = await _paymentStatusRepo.ObtenerEstadoPagoPorIdAsync(cuota.PaymentStatusId);

            return View(cuota);
        }

        // GET: Quotas/Create
        public async Task<IActionResult> Create()
        {
            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var matriculas = await _matriculaRepo.ObtenerMatriculasAsync();
            var estadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

            ViewBag.Estudiantes = new SelectList(
                estudiantes.Where(e => e.IsActive),
                "StudentId",
                "NombreCompleto"
            );

            ViewBag.Matriculas = new SelectList(
                matriculas.Where(m => m.IsActive),
                "MatriculaId",
                "MatriculaId"
            );

            ViewBag.EstadosPago = new SelectList(
                estadosPago,
                "StatusId",
                "StatusName"
            );

            return View();
        }

        // POST: Quotas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quota cuota)
        {
            if (!ModelState.IsValid)
            {
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var matriculas = await _matriculaRepo.ObtenerMatriculasAsync();
                var estadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto"
                );

                ViewBag.Matriculas = new SelectList(
                    matriculas.Where(m => m.IsActive),
                    "MatriculaId",
                    "MatriculaId"
                );

                ViewBag.EstadosPago = new SelectList(
                    estadosPago,
                    "StatusId",
                    "StatusName"
                );

                return View(cuota);
            }

            try
            {
                cuota.IsActive = true;
                await _quotaRepo.AgregarCuotaAsync(cuota);
                TempData["SuccessMessage"] = "Cuota creada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var matriculas = await _matriculaRepo.ObtenerMatriculasAsync();
                var estadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto"
                );

                ViewBag.Matriculas = new SelectList(
                    matriculas.Where(m => m.IsActive),
                    "MatriculaId",
                    "MatriculaId"
                );

                ViewBag.EstadosPago = new SelectList(
                    estadosPago,
                    "StatusId",
                    "StatusName"
                );

                return View(cuota);
            }
        }

        // GET: Quotas/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var cuota = await _quotaRepo.ObtenerCuotaPorIdAsync(id);
            if (cuota == null)
            {
                return NotFound();
            }

            if (!cuota.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar una cuota desactivada. Debe reactivarla primero.";
                return RedirectToAction(nameof(Index));
            }

            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var estadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

            ViewBag.Estudiantes = new SelectList(
                estudiantes.Where(e => e.IsActive),
                "StudentId",
                "NombreCompleto",
                cuota.StudentId
            );

            ViewBag.EstadosPago = new SelectList(
                estadosPago,
                "StatusId",
                "StatusName",
                cuota.PaymentStatusId
            );

            return View(cuota);
        }

        // POST: Quotas/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Quota cuota)
        {
            if (id != cuota.QuotaId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var estadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto",
                    cuota.StudentId
                );

                ViewBag.EstadosPago = new SelectList(
                    estadosPago,
                    "StatusId",
                    "StatusName",
                    cuota.PaymentStatusId
                );

                return View(cuota);
            }

            try
            {
                await _quotaRepo.ActualizarCuotaAsync(id, cuota);
                TempData["SuccessMessage"] = "Cuota actualizada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var estadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive),
                    "StudentId",
                    "NombreCompleto",
                    cuota.StudentId
                );

                ViewBag.EstadosPago = new SelectList(
                    estadosPago,
                    "StatusId",
                    "StatusName",
                    cuota.PaymentStatusId
                );

                return View(cuota);
            }
        }

        // GET: Quotas/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var cuota = await _quotaRepo.ObtenerCuotaPorIdAsync(id);
            if (cuota == null)
            {
                return NotFound();
            }

            cuota.Student = await _studentRepo.ObtenerEstudiantePorIdAsync(cuota.StudentId);
            cuota.PaymentStatus = await _paymentStatusRepo.ObtenerEstadoPagoPorIdAsync(cuota.PaymentStatusId);

            return View(cuota);
        }

        // POST: Quotas/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _quotaRepo.EliminarCuotaAsync(id);
                TempData["SuccessMessage"] = "Cuota desactivada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: Quotas/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var cuota = await _quotaRepo.ObtenerCuotaPorIdAsync(id);
                if (cuota == null)
                {
                    TempData["ErrorMessage"] = "Cuota no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (cuota.IsActive)
                {
                    TempData["ErrorMessage"] = "La cuota ya está activa";
                    return RedirectToAction(nameof(Index));
                }

                await _quotaRepo.ReactivarCuotaAsync(id);
                TempData["SuccessMessage"] = "Cuota reactivada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al reactivar: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}