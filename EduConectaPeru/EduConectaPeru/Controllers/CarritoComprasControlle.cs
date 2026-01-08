using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "ApoderadoOnly")]
    public class CarritoComprasController : Controller
    {
        private readonly CarritoComprasRepositoryADO _carritoRepo;
        private readonly QuotaRepositoryADO _quotaRepo;
        private readonly StudentRepositoryADO _studentRepo;
        private readonly LegalGuardianRepositoryADO _guardianRepo;
        private readonly QuotaCursoVacacionalRepositoryADO _quotaVacacionalRepo; 

        public CarritoComprasController(
            CarritoComprasRepositoryADO carritoRepo,
            QuotaRepositoryADO quotaRepo,
            StudentRepositoryADO studentRepo,
            LegalGuardianRepositoryADO guardianRepo,
            QuotaCursoVacacionalRepositoryADO quotaVacacionalRepo) 
        {
            _carritoRepo = carritoRepo;
            _quotaRepo = quotaRepo;
            _studentRepo = studentRepo;
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

        // GET: CarritoCompras
        public async Task<IActionResult> Index()
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta de apoderado";
                return RedirectToAction("Login", "Account");
            }

            var carrito = await _carritoRepo.ObtenerCarritoActivoAsync(apoderadoId.Value);

            if (carrito == null)
            {
                return View(new CarritoCompras
                {
                    LegalGuardianId = apoderadoId.Value,
                    DetallesCarrito = new List<DetalleCarrito>()
                });
            }

            carrito.DetallesCarrito = await _carritoRepo.ObtenerDetallesCarritoAsync(carrito.CarritoId);

            foreach (var detalle in carrito.DetallesCarrito)
            {
                if (detalle.QuotaId.HasValue)
                {
                    var quota = await _quotaRepo.ObtenerCuotaPorIdAsync(detalle.QuotaId.Value);
                    if (quota?.Student != null)
                    {
                        detalle.Quota = quota;
                    }
                }
            }

            return View(carrito);
        }

        // POST: CarritoCompras/AgregarCuota
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarCuota(int quotaId)
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction("Index", "ApoderadoDashboard");
            }

            try
            {
                var quota = await _quotaRepo.ObtenerCuotaPorIdAsync(quotaId);
                if (quota == null)
                {
                    TempData["ErrorMessage"] = "La cuota no existe";
                    return RedirectToAction("CuotasPendientes", "ApoderadoDashboard");
                }

                if (quota.Student?.LegalGuardianId != apoderadoId)
                {
                    TempData["ErrorMessage"] = "No tiene permiso para agregar esta cuota";
                    return RedirectToAction("CuotasPendientes", "ApoderadoDashboard");
                }

                if (quota.PaymentStatus?.StatusName == "Pagado")
                {
                    TempData["ErrorMessage"] = "Esta cuota ya está pagada";
                    return RedirectToAction("CuotasPendientes", "ApoderadoDashboard");
                }

                var carrito = await _carritoRepo.ObtenerCarritoActivoAsync(apoderadoId.Value);
                if (carrito == null)
                {
                    carrito = new CarritoCompras
                    {
                        LegalGuardianId = apoderadoId.Value,
                        FechaCreacion = DateTime.Now,
                        MontoTotal = 0,
                        Estado = "Activo"
                    };
                    await _carritoRepo.CrearCarritoAsync(carrito);
                    carrito = await _carritoRepo.ObtenerCarritoActivoAsync(apoderadoId.Value);
                }

                var detalles = await _carritoRepo.ObtenerDetallesCarritoAsync(carrito.CarritoId);
                if (detalles.Any(d => d.QuotaId == quotaId))
                {
                    TempData["ErrorMessage"] = "Esta cuota ya está en el carrito";
                    return RedirectToAction(nameof(Index));
                }

                var detalle = new DetalleCarrito
                {
                    CarritoId = carrito.CarritoId,
                    QuotaId = quotaId,
                    QuotaVacacionalId = null,
                    Concepto = $"Cuota {quota.Mes} {quota.Anio} - {quota.Student?.NombreCompleto}",
                    Monto = quota.Monto
                };

                await _carritoRepo.AgregarDetalleAsync(detalle);

                await _carritoRepo.ActualizarMontoTotalAsync(carrito.CarritoId);

                TempData["SuccessMessage"] = "Cuota agregada al carrito exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al agregar al carrito: " + ex.Message;
                return RedirectToAction("CuotasPendientes", "ApoderadoDashboard");
            }
        }

       

        // POST: CarritoCompras/EliminarDetalle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDetalle(int detalleId)
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var carrito = await _carritoRepo.ObtenerCarritoActivoAsync(apoderadoId.Value);
                if (carrito == null)
                {
                    TempData["ErrorMessage"] = "No tiene un carrito activo";
                    return RedirectToAction(nameof(Index));
                }

                await _carritoRepo.EliminarDetalleAsync(detalleId);
                await _carritoRepo.ActualizarMontoTotalAsync(carrito.CarritoId);

                TempData["SuccessMessage"] = "Cuota eliminada del carrito";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CarritoCompras/Vaciar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vaciar()
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var carrito = await _carritoRepo.ObtenerCarritoActivoAsync(apoderadoId.Value);
                if (carrito != null)
                {
                    await _carritoRepo.VaciarCarritoAsync(carrito.CarritoId);
                    TempData["SuccessMessage"] = "Carrito vaciado exitosamente";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al vaciar carrito: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CarritoCompras/ProcederPago
        public async Task<IActionResult> ProcederPago()
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction("Login", "Account");
            }

            var carrito = await _carritoRepo.ObtenerCarritoActivoAsync(apoderadoId.Value);
            if (carrito == null)
            {
                TempData["ErrorMessage"] = "No tiene un carrito activo";
                return RedirectToAction(nameof(Index));
            }

            var detalles = await _carritoRepo.ObtenerDetallesCarritoAsync(carrito.CarritoId);
            if (!detalles.Any())
            {
                TempData["ErrorMessage"] = "El carrito está vacío";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Index", "PasarelaPago", new { carritoId = carrito.CarritoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarMultiple(int[] quotaIds, int[] quotaVacacionalIds)
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta";
                return RedirectToAction("Index", "ApoderadoDashboard");
            }

            var totalCuotas = (quotaIds?.Length ?? 0) + (quotaVacacionalIds?.Length ?? 0);
            if (totalCuotas == 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar al menos una cuota para agregar al carrito";
                return RedirectToAction("CuotasPendientes", "ApoderadoDashboard");
            }

            try
            {
                var carrito = await _carritoRepo.ObtenerCarritoActivoAsync(apoderadoId.Value);
                if (carrito == null)
                {
                    var nuevoCarrito = new CarritoCompras
                    {
                        LegalGuardianId = apoderadoId.Value,
                        FechaCreacion = DateTime.Now,
                        MontoTotal = 0,
                        Estado = "Activo",
                        IsActive = true
                    };

                    int carritoId = await _carritoRepo.CrearCarritoAsync(nuevoCarrito);
                    carrito = await _carritoRepo.ObtenerCarritoPorIdAsync(carritoId);

                    if (carrito == null)
                    {
                        TempData["ErrorMessage"] = "Error al crear el carrito. Intente nuevamente.";
                        return RedirectToAction("CuotasPendientes", "ApoderadoDashboard");
                    }
                }

                var detallesExistentes = await _carritoRepo.ObtenerDetallesCarritoAsync(carrito.CarritoId);
                int agregadas = 0;
                int duplicadas = 0;

                if (quotaIds != null && quotaIds.Length > 0)
                {
                    foreach (var quotaId in quotaIds)
                    {
                        if (detallesExistentes.Any(d => d.QuotaId == quotaId))
                        {
                            duplicadas++;
                            continue;
                        }

                        var quota = await _quotaRepo.ObtenerCuotaPorIdAsync(quotaId);
                        if (quota == null || quota.Student?.LegalGuardianId != apoderadoId || quota.PaymentStatus?.StatusName == "Pagado")
                        {
                            continue;
                        }

                        var detalle = new DetalleCarrito
                        {
                            CarritoId = carrito.CarritoId,
                            QuotaId = quotaId,
                            QuotaVacacionalId = null,
                            Concepto = $"Pensión {quota.Mes} {quota.Anio} - {quota.Student?.NombreCompleto}",
                            Monto = quota.Monto
                        };

                        await _carritoRepo.AgregarDetalleAsync(detalle);
                        agregadas++;
                    }
                }

                if (quotaVacacionalIds != null && quotaVacacionalIds.Length > 0)
                {
                    foreach (var quotaVacacionalId in quotaVacacionalIds)
                    {
                        if (detallesExistentes.Any(d => d.QuotaVacacionalId == quotaVacacionalId))
                        {
                            duplicadas++;
                            continue;
                        }

                        var quotaVac = await _quotaVacacionalRepo.ObtenerCuotaPorIdAsync(quotaVacacionalId);
                        if (quotaVac == null || quotaVac.Inscripcion?.LegalGuardianId != apoderadoId || quotaVac.PaymentStatus?.StatusName == "Pagado")
                        {
                            continue;
                        }

                        var detalle = new DetalleCarrito
                        {
                            CarritoId = carrito.CarritoId,
                            QuotaId = null,
                            QuotaVacacionalId = quotaVacacionalId,
                            Concepto = $"Curso Vacacional {quotaVac.Mes} - {quotaVac.Inscripcion?.CursoVacacional?.NombreCurso}",
                            Monto = quotaVac.Monto
                        };

                        await _carritoRepo.AgregarDetalleAsync(detalle);
                        agregadas++;
                    }
                }

                await _carritoRepo.ActualizarMontoTotalAsync(carrito.CarritoId);

                if (agregadas > 0)
                {
                    TempData["SuccessMessage"] = $"✅ {agregadas} cuota(s) agregada(s) al carrito exitosamente";
                    if (duplicadas > 0)
                    {
                        TempData["SuccessMessage"] += $" ({duplicadas} ya estaban en el carrito)";
                    }
                }
                else if (duplicadas > 0)
                {
                    TempData["ErrorMessage"] = "Las cuotas seleccionadas ya están en el carrito";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo agregar ninguna cuota al carrito";
                }

                return RedirectToAction("Index", "CarritoCompras");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al agregar cuotas: " + ex.Message;
                return RedirectToAction("CuotasPendientes", "ApoderadoDashboard");
            }
        }
    }
}