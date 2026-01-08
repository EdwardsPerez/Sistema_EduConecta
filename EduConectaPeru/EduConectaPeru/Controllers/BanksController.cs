using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class BanksController : Controller
    {
        private readonly BankRepositoryADO _bankRepo;

        public BanksController(BankRepositoryADO bankRepo)
        {
            _bankRepo = bankRepo;
        }

        // GET: Banks
        public async Task<IActionResult> Index()
        {
            var bancos = await _bankRepo.ObtenerBancosAsync();
            return View(bancos);
        }

        // GET: Banks/Details/
        public async Task<IActionResult> Details(int id)
        {
            var banco = await _bankRepo.ObtenerBancoPorIdAsync(id);
            if (banco == null)
            {
                return NotFound();
            }

            return View(banco);
        }

        // GET: Banks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Banks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bank banco)
        {
            if (!ModelState.IsValid)
            {
                return View(banco);
            }

            try
            {
                await _bankRepo.AgregarBancoAsync(banco);
                TempData["SuccessMessage"] = "Banco agregado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(banco);
            }
        }

        // GET: Banks/Edit/
        public async Task<IActionResult> Edit(int id)
        {
            var banco = await _bankRepo.ObtenerBancoPorIdAsync(id);
            if (banco == null)
            {
                return NotFound();
            }

            if (!banco.IsActive)
            {
                TempData["ErrorMessage"] = "No se puede editar un banco desactivado. Debe reactivarlo primero.";
                return RedirectToAction(nameof(Index));
            }

            return View(banco);
        }

        // POST: Banks/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bank banco)
        {
            if (id != banco.BankId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(banco);
            }

            try
            {
                await _bankRepo.ActualizarBancoAsync(id, banco);
                TempData["SuccessMessage"] = "Banco actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                return View(banco);
            }
        }

        // GET: Banks/Delete/
        public async Task<IActionResult> Delete(int id)
        {
            var banco = await _bankRepo.ObtenerBancoPorIdAsync(id);
            if (banco == null)
            {
                return NotFound();
            }

            return View(banco);
        }

        // POST: Banks/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _bankRepo.EliminarBancoAsync(id);
                TempData["SuccessMessage"] = "Banco desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desactivar: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
        // POST: Banks/Reactivar/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int id)
        {
            try
            {
                var bank = await _bankRepo.ObtenerBancoPorIdAsync(id);
                if (bank == null)
                {
                    TempData["ErrorMessage"] = "Banco no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (bank.IsActive)
                {
                    TempData["ErrorMessage"] = "El banco ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                await _bankRepo.ReactivarAsync(id);
                TempData["SuccessMessage"] = $"Banco {bank.BankName} reactivado exitosamente";
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