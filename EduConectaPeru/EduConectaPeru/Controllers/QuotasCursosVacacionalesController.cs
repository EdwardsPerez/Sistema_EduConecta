using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class QuotasCursosVacacionalesController : Controller
    {
        private readonly QuotaCursoVacacionalRepositoryADO _quotaRepo;
        private readonly InscripcionCursoVacacionalRepositoryADO _inscripcionRepo;
        private readonly StudentRepositoryADO _studentRepo;
        private readonly PaymentStatusRepositoryADO _paymentStatusRepo;

        public QuotasCursosVacacionalesController(
            QuotaCursoVacacionalRepositoryADO quotaRepo,
            InscripcionCursoVacacionalRepositoryADO inscripcionRepo,
            StudentRepositoryADO studentRepo,
            PaymentStatusRepositoryADO paymentStatusRepo)
        {
            _quotaRepo = quotaRepo;
            _inscripcionRepo = inscripcionRepo;
            _studentRepo = studentRepo;
            _paymentStatusRepo = paymentStatusRepo;
        }

        // GET: QuotasCursosVacacionales
        public async Task<IActionResult> Index()
        {
            var cuotas = await _quotaRepo.ObtenerTodasCuotasAsync();
            return View(cuotas);
        }

        // GET: QuotasCursosVacacionales/Details/
        public async Task<IActionResult> Details(int id)
        {
            var cuota = await _quotaRepo.ObtenerCuotaPorIdAsync(id);
            if (cuota == null)
            {
                return NotFound();
            }
            return View(cuota);
        }

        // GET: QuotasCursosVacacionales/Create
        public async Task<IActionResult> Create()
        {
            var inscripciones = await _inscripcionRepo.ObtenerTodasInscripcionesAsync();
            ViewBag.Inscripciones = inscripciones.Where(i => i.IsActive && i.Estado == "Activa").ToList();

            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            ViewBag.Estudiantes = estudiantes.Where(e => e.IsActive).ToList();

            ViewBag.EstadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

            return View();
        }

        // POST: QuotasCursosVacacionales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuotaCursoVacacional cuota)
        {
            if (cuota.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto debe ser mayor a 0");
            }

            if (cuota.FechaVencimiento < DateTime.Today)
            {
                ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser anterior a hoy");
            }

            var inscripcion = await _inscripcionRepo.ObtenerInscripcionPorIdAsync(cuota.InscripcionId);
            if (inscripcion == null || !inscripcion.IsActive)
            {
                ModelState.AddModelError("InscripcionId", "La inscripción seleccionada no es válida o está inactiva");
            }

            if (!ModelState.IsValid)
            {
                var inscripciones = await _inscripcionRepo.ObtenerTodasInscripcionesAsync();
                ViewBag.Inscripciones = inscripciones.Where(i => i.IsActive && i.Estado == "Activa").ToList();

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                ViewBag.Estudiantes = estudiantes.Where(e => e.IsActive).ToList();

                ViewBag.EstadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

                return View(cuota);
            }

            try
            {
                cuota.IsActive = true;

                await _quotaRepo.AgregarCuotaAsync(cuota);
                TempData["SuccessMessage"] = "Cuota vacacional creada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);

                var inscripciones = await _inscripcionRepo.ObtenerTodasInscripcionesAsync();
                ViewBag.Inscripciones = inscripciones.Where(i => i.IsActive && i.Estado == "Activa").ToList();

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                ViewBag.Estudiantes = estudiantes.Where(e => e.IsActive).ToList();

                ViewBag.EstadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

                return View(cuota);
            }
        }

        // GET: QuotasCursosVacacionales/Edit/
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

            var inscripciones = await _inscripcionRepo.ObtenerTodasInscripcionesAsync();
            ViewBag.Inscripciones = inscripciones.Where(i => i.IsActive && i.Estado == "Activa").ToList();

            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            ViewBag.Estudiantes = estudiantes.Where(e => e.IsActive).ToList();

            ViewBag.EstadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

            return View(cuota);
        }

        // POST: QuotasCursosVacacionales/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QuotaCursoVacacional cuota)
        {
            if (id != cuota.QuotaVacacionalId)
            {
                return BadRequest();
            }

            if (cuota.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto debe ser mayor a 0");
            }

            var cuotaActual = await _quotaRepo.ObtenerCuotaPorIdAsync(id);
            if (cuotaActual != null && cuotaActual.PaymentStatus?.StatusName == "Pendiente")
            {
                if (cuota.FechaVencimiento < DateTime.Today)
                {
                    ModelState.AddModelError("FechaVencimiento", "La fecha de vencimiento no puede ser anterior a hoy");
                }
            }

            if (!ModelState.IsValid)
            {
                var inscripciones = await _inscripcionRepo.ObtenerTodasInscripcionesAsync();
                ViewBag.Inscripciones = inscripciones.Where(i => i.IsActive && i.Estado == "Activa").ToList();

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                ViewBag.Estudiantes = estudiantes.Where(e => e.IsActive).ToList();

                ViewBag.EstadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

                return View(cuota);
            }

            try
            {
                await _quotaRepo.ActualizarCuotaAsync(id, cuota);
                TempData["SuccessMessage"] = "Cuota vacacional actualizada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);

                var inscripciones = await _inscripcionRepo.ObtenerTodasInscripcionesAsync();
                ViewBag.Inscripciones = inscripciones.Where(i => i.IsActive && i.Estado == "Activa").ToList();

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                ViewBag.Estudiantes = estudiantes.Where(e => e.IsActive).ToList();

                ViewBag.EstadosPago = await _paymentStatusRepo.ObtenerEstadosPagoAsync();

                return View(cuota);
            }
        }

        // GET: QuotasCursosVacacionales/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var cuota = await _quotaRepo.ObtenerCuotaPorIdAsync(id);
            if (cuota == null)
            {
                return NotFound();
            }
            return View(cuota);
        }

        // POST: QuotasCursosVacacionales/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _quotaRepo.EliminarCuotaAsync(id);
                TempData["SuccessMessage"] = "Cuota vacacional desactivada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: QuotasCursosVacacionales/Reactivar/
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
                TempData["SuccessMessage"] = "Cuota vacacional reactivada exitosamente";
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