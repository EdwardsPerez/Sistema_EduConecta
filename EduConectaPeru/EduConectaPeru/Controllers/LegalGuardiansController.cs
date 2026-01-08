using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;
using System.Security.Cryptography;
using System.Text;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class LegalGuardiansController : Controller
    {
        private readonly LegalGuardianRepositoryADO _guardianRepo;
        private readonly UserRepositoryADO _userRepo;

        public LegalGuardiansController(
            LegalGuardianRepositoryADO guardianRepo,
            UserRepositoryADO userRepo)
        {
            _guardianRepo = guardianRepo;
            _userRepo = userRepo;
        }

        // GET: LegalGuardians
        public async Task<IActionResult> Index()
        {
            var apoderados = await _guardianRepo.ObtenerApoderadosAsync();
            return View(apoderados);
        }

        // GET: LegalGuardians/Details/
        public async Task<IActionResult> Details(int id)
        {
            var apoderado = await _guardianRepo.ObtenerApoderadoPorIdAsync(id);
            if (apoderado == null)
            {
                return NotFound();
            }

            return View(apoderado);
        }

        // GET: LegalGuardians/Create
        public IActionResult Create()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = BitConverter.ToString(saltBytes).Replace("-", "");

            string passwordWithSalt = salt + password;
            byte[] hashBytes;
            using (var sha256 = SHA256.Create())
            {
                hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
            }
            string hash = BitConverter.ToString(hashBytes).Replace("-", "");

            return $"{salt}:{hash}";
        }

        // POST: LegalGuardians/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LegalGuardian apoderado)
        {
            if (await _guardianRepo.ExisteDNIAsync(apoderado.DNI))
            {
                ModelState.AddModelError("DNI", $"El DNI {apoderado.DNI} ya está registrado");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(apoderado.DNI, @"^\d{8}$"))
            {
                ModelState.AddModelError("DNI", "El DNI debe tener exactamente 8 dígitos");
            }

            if (!string.IsNullOrEmpty(apoderado.Telefono))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(apoderado.Telefono, @"^\d{9}$"))
                {
                    ModelState.AddModelError("Telefono", "El teléfono debe tener 9 dígitos");
                }
            }

            if (!string.IsNullOrEmpty(apoderado.Email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(apoderado.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError("Email", "Ingrese un email válido");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(apoderado);
            }

            try
            {
                apoderado.FechaRegistro = DateTime.Now;
                apoderado.IsActive = true;

                int apoderadoId = await _guardianRepo.AgregarApoderadoAsync(apoderado);

                string nombresPrimeros = apoderado.Nombre.Split(' ')[0];
                string usuario = $"{nombresPrimeros}{apoderado.DNI}";
                string contrasena = apoderado.DNI;

                var usuarioExistente = await _userRepo.ObtenerUsuarioPorUsernameAsync(usuario);
                if (usuarioExistente == null)
                {
                    string contrasenaHasheada = HashPassword(contrasena);

                    var nuevoUsuario = new User
                    {
                        Username = usuario,
                        PasswordHash = contrasenaHasheada,
                        Role = "Apoderado",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    await _userRepo.AgregarUsuarioAsync(nuevoUsuario);
                }

                TempData["UsuarioGenerado"] = usuario;
                TempData["ContrasenaGenerada"] = contrasena;
                TempData["NombreApoderado"] = apoderado.NombreCompleto;
                TempData["DNIApoderado"] = apoderado.DNI;

                return RedirectToAction("Credenciales");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(apoderado);
            }
        }

        // GET: LegalGuardians/Credenciales
        public IActionResult Credenciales()
        {
            if (TempData["UsuarioGenerado"] == null ||
                TempData["ContrasenaGenerada"] == null ||
                TempData["NombreApoderado"] == null)
            {
                TempData["ErrorMessage"] = "No hay credenciales para mostrar";
                return RedirectToAction(nameof(Index));
            }

            TempData.Keep("UsuarioGenerado");
            TempData.Keep("ContrasenaGenerada");
            TempData.Keep("NombreApoderado");
            TempData.Keep("DNIApoderado");

            return View();
        }

        // GET: LegalGuardians/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var apoderado = await _guardianRepo.ObtenerApoderadoPorIdAsync(id);
            if (apoderado == null)
            {
                return NotFound();
            }

            if (!apoderado.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar un apoderado desactivado. Debe reactivarlo primero.";
                return RedirectToAction(nameof(Index));
            }

            return View(apoderado);
        }

        // POST: LegalGuardians/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LegalGuardian apoderado)
        {
            if (id != apoderado.LegalGuardianId)
            {
                return BadRequest();
            }

            if (await _guardianRepo.ExisteDNIAsync(apoderado.DNI, apoderado.LegalGuardianId))
            {
                ModelState.AddModelError("DNI", $"El DNI {apoderado.DNI} ya está registrado en otro apoderado");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(apoderado.DNI, @"^\d{8}$"))
            {
                ModelState.AddModelError("DNI", "El DNI debe tener exactamente 8 dígitos");
            }

            if (!string.IsNullOrEmpty(apoderado.Telefono))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(apoderado.Telefono, @"^\d{9}$"))
                {
                    ModelState.AddModelError("Telefono", "El teléfono debe tener 9 dígitos");
                }
            }

            if (!string.IsNullOrEmpty(apoderado.Email))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(apoderado.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError("Email", "Ingrese un email válido");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(apoderado);
            }

            try
            {
                await _guardianRepo.ActualizarApoderadoAsync(id, apoderado);
                TempData["SuccessMessage"] = "Apoderado actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(apoderado);
            }
        }

        // GET: LegalGuardians/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var apoderado = await _guardianRepo.ObtenerApoderadoPorIdAsync(id);
            if (apoderado == null)
            {
                return NotFound();
            }

            return View(apoderado);
        }

        // POST: LegalGuardians/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _guardianRepo.EliminarApoderadoAsync(id);
                TempData["SuccessMessage"] = "Apoderado desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: LegalGuardians/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var apoderado = await _guardianRepo.ObtenerApoderadoPorIdAsync(id);
                if (apoderado == null)
                {
                    TempData["ErrorMessage"] = "Apoderado no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (apoderado.IsActive)
                {
                    TempData["ErrorMessage"] = "El apoderado ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                await _guardianRepo.ReactivarApoderadoAsync(id);
                TempData["SuccessMessage"] = $"Apoderado {apoderado.NombreCompleto} reactivado exitosamente";
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