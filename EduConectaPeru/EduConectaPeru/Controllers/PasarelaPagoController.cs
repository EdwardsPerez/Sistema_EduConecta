using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "ApoderadoOnly")]
    public class PasarelaPagoController : Controller
    {
        private readonly CarritoComprasRepositoryADO _carritoRepo;
        private readonly TransaccionPagoRepositoryADO _transaccionRepo;
        private readonly LegalGuardianRepositoryADO _guardianRepo;
        private readonly QuotaRepositoryADO _quotaRepo;
        private readonly PaymentRepositoryADO _paymentRepo;
        private readonly PaymentTypeRepositoryADO _paymentTypeRepo;
        private readonly BankRepositoryADO _bankRepo;
        private readonly QuotaCursoVacacionalRepositoryADO _quotaVacacionalRepo;

        public PasarelaPagoController(
            TransaccionPagoRepositoryADO transaccionRepo,
            CarritoComprasRepositoryADO carritoRepo,
            PaymentRepositoryADO paymentRepo,
            QuotaRepositoryADO quotaRepo,
            BankRepositoryADO bankRepo,
            PaymentTypeRepositoryADO paymentTypeRepo,
            LegalGuardianRepositoryADO guardianRepo,
            QuotaCursoVacacionalRepositoryADO quotaVacacionalRepo)
        {
            _transaccionRepo = transaccionRepo;
            _carritoRepo = carritoRepo;
            _paymentRepo = paymentRepo;
            _quotaRepo = quotaRepo;
            _bankRepo = bankRepo;
            _paymentTypeRepo = paymentTypeRepo;
            _guardianRepo = guardianRepo;
            _quotaVacacionalRepo = quotaVacacionalRepo;
        }

        private async Task<int?> ObtenerApoderadoIdAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return null;

            var apoderados = await _guardianRepo.ObtenerApoderadosAsync();

            var apoderado = apoderados.FirstOrDefault(a =>
                (a.Email != null && a.Email == username) ||
                username.Contains(a.DNI));

            return apoderado?.LegalGuardianId;
        }

        public async Task<IActionResult> Index(int carritoId)
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction("Login", "Account");
            }

            var carrito = await _carritoRepo.ObtenerCarritoPorIdAsync(carritoId);
            if (carrito == null)
            {
                TempData["ErrorMessage"] = "Carrito no encontrado";
                return RedirectToAction("Index", "CarritoCompras");
            }

            if (carrito.LegalGuardianId != apoderadoId)
            {
                TempData["ErrorMessage"] = "No tiene permiso para acceder a este carrito";
                return RedirectToAction("Index", "CarritoCompras");
            }

            carrito.DetallesCarrito = await _carritoRepo.ObtenerDetallesCarritoAsync(carritoId);

            if (!carrito.DetallesCarrito.Any())
            {
                TempData["ErrorMessage"] = "El carrito está vacío";
                return RedirectToAction("Index", "CarritoCompras");
            }

            ViewBag.PaymentTypes = await _paymentTypeRepo.ObtenerTiposPagoAsync();
            ViewBag.Banks = await _bankRepo.ObtenerBancosAsync();

            return View(carrito);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(int carritoId, string numeroTarjeta, string tipoTarjeta,
                                               string mesVencimiento, string anioVencimiento, string cvv,
                                               string nombreTitular, int paymentTypeId, int? bankId)
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var carrito = await _carritoRepo.ObtenerCarritoPorIdAsync(carritoId);
                if (carrito == null || carrito.LegalGuardianId != apoderadoId)
                {
                    TempData["ErrorMessage"] = "Carrito no válido";
                    return RedirectToAction("Index", "CarritoCompras");
                }

                carrito.DetallesCarrito = await _carritoRepo.ObtenerDetallesCarritoAsync(carritoId);

                if (!carrito.DetallesCarrito.Any())
                {
                    TempData["ErrorMessage"] = "El carrito está vacío";
                    return RedirectToAction("Index", "CarritoCompras");
                }

                Random random = new Random();
                bool aprobado = random.Next(1, 11) <= 9;

                string ultimos4Digitos = numeroTarjeta.Length >= 4
                    ? numeroTarjeta.Substring(numeroTarjeta.Length - 4)
                    : numeroTarjeta;

                var transaccion = new TransaccionPago
                {
                    CarritoId = carritoId,
                    MontoTotal = carrito.MontoTotal,
                    FechaTransaccion = DateTime.Now,
                    PaymentTypeId = paymentTypeId,
                    BankId = bankId,
                    NumeroTarjeta = ultimos4Digitos,
                    Estado = aprobado ? "Aprobada" : "Rechazada",
                    CodigoAutorizacion = aprobado ? "AUTH-" + random.Next(100000, 999999).ToString() : null
                };

                await _transaccionRepo.CrearTransaccionAsync(transaccion);

                var transaccionCreada = await _transaccionRepo.ObtenerUltimaTransaccionPorCarritoAsync(carritoId);

                if (aprobado)
                {
                    var paymentStatusPagadoId = 2;
                    int cuotasActualizadas = 0;
                    int cuotasVacacionalesActualizadas = 0;
                    var errores = new List<string>();

                    foreach (var detalle in carrito.DetallesCarrito)
                    {
                        try
                        {
                            if (detalle.QuotaId.HasValue)
                            {
                                await _quotaRepo.ActualizarEstadoPagoAsync(detalle.QuotaId.Value, paymentStatusPagadoId);

                                var quota = await _quotaRepo.ObtenerCuotaPorIdAsync(detalle.QuotaId.Value);

                                if (quota == null)
                                {
                                    errores.Add($"Cuota {detalle.QuotaId} no encontrada");
                                    continue;
                                }

                                var payment = new Payment
                                {
                                    QuotaId = detalle.QuotaId.Value,
                                    StudentId = quota.StudentId,
                                    Monto = detalle.Monto,
                                    FechaPago = DateTime.Now,
                                    PaymentTypeId = paymentTypeId,
                                    BankId = bankId,
                                    NumeroOperacion = transaccion.CodigoAutorizacion,
                                    Observaciones = $"Pago procesado mediante pasarela - Transacción: {transaccionCreada?.TransaccionId}",
                                    IsActive = true
                                };

                                await _paymentRepo.AgregarPagoAsync(payment);
                                cuotasActualizadas++;
                            }

                            if (detalle.QuotaVacacionalId.HasValue)
                            {
                                await _quotaVacacionalRepo.ActualizarEstadoPagoAsync(detalle.QuotaVacacionalId.Value, paymentStatusPagadoId);
                                cuotasVacacionalesActualizadas++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errores.Add($"Error en cuota {detalle.QuotaId ?? detalle.QuotaVacacionalId}: {ex.Message}");
                        }
                    }

                    await _carritoRepo.VaciarCarritoAsync(carritoId);
                    await _carritoRepo.ActualizarEstadoCarritoAsync(carritoId, "Procesado");

                    var mensajeDetallado = $"¡Pago procesado exitosamente! " +
                        $"💳 Transacción aprobada por S/ {carrito.MontoTotal:N2}. ";

                    if (cuotasActualizadas > 0)
                    {
                        mensajeDetallado += $"✅ {cuotasActualizadas} pensión(es) pagada(s). ";
                    }

                    if (cuotasVacacionalesActualizadas > 0)
                    {
                        mensajeDetallado += $"✅ {cuotasVacacionalesActualizadas} cuota(s) de curso vacacional pagada(s). ";
                    }

                    mensajeDetallado += $"📄 Código de autorización: {transaccion.CodigoAutorizacion}";

                    if (errores.Any())
                    {
                        mensajeDetallado += $" ⚠️ Algunas cuotas tuvieron problemas: {string.Join(", ", errores)}";
                    }

                    TempData["SuccessMessage"] = mensajeDetallado;
                    return RedirectToAction("Confirmacion", new { transaccionId = transaccionCreada?.TransaccionId });
                }
                else
                {
                    TempData["ErrorMessage"] = $"❌ El pago fue RECHAZADO. Por favor, verifique sus datos e intente nuevamente. " +
                        $"Si el problema persiste, contacte con su banco.";
                    return RedirectToAction("Confirmacion", new { transaccionId = transaccionCreada?.TransaccionId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al procesar el pago: " + ex.Message;
                return RedirectToAction("Index", new { carritoId });
            }
        }

        public async Task<IActionResult> Confirmacion(int transaccionId)
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction("Login", "Account");
            }

            var transaccion = await _transaccionRepo.ObtenerTransaccionPorIdAsync(transaccionId);
            if (transaccion == null)
            {
                TempData["ErrorMessage"] = "Transacción no encontrada";
                return RedirectToAction("Index", "ApoderadoDashboard");
            }

            var carrito = await _carritoRepo.ObtenerCarritoPorIdAsync(transaccion.CarritoId);
            if (carrito == null || carrito.LegalGuardianId != apoderadoId)
            {
                TempData["ErrorMessage"] = "No tiene permiso para ver esta transacción";
                return RedirectToAction("Index", "ApoderadoDashboard");
            }

            return View(transaccion);
        }

        public async Task<IActionResult> Comprobante(int transaccionId)
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction("Login", "Account");
            }

            var transaccion = await _transaccionRepo.ObtenerTransaccionPorIdAsync(transaccionId);
            if (transaccion == null)
            {
                TempData["ErrorMessage"] = "Transacción no encontrada";
                return RedirectToAction("Index", "ApoderadoDashboard");
            }

            var carrito = await _carritoRepo.ObtenerCarritoPorIdAsync(transaccion.CarritoId);
            if (carrito == null || carrito.LegalGuardianId != apoderadoId)
            {
                TempData["ErrorMessage"] = "No tiene permiso para ver este comprobante";
                return RedirectToAction("Index", "ApoderadoDashboard");
            }

            var apoderado = await _guardianRepo.ObtenerApoderadoPorIdAsync(apoderadoId.Value);
            ViewBag.Apoderado = apoderado;

            transaccion.Carrito = carrito;
            carrito.DetallesCarrito = await _carritoRepo.ObtenerDetallesCarritoAsync(carrito.CarritoId);

            return View(transaccion);
        }
    }
}