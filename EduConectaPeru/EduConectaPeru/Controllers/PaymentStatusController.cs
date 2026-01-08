using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class PaymentStatusController : Controller
    {
        private readonly PaymentStatusRepositoryADO _paymentStatusRepo;

        public PaymentStatusController(PaymentStatusRepositoryADO paymentStatusRepo)
        {
            _paymentStatusRepo = paymentStatusRepo;
        }

        // GET: PaymentStatus
        public async Task<IActionResult> Index()
        {
            var estados = await _paymentStatusRepo.ObtenerEstadosPagoAsync();
            return View(estados);
        }

        // GET: PaymentStatus/Details/
        public async Task<IActionResult> Details(int id)
        {
            var estado = await _paymentStatusRepo.ObtenerEstadoPagoPorIdAsync(id);
            if (estado == null)
            {
                return NotFound();
            }

            return View(estado);
        }

        // GET: PaymentStatus/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PaymentStatus/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentStatus estado)
        {
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }
                return View(estado);
            }

            try
            {
                var estadosExistentes = await _paymentStatusRepo.ObtenerEstadosPagoAsync();
                if (estadosExistentes.Any(e => e.StatusName.Equals(estado.StatusName, StringComparison.OrdinalIgnoreCase)))
                {
                    if (isAjaxRequest)
                    {
                        return Json(new { success = false, message = $"El estado '{estado.StatusName}' ya existe" });
                    }
                    ModelState.AddModelError("StatusName", $"El estado '{estado.StatusName}' ya existe");
                    return View(estado);
                }

                await _paymentStatusRepo.AgregarEstadoPagoAsync(estado);
                TempData["SuccessMessage"] = "Estado de pago agregado exitosamente";

                if (isAjaxRequest)
                {
                    return Json(new { success = true, message = "Estado de pago agregado exitosamente" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "Error al guardar: " + ex.Message });
                }
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(estado);
            }
        }

        // GET: PaymentStatus/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var estado = await _paymentStatusRepo.ObtenerEstadoPagoPorIdAsync(id);
            if (estado == null)
            {
                return NotFound();
            }

            return View(estado);
        }

        // POST: PaymentStatus/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentStatus estado)
        {
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (id != estado.StatusId)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "ID no coincide" });
                }
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }
                return View(estado);
            }

            try
            {
                var estadosExistentes = await _paymentStatusRepo.ObtenerEstadosPagoAsync();
                if (estadosExistentes.Any(e => e.StatusName.Equals(estado.StatusName, StringComparison.OrdinalIgnoreCase) && e.StatusId != id))
                {
                    if (isAjaxRequest)
                    {
                        return Json(new { success = false, message = $"El estado '{estado.StatusName}' ya existe" });
                    }
                    ModelState.AddModelError("StatusName", $"El estado '{estado.StatusName}' ya existe");
                    return View(estado);
                }

                await _paymentStatusRepo.ActualizarEstadoPagoAsync(id, estado);
                TempData["SuccessMessage"] = "Estado de pago actualizado exitosamente";

                if (isAjaxRequest)
                {
                    return Json(new { success = true, message = "Estado de pago actualizado exitosamente" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "Error al actualizar: " + ex.Message });
                }
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(estado);
            }
        }

        // GET: PaymentStatus/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var estado = await _paymentStatusRepo.ObtenerEstadoPagoPorIdAsync(id);
            if (estado == null)
            {
                return NotFound();
            }

            return View(estado);
        }

        // POST: PaymentStatus/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            try
            {
                await _paymentStatusRepo.EliminarEstadoPagoAsync(id);
                TempData["SuccessMessage"] = "Estado de pago eliminado exitosamente";

                if (isAjaxRequest)
                {
                    return Json(new { success = true, message = "Estado de pago eliminado exitosamente" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
                }
                TempData["ErrorMessage"] = "Error al eliminar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var estado = await _paymentStatusRepo.ObtenerEstadoPagoPorIdAsync(id);
                if (estado == null)
                {
                    TempData["ErrorMessage"] = "Estado de pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = $"Estado '{estado.StatusName}' reactivado exitosamente";
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