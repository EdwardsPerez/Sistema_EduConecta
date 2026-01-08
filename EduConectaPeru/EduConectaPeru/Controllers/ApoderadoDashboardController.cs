using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Models;
using EduConectaPeru.Data;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "ApoderadoOnly")]
    public class ApoderadoDashboardController : Controller
    {
        private readonly LegalGuardianRepositoryADO _guardianRepo;
        private readonly StudentRepositoryADO _studentRepo;
        private readonly QuotaRepositoryADO _quotaRepo;
        private readonly PaymentRepositoryADO _paymentRepo;
        private readonly MatriculaRepositoryADO _matriculaRepo;
        private readonly QuotaCursoVacacionalRepositoryADO _quotaVacacionalRepo;

        public ApoderadoDashboardController(
            LegalGuardianRepositoryADO guardianRepo,
            StudentRepositoryADO studentRepo,
            QuotaRepositoryADO quotaRepo,
            PaymentRepositoryADO paymentRepo,
            MatriculaRepositoryADO matriculaRepo,
            QuotaCursoVacacionalRepositoryADO quotaVacacionalRepo) 
        {
            _guardianRepo = guardianRepo;
            _studentRepo = studentRepo;
            _quotaRepo = quotaRepo;
            _paymentRepo = paymentRepo;
            _matriculaRepo = matriculaRepo;
            _quotaVacacionalRepo = quotaVacacionalRepo; 
        }

        private async Task<int?> ObtenerApoderadoIdAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return null;

            if (username.Length < 8)
                return null;

            string dni = username.Substring(username.Length - 8);
            var apoderado = await _guardianRepo.ObtenerApoderadoPorDNIAsync(dni);
            return apoderado?.LegalGuardianId;
        }

        // GET: ApoderadoDashboard
        public async Task<IActionResult> Index()
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta de apoderado";
                return RedirectToAction("Login", "Account");
            }

            var todosEstudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var misEstudiantes = todosEstudiantes.Where(e => e.LegalGuardianId == apoderadoId && e.IsActive).ToList();

            var cuotasPendientes = await _quotaRepo.ObtenerCuotasPendientesPorApoderadoAsync(apoderadoId.Value);

            var totalDeuda = cuotasPendientes.Sum(c => c.Monto);
            var cuotasVencidas = cuotasPendientes.Where(c => c.FechaVencimiento < DateTime.Today).Count();
            var proximosVencimientos = cuotasPendientes
                .Where(c => c.FechaVencimiento >= DateTime.Today && c.FechaVencimiento <= DateTime.Today.AddDays(7))
                .Count();

            var viewModel = new
            {
                MisEstudiantes = misEstudiantes,
                CantidadEstudiantes = misEstudiantes.Count,
                CantidadCuotasPendientes = cuotasPendientes.Count,
                TotalDeuda = totalDeuda,
                ProximosVencimientos = proximosVencimientos,
                CuotasVencidas = cuotasVencidas,
                UltimasCuotas = cuotasPendientes.OrderBy(c => c.FechaVencimiento).Take(5).ToList()
            };

            return View(viewModel);
        }

        // GET: ApoderadoDashboard/MisEstudiantes
        public async Task<IActionResult> MisEstudiantes()
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta de apoderado";
                return RedirectToAction("Login", "Account");
            }

            var todosEstudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var misEstudiantes = todosEstudiantes.Where(e => e.LegalGuardianId == apoderadoId).ToList();

            var todasMatriculas = await _matriculaRepo.ObtenerMatriculasAsync();

            foreach (var estudiante in misEstudiantes)
            {
                var matriculaActual = todasMatriculas
                    .Where(m => m.StudentId == estudiante.StudentId && m.IsActive)
                    .OrderByDescending(m => m.AnioEscolar)
                    .FirstOrDefault();

                estudiante.Matriculas = matriculaActual != null ?
                    new List<Matricula> { matriculaActual } : new List<Matricula>();
            }

            return View(misEstudiantes);
        }

        // GET: ApoderadoDashboard/CuotasPendientes
        public async Task<IActionResult> CuotasPendientes()
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta de apoderado";
                return RedirectToAction("Login", "Account");
            }

            var todosEstudiantes = await _studentRepo.ObtenerEstudiantesAsync();
            var estudiantes = todosEstudiantes.Where(e => e.LegalGuardianId == apoderadoId).ToList();

            var cuotasPendientesCombinadas = new List<object>();

            foreach (var estudiante in estudiantes)
            {
                var cuotasRegulares = await _quotaRepo.ObtenerCuotasPorEstudianteAsync(estudiante.StudentId);
                var cuotasRegularesPendientes = cuotasRegulares
                    .Where(c => c.PaymentStatus?.StatusName != "Pagado" && c.IsActive)
                    .ToList();

                foreach (var cuota in cuotasRegularesPendientes)
                {
                    cuotasPendientesCombinadas.Add(new
                    {
                        QuotaId = cuota.QuotaId,
                        QuotaVacacionalId = (int?)null,
                        StudentId = estudiante.StudentId,
                        StudentName = estudiante.NombreCompleto,
                        StudentDNI = estudiante.DNI,
                        Concepto = $"Pensión {cuota.Mes} {cuota.Anio}",
                        Monto = cuota.Monto,
                        FechaVencimiento = cuota.FechaVencimiento,
                        Estado = cuota.PaymentStatus?.StatusName ?? "Pendiente",
                        TipoCuota = "Regular"
                    });
                }

                var cuotasVacacionales = await _quotaVacacionalRepo.ObtenerCuotasPorEstudianteAsync(estudiante.StudentId);
                var cuotasVacacionalesPendientes = cuotasVacacionales
                    .Where(c => c.PaymentStatus?.StatusName != "Pagado" && c.IsActive)
                    .ToList();

                foreach (var cuotaVac in cuotasVacacionalesPendientes)
                {
                    cuotasPendientesCombinadas.Add(new
                    {
                        QuotaId = (int?)null,
                        QuotaVacacionalId = cuotaVac.QuotaVacacionalId, 
                        StudentId = estudiante.StudentId,
                        StudentName = estudiante.NombreCompleto,
                        StudentDNI = estudiante.DNI,
                        Concepto = $"Curso Vacacional - {cuotaVac.Mes}: {cuotaVac.Inscripcion?.CursoVacacional?.NombreCurso}",
                        Monto = cuotaVac.Monto,
                        FechaVencimiento = cuotaVac.FechaVencimiento,
                        Estado = cuotaVac.PaymentStatus?.StatusName ?? "Pendiente",
                        TipoCuota = "Vacacional"
                    });
                }
            }

            cuotasPendientesCombinadas = cuotasPendientesCombinadas
                .OrderBy(c => ((dynamic)c).FechaVencimiento)
                .ToList();

            ViewBag.CuotasPendientes = cuotasPendientesCombinadas;
            return View();
        }

        // GET: ApoderadoDashboard/HistorialPagos
        public async Task<IActionResult> HistorialPagos()
        {
            var apoderadoId = await ObtenerApoderadoIdAsync();
            if (apoderadoId == null)
            {
                TempData["ErrorMessage"] = "No se pudo identificar su cuenta de apoderado";
                return RedirectToAction("Login", "Account");
            }

            var todosPagos = await _paymentRepo.ObtenerPagosAsync();
            var misPagos = todosPagos
                .Where(p => p.Student != null && p.Student.LegalGuardianId == apoderadoId)
                .OrderByDescending(p => p.FechaPago)
                .ToList();

            return View(misPagos);
        }
    }
}