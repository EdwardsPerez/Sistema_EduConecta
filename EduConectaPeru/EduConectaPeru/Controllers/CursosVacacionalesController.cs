using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class CursosVacacionalesController : Controller
    {
        private readonly CursoVacacionalRepositoryADO _cursoRepo;

        public CursosVacacionalesController(CursoVacacionalRepositoryADO cursoRepo)
        {
            _cursoRepo = cursoRepo;
        }

        // GET: CursosVacacionales
        public async Task<IActionResult> Index()
        {
            var cursos = await _cursoRepo.ObtenerCursosVacacionalesAsync();
            return View(cursos);
        }

        // GET: CursosVacacionales/Details/
        public async Task<IActionResult> Details(int id)
        {
            var curso = await _cursoRepo.ObtenerCursoVacacionalPorIdAsync(id);
            if (curso == null)
            {
                return NotFound();
            }
            return View(curso);
        }

        // GET: CursosVacacionales/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CursosVacacionales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CursoVacacional curso)
        {
            if (await _cursoRepo.ExisteNombreAsync(curso.NombreCurso))
            {
                ModelState.AddModelError("NombreCurso", $"Ya existe un curso con el nombre '{curso.NombreCurso}'");
            }

            if (curso.FechaFin <= curso.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de finalización debe ser posterior a la fecha de inicio");
            }

            if (curso.Costo <= 0)
            {
                ModelState.AddModelError("Costo", "El costo debe ser mayor a 0");
            }

            if (curso.CapacidadMaxima <= 0)
            {
                ModelState.AddModelError("CapacidadMaxima", "La capacidad máxima debe ser mayor a 0");
            }

            if (!ModelState.IsValid)
            {
                return View(curso);
            }

            try
            {
                curso.CuposDisponibles = curso.CapacidadMaxima;
                curso.IsActive = true;
                curso.FechaCreacion = DateTime.Now;

                await _cursoRepo.AgregarCursoVacacionalAsync(curso);
                TempData["SuccessMessage"] = "Curso vacacional creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(curso);
            }
        }

        // GET: CursosVacacionales/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var curso = await _cursoRepo.ObtenerCursoVacacionalPorIdAsync(id);
            if (curso == null)
            {
                return NotFound();
            }

            if (!curso.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar un curso desactivado. Debe reactivarlo primero.";
                return RedirectToAction(nameof(Index));
            }

            return View(curso);
        }

        // POST: CursosVacacionales/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CursoVacacional curso)
        {
            if (id != curso.CursoVacacionalId)
            {
                return BadRequest();
            }

            if (await _cursoRepo.ExisteNombreAsync(curso.NombreCurso, curso.CursoVacacionalId))
            {
                ModelState.AddModelError("NombreCurso", $"Ya existe otro curso con el nombre '{curso.NombreCurso}'");
            }

            if (curso.FechaFin <= curso.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de finalización debe ser posterior a la fecha de inicio");
            }

            if (curso.Costo <= 0)
            {
                ModelState.AddModelError("Costo", "El costo debe ser mayor a 0");
            }

            if (curso.CapacidadMaxima <= 0)
            {
                ModelState.AddModelError("CapacidadMaxima", "La capacidad máxima debe ser mayor a 0");
            }

            if (!ModelState.IsValid)
            {
                return View(curso);
            }

            try
            {
                await _cursoRepo.ActualizarCursoVacacionalAsync(id, curso);
                TempData["SuccessMessage"] = "Curso vacacional actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(curso);
            }
        }

        // GET: CursosVacacionales/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var curso = await _cursoRepo.ObtenerCursoVacacionalPorIdAsync(id);
            if (curso == null)
            {
                return NotFound();
            }
            return View(curso);
        }

        // POST: CursosVacacionales/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _cursoRepo.EliminarCursoVacacionalAsync(id);
                TempData["SuccessMessage"] = "Curso vacacional desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: CursosVacacionales/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var curso = await _cursoRepo.ObtenerCursoVacacionalPorIdAsync(id);
                if (curso == null)
                {
                    TempData["ErrorMessage"] = "Curso no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (curso.IsActive)
                {
                    TempData["ErrorMessage"] = "El curso ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                await _cursoRepo.ReactivarCursoVacacionalAsync(id);
                TempData["SuccessMessage"] = "Curso vacacional reactivado exitosamente";
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