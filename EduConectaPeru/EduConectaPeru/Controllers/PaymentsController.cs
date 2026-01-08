using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class PaymentsController : Controller
    {
        private readonly PaymentRepositoryADO _paymentRepo;
        private readonly StudentRepositoryADO _studentRepo;
        private readonly QuotaRepositoryADO _quotaRepo;
        private readonly PaymentTypeRepositoryADO _paymentTypeRepo;
        private readonly BankRepositoryADO _bankRepo;

        public PaymentsController(
            PaymentRepositoryADO paymentRepo,
            StudentRepositoryADO studentRepo,
            QuotaRepositoryADO quotaRepo,
            PaymentTypeRepositoryADO paymentTypeRepo,
            BankRepositoryADO bankRepo)
        {
            _paymentRepo = paymentRepo;
            _studentRepo = studentRepo;
            _quotaRepo = quotaRepo;
            _paymentTypeRepo = paymentTypeRepo;
            _bankRepo = bankRepo;
        }

        // GET: Payments
        public async Task<IActionResult> Index(int? estudianteId)
        {
            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var estudiantesActivos = estudiantes.Where(e => e.IsActive).ToList();

            ViewBag.Estudiantes = new SelectList(
                estudiantesActivos,
                "StudentId",
                "NombreCompleto",
                estudianteId
            );

            var pagos = await _paymentRepo.ObtenerPagosAsync();
            var pagosList = pagos.ToList();

            if (estudianteId.HasValue && estudianteId.Value > 0)
            {
                pagosList = pagosList.Where(p => p.StudentId == estudianteId.Value).ToList();
                var estudiante = await _studentRepo.ObtenerEstudiantePorIdAsync(estudianteId.Value);
                ViewBag.MensajeFiltro = $"Mostrando {pagosList.Count} pago(s) de {estudiante?.NombreCompleto}";
            }
            else
            {
                ViewBag.MensajeFiltro = $"Mostrando todos los pagos ({pagosList.Count} total)";
            }

            pagosList = pagosList.OrderByDescending(p => p.FechaPago).ToList();

            return View(pagosList);
        }

        // GET: Payments/GetPaymentsByStudent
        [HttpGet]
        public async Task<JsonResult> GetPaymentsByStudent(int studentId)
        {
            try
            {
                var pagos = await _paymentRepo.ObtenerPagosPorEstudianteAsync(studentId);
                var pagosData = pagos.Select(p => new
                {
                    paymentId = p.PaymentId,
                    quotaId = p.QuotaId,
                    mes = p.Quota?.Mes ?? "N/A",
                    anio = p.Quota?.Anio ?? 0,
                    monto = p.Monto,
                    fechaPago = p.FechaPago.ToString("dd/MM/yyyy"),
                    tipoPago = p.PaymentType?.TypeName ?? "N/A",
                    banco = p.Bank?.BankName ?? "N/A",
                    numeroOperacion = p.NumeroOperacion ?? "N/A"
                }).ToList();

                return Json(new { success = true, pagos = pagosData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Payments/Details/
        public async Task<IActionResult> Details(int id)
        {
            var pago = await _paymentRepo.ObtenerPagoPorIdAsync(id);
            if (pago == null)
            {
                return NotFound();
            }

            pago.Quota = await _quotaRepo.ObtenerCuotaPorIdAsync(pago.QuotaId);
            if (pago.Quota != null)
            {
                pago.Quota.Student = await _studentRepo.ObtenerEstudiantePorIdAsync(pago.Quota.StudentId);
            }
            pago.Student = await _studentRepo.ObtenerEstudiantePorIdAsync(pago.StudentId);
            pago.PaymentType = await _paymentTypeRepo.ObtenerTipoPagoPorIdAsync(pago.PaymentTypeId);

            if (pago.BankId.HasValue)
            {
                pago.Bank = await _bankRepo.ObtenerBancoPorIdAsync(pago.BankId.Value);
            }

            return View(pago);
        }

        // GET: Payments/Create
        public async Task<IActionResult> Create()
        {
            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var tiposPago = await _paymentTypeRepo.ObtenerTiposPagoAsync();
            var bancos = await _bankRepo.ObtenerBancosAsync();

            ViewBag.Estudiantes = new SelectList(
                estudiantes.Where(e => e.IsActive).ToList(),
                "StudentId",
                "NombreCompleto"
            );
            ViewBag.TiposPago = new SelectList(
                tiposPago.Where(t => t.IsActive).ToList(),
                "PaymentTypeId",
                "TypeName"
            );
            ViewBag.Bancos = new SelectList(
                bancos.Where(b => b.IsActive).ToList(),
                "BankId",
                "BankName"
            );

            return View();
        }

        // POST: Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment pago)
        {
            if (!ModelState.IsValid)
            {
                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var tiposPago = await _paymentTypeRepo.ObtenerTiposPagoAsync();
                var bancos = await _bankRepo.ObtenerBancosAsync();

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive).ToList(),
                    "StudentId",
                    "NombreCompleto",
                    pago.StudentId
                );
                ViewBag.TiposPago = new SelectList(
                    tiposPago.Where(t => t.IsActive).ToList(),
                    "PaymentTypeId",
                    "TypeName",
                    pago.PaymentTypeId
                );
                ViewBag.Bancos = new SelectList(
                    bancos.Where(b => b.IsActive).ToList(),
                    "BankId",
                    "BankName",
                    pago.BankId
                );

                return View(pago);
            }

            try
            {
                pago.FechaPago = DateTime.Now;
                pago.IsActive = true;
                await _paymentRepo.AgregarPagoAsync(pago);
                TempData["SuccessMessage"] = "Pago registrado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);

                var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
                var tiposPago = await _paymentTypeRepo.ObtenerTiposPagoAsync();
                var bancos = await _bankRepo.ObtenerBancosAsync();

                ViewBag.Estudiantes = new SelectList(
                    estudiantes.Where(e => e.IsActive).ToList(),
                    "StudentId",
                    "NombreCompleto",
                    pago.StudentId
                );
                ViewBag.TiposPago = new SelectList(
                    tiposPago.Where(t => t.IsActive).ToList(),
                    "PaymentTypeId",
                    "TypeName",
                    pago.PaymentTypeId
                );
                ViewBag.Bancos = new SelectList(
                    bancos.Where(b => b.IsActive).ToList(),
                    "BankId",
                    "BankName",
                    pago.BankId
                );

                return View(pago);
            }
        }

        // GET: Payments/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var pago = await _paymentRepo.ObtenerPagoPorIdAsync(id);
            if (pago == null)
            {
                return NotFound();
            }

            var cuotas = await _quotaRepo.ObtenerCuotasAsync();
            var tiposPago = await _paymentTypeRepo.ObtenerTiposPagoAsync();
            var bancos = await _bankRepo.ObtenerBancosAsync();

            ViewBag.Cuotas = new SelectList(cuotas.ToList(), "QuotaId", "QuotaId");
            ViewBag.TiposPago = new SelectList(
                tiposPago.Where(t => t.IsActive).ToList(),
                "PaymentTypeId",
                "TypeName",
                pago.PaymentTypeId
            );
            ViewBag.Bancos = new SelectList(
                bancos.Where(b => b.IsActive).ToList(),
                "BankId",
                "BankName",
                pago.BankId
            );

            return View(pago);
        }

        // POST: Payments/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment pago)
        {
            if (id != pago.PaymentId)
            {
                return BadRequest();
            }

            if (pago.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto debe ser mayor a 0");
            }

            if (!ModelState.IsValid)
            {
                var cuotas = await _quotaRepo.ObtenerCuotasAsync();
                var tiposPago = await _paymentTypeRepo.ObtenerTiposPagoAsync();
                var bancos = await _bankRepo.ObtenerBancosAsync();

                ViewBag.Cuotas = new SelectList(cuotas.ToList(), "QuotaId", "QuotaId");
                ViewBag.TiposPago = new SelectList(
                    tiposPago.Where(t => t.IsActive).ToList(),
                    "PaymentTypeId",
                    "TypeName",
                    pago.PaymentTypeId
                );
                ViewBag.Bancos = new SelectList(
                    bancos.Where(b => b.IsActive).ToList(),
                    "BankId",
                    "BankName",
                    pago.BankId
                );

                return View(pago);
            }

            try
            {
                await _paymentRepo.ActualizarPagoAsync(id, pago);
                TempData["SuccessMessage"] = "Pago actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);

                var cuotas = await _quotaRepo.ObtenerCuotasAsync();
                var tiposPago = await _paymentTypeRepo.ObtenerTiposPagoAsync();
                var bancos = await _bankRepo.ObtenerBancosAsync();

                ViewBag.Cuotas = new SelectList(cuotas.ToList(), "QuotaId", "QuotaId");
                ViewBag.TiposPago = new SelectList(
                    tiposPago.Where(t => t.IsActive).ToList(),
                    "PaymentTypeId",
                    "TypeName",
                    pago.PaymentTypeId
                );
                ViewBag.Bancos = new SelectList(
                    bancos.Where(b => b.IsActive).ToList(),
                    "BankId",
                    "BankName",
                    pago.BankId
                );

                return View(pago);
            }
        }

        // GET: Payments/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var pago = await _paymentRepo.ObtenerPagoPorIdAsync(id);
            if (pago == null)
            {
                return NotFound();
            }

            pago.Quota = await _quotaRepo.ObtenerCuotaPorIdAsync(pago.QuotaId);
            if (pago.Quota != null)
            {
                pago.Quota.Student = await _studentRepo.ObtenerEstudiantePorIdAsync(pago.Quota.StudentId);
            }
            pago.Student = await _studentRepo.ObtenerEstudiantePorIdAsync(pago.StudentId);
            pago.PaymentType = await _paymentTypeRepo.ObtenerTipoPagoPorIdAsync(pago.PaymentTypeId);

            if (pago.BankId.HasValue)
            {
                pago.Bank = await _bankRepo.ObtenerBancoPorIdAsync(pago.BankId.Value);
            }

            return View(pago);
        }

        // POST: Payments/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _paymentRepo.EliminarPagoAsync(id);
                TempData["SuccessMessage"] = "Pago eliminado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetQuotasByStudent(int studentId)
        {
            try
            {
                var cuotas = await _quotaRepo.ObtenerCuotasPorEstudianteAsync(studentId);
                var cuotasData = cuotas.Select(c => new
                {
                    quotaId = c.QuotaId,
                    displayText = $"{c.Mes} {c.Anio} - S/ {c.Monto:N2} ({c.PaymentStatus?.StatusName ?? "N/A"})",
                    monto = c.Monto
                }).ToList();

                return Json(new { success = true, cuotas = cuotasData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}