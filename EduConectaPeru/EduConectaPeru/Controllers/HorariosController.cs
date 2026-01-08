using EduConectaPeru.Data;
using EduConectaPeru.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class HorariosController : Controller
    {
        private readonly HorarioRepositoryADO _horarioRepo;
        private readonly GradoSeccionRepositoryADO _gradoSeccionRepo;

        public HorariosController(
            HorarioRepositoryADO horarioRepo,
            GradoSeccionRepositoryADO gradoSeccionRepo)
        {
            _horarioRepo = horarioRepo;
            _gradoSeccionRepo = gradoSeccionRepo;
        }

        public async Task<IActionResult> Index()
        {
            var horarios = await _horarioRepo.ObtenerHorariosAsync();

            foreach (var horario in horarios)
            {
                horario.GradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(horario.GradoSeccionId);
            }

            return View(horarios);
        }

        public async Task<IActionResult> Details(int id)
        {
            var horario = await _horarioRepo.ObtenerHorarioPorIdAsync(id);
            if (horario == null)
            {
                return NotFound();
            }

            horario.GradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(horario.GradoSeccionId);
            return View(horario);
        }

        public async Task<IActionResult> Create()
        {
            var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();

            ViewBag.GradoSecciones = new SelectList(
                gradoSecciones.Where(g => g.IsActive),
                "GradoSeccionId",
                "GradoSeccionNombre"
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Horario horario)
        {
            if (!ModelState.IsValid)
            {
                var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();
                ViewBag.GradoSecciones = new SelectList(
                    gradoSecciones.Where(g => g.IsActive),
                    "GradoSeccionId",
                    "GradoSeccionNombre"
                );
                return View(horario);
            }

            try
            {
                horario.IsActive = true;
                await _horarioRepo.AgregarHorarioAsync(horario);
                TempData["SuccessMessage"] = "Horario registrado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();
                ViewBag.GradoSecciones = new SelectList(
                    gradoSecciones.Where(g => g.IsActive),
                    "GradoSeccionId",
                    "GradoSeccionNombre"
                );
                return View(horario);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var horario = await _horarioRepo.ObtenerHorarioPorIdAsync(id);
            if (horario == null)
            {
                return NotFound();
            }

            if (!horario.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar un horario desactivado. Debe reactivarlo primero.";
                return RedirectToAction(nameof(Index));
            }

            var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();
            ViewBag.GradoSecciones = new SelectList(
                gradoSecciones.Where(g => g.IsActive),
                "GradoSeccionId",
                "GradoSeccionNombre",
                horario.GradoSeccionId
            );

            return View(horario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Horario horario)
        {
            if (id != horario.HorarioId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();
                ViewBag.GradoSecciones = new SelectList(
                    gradoSecciones.Where(g => g.IsActive),
                    "GradoSeccionId",
                    "GradoSeccionNombre",
                    horario.GradoSeccionId
                );
                return View(horario);
            }

            try
            {
                await _horarioRepo.ActualizarHorarioAsync(id, horario);
                TempData["SuccessMessage"] = "Horario actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                var gradoSecciones = await _gradoSeccionRepo.ObtenerGradoSeccionesAsync();
                ViewBag.GradoSecciones = new SelectList(
                    gradoSecciones.Where(g => g.IsActive),
                    "GradoSeccionId",
                    "GradoSeccionNombre",
                    horario.GradoSeccionId
                );
                return View(horario);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var horario = await _horarioRepo.ObtenerHorarioPorIdAsync(id);
            if (horario == null)
            {
                return NotFound();
            }

            horario.GradoSeccion = await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(horario.GradoSeccionId);
            return View(horario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _horarioRepo.EliminarHorarioAsync(id);
                TempData["SuccessMessage"] = "Horario desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var horario = await _horarioRepo.ObtenerHorarioPorIdAsync(id);
                if (horario == null)
                {
                    TempData["ErrorMessage"] = "Horario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (horario.IsActive)
                {
                    TempData["ErrorMessage"] = "El horario ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                await _horarioRepo.ReactivarHorarioAsync(id);
                TempData["SuccessMessage"] = "Horario reactivado exitosamente";
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