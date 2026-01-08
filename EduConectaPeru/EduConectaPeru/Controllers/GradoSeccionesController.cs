using EduConectaPeru.Data;
using EduConectaPeru.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class GradoSeccionesController : Controller
    {
        private readonly GradoSeccionRepositoryADO _gradoSeccionRepo;

        public GradoSeccionesController(GradoSeccionRepositoryADO gradoSeccionRepo)
        {
            _gradoSeccionRepo = gradoSeccionRepo;
        }

        // GET: GradoSecciones
        public async Task<IActionResult> Index()
        {
            var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();
            return View(gradoSecciones);
        }

        // GET: GradoSecciones/Details/
        public async Task<IActionResult> Details(int id)
        {
            var gradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(id);
            if (gradoSeccion == null)
            {
                return NotFound();
            }

            return View(gradoSeccion);
        }

        // GET: GradoSecciones/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: GradoSecciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GradoSeccion gradoSeccion)
        {
            if (gradoSeccion.Capacidad < 1 || gradoSeccion.Capacidad > 50)
            {
                ModelState.AddModelError("Capacidad", "La capacidad debe estar entre 1 y 50 estudiantes");
            }

            var existe = await _gradoSeccionRepo.ExisteCombinacionAsync(
                gradoSeccion.Grado,
                gradoSeccion.Seccion,
                gradoSeccion.AnioEscolar,
                null);

            if (existe)
            {
                ModelState.AddModelError("", $"Ya existe una sección '{gradoSeccion.Seccion}' para el grado '{gradoSeccion.Grado}' en el año {gradoSeccion.AnioEscolar}");
            }

            if (!ModelState.IsValid)
            {
                return View(gradoSeccion);
            }

            try
            {
                gradoSeccion.IsActive = true;
                await _gradoSeccionRepo.AgregarGradoSeccionAsync(gradoSeccion);
                TempData["SuccessMessage"] = "Grado y Sección creados exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(gradoSeccion);
            }
        }

        // GET: GradoSecciones/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var gradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(id);
            if (gradoSeccion == null)
            {
                return NotFound();
            }

            if (!gradoSeccion.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar un grado/sección desactivado. Debe reactivarlo primero.";
                return RedirectToAction(nameof(Index));
            }

            return View(gradoSeccion);
        }

        // POST: GradoSecciones/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GradoSeccion gradoSeccion)
        {
            if (id != gradoSeccion.GradoSeccionId)
            {
                return BadRequest();
            }

            if (gradoSeccion.Capacidad < 1 || gradoSeccion.Capacidad > 50)
            {
                ModelState.AddModelError("Capacidad", "La capacidad debe estar entre 1 y 50 estudiantes");
            }

            var existe = await _gradoSeccionRepo.ExisteCombinacionAsync(
                gradoSeccion.Grado,
                gradoSeccion.Seccion,
                gradoSeccion.AnioEscolar,
                id);

            if (existe)
            {
                ModelState.AddModelError("", $"Ya existe otra sección '{gradoSeccion.Seccion}' para el grado '{gradoSeccion.Grado}' en el año {gradoSeccion.AnioEscolar}");
            }

            if (!ModelState.IsValid)
            {
                return View(gradoSeccion);
            }

            try
            {
                await _gradoSeccionRepo.ActualizarGradoSeccionAsync(id, gradoSeccion);
                TempData["SuccessMessage"] = "Grado y Sección actualizados exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(gradoSeccion);
            }
        }

        // GET: GradoSecciones/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var gradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(id);
            if (gradoSeccion == null)
            {
                return NotFound();
            }

            return View(gradoSeccion);
        }

        // POST: GradoSecciones/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _gradoSeccionRepo.EliminarGradoSeccionAsync(id);
                TempData["SuccessMessage"] = "Grado/Sección desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: GradoSecciones/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var gradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(id);
                if (gradoSeccion == null)
                {
                    TempData["ErrorMessage"] = "Grado/Sección no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (gradoSeccion.IsActive)
                {
                    TempData["ErrorMessage"] = "El grado/sección ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                await _gradoSeccionRepo.ReactivarGradoSeccionAsync(id);
                TempData["SuccessMessage"] = $"Grado/Sección {gradoSeccion.GradoSeccionNombre} reactivado exitosamente";
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