using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ConfiguracionCostosController : Controller
    {
        private readonly ConfiguracionCostoRepositoryADO _configuracionRepo;

        public ConfiguracionCostosController(ConfiguracionCostoRepositoryADO configuracionRepo)
        {
            _configuracionRepo = configuracionRepo;
        }

        // GET: ConfiguracionCostos
        public async Task<IActionResult> Index()
        {
            var configuraciones = await _configuracionRepo.ObtenerTodasConfiguracionesAsync();
            return View(configuraciones);
        }

        // GET: ConfiguracionCostos/Details/
        public async Task<IActionResult> Details(int id)
        {
            var configuracion = await _configuracionRepo.ObtenerConfiguracionPorIdAsync(id);
            if (configuracion == null)
            {
                return NotFound();
            }
            return View(configuracion);
        }

        // GET: ConfiguracionCostos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ConfiguracionCostos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ConfiguracionCosto configuracion)
        {
            if (configuracion.TipoCosto != "Matricula" && configuracion.TipoCosto != "Pension")
            {
                ModelState.AddModelError("TipoCosto", "El tipo de costo debe ser 'Matricula' o 'Pension'");
            }

            if (configuracion.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto debe ser mayor a 0");
            }

            if (configuracion.AnioEscolar < 2024 || configuracion.AnioEscolar > 2050)
            {
                ModelState.AddModelError("AnioEscolar", "El año escolar debe estar entre 2024 y 2050");
            }

            if (configuracion.GradoSeccionId.HasValue)
            {
                var existe = await _configuracionRepo.ExisteConfiguracionAsync(
                    configuracion.TipoCosto,
                    configuracion.GradoSeccionId,
                    configuracion.AnioEscolar,
                    null);

                if (existe)
                {
                    ModelState.AddModelError("", $"Ya existe una configuración de {configuracion.TipoCosto} para Grado {configuracion.GradoSeccionId} en el año {configuracion.AnioEscolar}");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(configuracion);
            }

            try
            {
                configuracion.IsActive = true;
                configuracion.FechaVigencia = DateTime.Now;

                await _configuracionRepo.AgregarConfiguracionAsync(configuracion);
                TempData["SuccessMessage"] = "Configuración de costo creada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(configuracion);
            }
        }

        // GET: ConfiguracionCostos/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var configuracion = await _configuracionRepo.ObtenerConfiguracionPorIdAsync(id);
            if (configuracion == null)
            {
                return NotFound();
            }

            if (!configuracion.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar una configuración desactivada. Debe reactivarla primero.";
                return RedirectToAction(nameof(Index));
            }

            return View(configuracion);
        }

        // POST: ConfiguracionCostos/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ConfiguracionCosto configuracion)
        {
            if (id != configuracion.ConfigId)
            {
                return BadRequest();
            }

            if (configuracion.TipoCosto != "Matricula" && configuracion.TipoCosto != "Pension")
            {
                ModelState.AddModelError("TipoCosto", "El tipo de costo debe ser 'Matricula' o 'Pension'");
            }

            if (configuracion.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto debe ser mayor a 0");
            }

            if (configuracion.AnioEscolar < 2024 || configuracion.AnioEscolar > 2050)
            {
                ModelState.AddModelError("AnioEscolar", "El año escolar debe estar entre 2024 y 2050");
            }

            if (configuracion.GradoSeccionId.HasValue)
            {
                var existe = await _configuracionRepo.ExisteConfiguracionAsync(
                    configuracion.TipoCosto,
                    configuracion.GradoSeccionId,
                    configuracion.AnioEscolar,
                    id);

                if (existe)
                {
                    ModelState.AddModelError("", $"Ya existe otra configuración de {configuracion.TipoCosto} para Grado {configuracion.GradoSeccionId} en el año {configuracion.AnioEscolar}");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(configuracion);
            }

            try
            {
                await _configuracionRepo.ActualizarConfiguracionAsync(id, configuracion);
                TempData["SuccessMessage"] = "Configuración de costo actualizada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(configuracion);
            }
        }

        // GET: ConfiguracionCostos/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var configuracion = await _configuracionRepo.ObtenerConfiguracionPorIdAsync(id);
            if (configuracion == null)
            {
                return NotFound();
            }
            return View(configuracion);
        }

        // POST: ConfiguracionCostos/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _configuracionRepo.DesactivarConfiguracionAsync(id);
                TempData["SuccessMessage"] = "Configuración de costo desactivada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: ConfiguracionCostos/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var configuracion = await _configuracionRepo.ObtenerConfiguracionPorIdAsync(id);
                if (configuracion == null)
                {
                    TempData["ErrorMessage"] = "Configuración no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (configuracion.IsActive)
                {
                    TempData["ErrorMessage"] = "La configuración ya está activa";
                    return RedirectToAction(nameof(Index));
                }

                await _configuracionRepo.ReactivarConfiguracionAsync(id);
                TempData["SuccessMessage"] = "Configuración de costo reactivada exitosamente";
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