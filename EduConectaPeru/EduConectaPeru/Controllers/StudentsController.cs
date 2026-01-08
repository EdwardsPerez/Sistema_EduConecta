using EduConectaPeru.Data;
using EduConectaPeru.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class StudentsController : Controller
    {
        private readonly StudentRepositoryADO _studentRepo;
        private readonly LegalGuardianRepositoryADO _guardianRepo;

        public StudentsController(StudentRepositoryADO studentRepo, LegalGuardianRepositoryADO guardianRepo)
        {
            _studentRepo = studentRepo;
            _guardianRepo = guardianRepo;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            var estudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            return View(estudiantes);
        }

        // GET: Students/Details
        public async Task<IActionResult> Details(int id)
        {
            var estudiante = await _studentRepo.ObtenerEstudiantePorIdAsync(id);
            if (estudiante == null)
            {
                return NotFound();
            }

            estudiante.LegalGuardian = await _guardianRepo.ObtenerApoderadoPorIdAsync(estudiante.LegalGuardianId);
            return View(estudiante);
        }

        // GET: Students/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Apoderados = await _guardianRepo.ObtenerApoderadosAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            student.FechaRegistro = DateTime.Now;
            student.IsActive = true;

            if (await _studentRepo.ExisteDNIAsync(student.DNI))
            {
                ModelState.AddModelError("DNI", $"El DNI {student.DNI} ya está registrado en el sistema");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(student.DNI, @"^\d{8}$"))
            {
                ModelState.AddModelError("DNI", "El DNI debe tener exactamente 8 dígitos numéricos");
            }

            if (!string.IsNullOrEmpty(student.Telefono))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(student.Telefono, @"^\d{9}$"))
                {
                    ModelState.AddModelError("Telefono", "El teléfono debe tener exactamente 9 dígitos numéricos");
                }
            }

            if (!string.IsNullOrEmpty(student.Email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(student.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError("Email", "Ingrese un correo electrónico válido");
                }
            }

            if (student.FechaNacimiento > DateTime.Now)
            {
                ModelState.AddModelError("FechaNacimiento", "La fecha de nacimiento no puede ser futura");
            }

            var edad = DateTime.Now.Year - student.FechaNacimiento.Year;
            if (student.FechaNacimiento > DateTime.Now.AddYears(-edad)) edad--;

            if (edad < 3)
            {
                ModelState.AddModelError("FechaNacimiento", "El estudiante debe tener al menos 3 años");
            }

            if (edad > 18)
            {
                ModelState.AddModelError("FechaNacimiento", "El estudiante no puede tener más de 18 años");
            }

            if (!ModelState.IsValid)
            {
                if (isAjaxRequest)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    return Json(new { success = false, errors = errors });
                }

                ViewBag.Apoderados = await _guardianRepo.ObtenerApoderadosAsync();
                return View(student);
            }

            try
            {
                await _studentRepo.AgregarEstudianteAsync(student);
                TempData["SuccessMessage"] = "Estudiante registrado exitosamente";

                if (isAjaxRequest)
                {
                    return Json(new { success = true, message = "Estudiante registrado exitosamente" });
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
                ViewBag.Apoderados = await _guardianRepo.ObtenerApoderadosAsync();
                return View(student);
            }
        }

        // GET: Students/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _studentRepo.ObtenerEstudiantePorIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            if (!student.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar un estudiante desactivado. Debe reactivarlo primero.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Apoderados = await _guardianRepo.ObtenerApoderadosAsync();

            return View(student);
        }

        // POST: Students/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.StudentId)
            {
                return BadRequest();
            }

            if (await _studentRepo.ExisteDNIAsync(student.DNI, student.StudentId))
            {
                ModelState.AddModelError("DNI", $"El DNI {student.DNI} ya está registrado en otro estudiante");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(student.DNI, @"^\d{8}$"))
            {
                ModelState.AddModelError("DNI", "El DNI debe tener exactamente 8 dígitos numéricos");
            }

            if (!string.IsNullOrEmpty(student.Telefono))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(student.Telefono, @"^\d{9}$"))
                {
                    ModelState.AddModelError("Telefono", "El teléfono debe tener exactamente 9 dígitos numéricos");
                }
            }

            if (!string.IsNullOrEmpty(student.Email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(student.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError("Email", "Ingrese un correo electrónico válido");
                }
            }

            if (student.FechaNacimiento > DateTime.Now)
            {
                ModelState.AddModelError("FechaNacimiento", "La fecha de nacimiento no puede ser futura");
            }

            var edad = DateTime.Now.Year - student.FechaNacimiento.Year;
            if (student.FechaNacimiento > DateTime.Now.AddYears(-edad)) edad--;

            if (edad < 3)
            {
                ModelState.AddModelError("FechaNacimiento", "El estudiante debe tener al menos 3 años");
            }

            if (edad > 18)
            {
                ModelState.AddModelError("FechaNacimiento", "El estudiante no puede tener más de 18 años");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Apoderados = await _guardianRepo.ObtenerApoderadosAsync();
                return View(student);
            }

            try
            {
                await _studentRepo.ActualizarEstudianteAsync(id, student);
                TempData["SuccessMessage"] = "Estudiante actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                ViewBag.Apoderados = await _guardianRepo.ObtenerApoderadosAsync();
                return View(student);
            }
        }

        // GET: Students/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var estudiante = await _studentRepo.ObtenerEstudiantePorIdAsync(id);
            if (estudiante == null)
            {
                return NotFound();
            }

            estudiante.LegalGuardian = await _guardianRepo.ObtenerApoderadoPorIdAsync(estudiante.LegalGuardianId);
            return View(estudiante);
        }

        // POST: Students/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _studentRepo.EliminarEstudianteAsync(id);
                TempData["SuccessMessage"] = "Estudiante desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: Students/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var student = await _studentRepo.ObtenerEstudiantePorIdAsync(id);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Estudiante no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (student.IsActive)
                {
                    TempData["ErrorMessage"] = "El estudiante ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                await _studentRepo.ReactivarEstudianteAsync(id);
                TempData["SuccessMessage"] = $"Estudiante {student.NombreCompleto} reactivado exitosamente";
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