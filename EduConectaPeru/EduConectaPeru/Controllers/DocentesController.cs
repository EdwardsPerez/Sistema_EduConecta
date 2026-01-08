using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class DocentesController : Controller
    {
        private readonly DocenteRepositoryADO _docenteRepo;

        public DocentesController(DocenteRepositoryADO docenteRepo)
        {
            _docenteRepo = docenteRepo;
        }

        // GET: Docentes
        public async Task<IActionResult> Index()
        {
            var docentes = await _docenteRepo.ObtenerDocentesAsync();
            return View(docentes);
        }

        // GET: Docentes/Details/
        public async Task<IActionResult> Details(int id)
        {
            var docente = await _docenteRepo.ObtenerDocentePorIdAsync(id);
            if (docente == null)
            {
                return NotFound();
            }

            return View(docente);
        }

        // GET: Docentes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Docentes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Docente docente)
        {
            if (await _docenteRepo.ExisteDNIAsync(docente.DNI))
            {
                ModelState.AddModelError("DNI", $"El DNI {docente.DNI} ya está registrado");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(docente.DNI, @"^\d{8}$"))
            {
                ModelState.AddModelError("DNI", "El DNI debe tener exactamente 8 dígitos");
            }

            if (!string.IsNullOrEmpty(docente.Telefono))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(docente.Telefono, @"^\d{9}$"))
                {
                    ModelState.AddModelError("Telefono", "El teléfono debe tener 9 dígitos");
                }
            }

            if (!string.IsNullOrEmpty(docente.Email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(docente.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError("Email", "Ingrese un email válido");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(docente);
            }

            try
            {
                await _docenteRepo.AgregarDocenteAsync(docente);
                TempData["SuccessMessage"] = "Docente registrado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(docente);
            }
        }

        // GET: Docentes/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var docente = await _docenteRepo.ObtenerDocentePorIdAsync(id);
            if (docente == null)
            {
                return NotFound();
            }

            if (!docente.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar un docente desactivado. Debe reactivarlo primero.";
                return RedirectToAction(nameof(Index));
            }

            return View(docente);
        }
        // POST: Docentes/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Docente docente)
        {
            if (id != docente.DocenteId)
            {
                return BadRequest();
            }

            if (await _docenteRepo.ExisteDNIAsync(docente.DNI, docente.DocenteId))
            {
                ModelState.AddModelError("DNI", $"El DNI {docente.DNI} ya está registrado en otro docente");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(docente.DNI, @"^\d{8}$"))
            {
                ModelState.AddModelError("DNI", "El DNI debe tener exactamente 8 dígitos");
            }

            if (!string.IsNullOrEmpty(docente.Telefono))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(docente.Telefono, @"^\d{9}$"))
                {
                    ModelState.AddModelError("Telefono", "El teléfono debe tener 9 dígitos");
                }
            }

            if (!string.IsNullOrEmpty(docente.Email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(docente.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError("Email", "Ingrese un email válido");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(docente);
            }

            try
            {
                await _docenteRepo.ActualizarDocenteAsync(id, docente);
                TempData["SuccessMessage"] = "Docente actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(docente);
            }
        }

        // GET: Docentes/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var docente = await _docenteRepo.ObtenerDocentePorIdAsync(id);
            if (docente == null)
            {
                return NotFound();
            }

            return View(docente);
        }

        // POST: Docentes/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _docenteRepo.EliminarDocenteAsync(id);
                TempData["SuccessMessage"] = "Docente desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
        // POST: Docentes/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var docente = await _docenteRepo.ObtenerDocentePorIdAsync(id);
                if (docente == null)
                {
                    TempData["ErrorMessage"] = "Docente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (docente.IsActive)
                {
                    TempData["ErrorMessage"] = "El docente ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                await _docenteRepo.ReactivarDocenteAsync(id);
                TempData["SuccessMessage"] = $"Docente {docente.NombreCompleto} reactivado exitosamente";
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