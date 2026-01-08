using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class PaymentTypesController : Controller
    {
        private readonly PaymentTypeRepositoryADO _paymentTypeRepo;

        public PaymentTypesController(PaymentTypeRepositoryADO paymentTypeRepo)
        {
            _paymentTypeRepo = paymentTypeRepo;
        }

        // GET: PaymentTypes
        public async Task<IActionResult> Index()
        {
            var tipos = await _paymentTypeRepo.ObtenerTiposPagoAsync();
            return View(tipos);
        }

        // GET: PaymentTypes/Details/
        public async Task<IActionResult> Details(int id)
        {
            var tipo = await _paymentTypeRepo.ObtenerTipoPagoPorIdAsync(id);
            if (tipo == null)
            {
                return NotFound();
            }

            return View(tipo);
        }

        // GET: PaymentTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PaymentTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentType tipo)
        {
            if (!ModelState.IsValid)
            {
                return View(tipo);
            }

            try
            {
                tipo.IsActive = true;
                await _paymentTypeRepo.AgregarTipoPagoAsync(tipo);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return BadRequest(ModelState);
            }
        }

        // GET: PaymentTypes/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var tipo = await _paymentTypeRepo.ObtenerTipoPagoPorIdAsync(id);
            if (tipo == null)
            {
                return NotFound();
            }

            if (!tipo.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar un tipo de pago desactivado. Debe reactivarlo primero.";
                return RedirectToAction(nameof(Index));
            }

            return View(tipo);
        }

        // POST: PaymentTypes/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentType tipo)
        {
            if (id != tipo.PaymentTypeId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(tipo);
            }

            try
            {
                await _paymentTypeRepo.ActualizarTipoPagoAsync(id, tipo);
                TempData["SuccessMessage"] = "Tipo de pago actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(tipo);
            }
        }

        // GET: PaymentTypes/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var tipo = await _paymentTypeRepo.ObtenerTipoPagoPorIdAsync(id);
            if (tipo == null)
            {
                return NotFound();
            }

            return View(tipo);
        }

        // POST: PaymentTypes/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _paymentTypeRepo.EliminarTipoPagoAsync(id);
                TempData["SuccessMessage"] = "Tipo de pago desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: PaymentTypes/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var tipo = await _paymentTypeRepo.ObtenerTipoPagoPorIdAsync(id);
                if (tipo == null)
                {
                    TempData["ErrorMessage"] = "Tipo de pago no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (tipo.IsActive)
                {
                    TempData["ErrorMessage"] = "El tipo de pago ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                await _paymentTypeRepo.ReactivarTipoPagoAsync(id);
                TempData["SuccessMessage"] = $"Tipo de pago {tipo.TypeName} reactivado exitosamente";
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