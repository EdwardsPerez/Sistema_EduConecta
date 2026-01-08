using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EduConectaPeru.Data;
using EduConectaPeru.Models;

namespace EduConectaPeru.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepositoryADO _userRepository;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserRepositoryADO userRepository, ILogger<AccountController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        // GET: Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = returnUrl;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError(string.Empty, "Usuario y contraseña son requeridos");
                    return View();
                }

                var user = await _userRepository.ObtenerUsuarioPorUsernameAsync(username);

                if (user == null)
                {
                    _logger.LogWarning("Intento de login fallido: Usuario {Username} no encontrado", username);
                    ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos");
                    return View();
                }

                bool isPasswordValid = VerifyPassword(password, user.PasswordHash);

                if (!isPasswordValid)
                {
                    _logger.LogWarning("Intento de login fallido: Contraseña incorrecta para usuario {Username}", username);
                    ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos");
                    return View();
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Intento de login fallido: Usuario {Username} está inactivo", username);
                    ModelState.AddModelError(string.Empty, "Usuario inactivo. Contacte al administrador");
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.UserId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
                };

                if (user.Role == "Apoderado")
                {
                    claims.Add(new Claim("ApoderadoUsername", user.Username));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    authProperties
                );

                _logger.LogInformation("Usuario {Username} con rol {Role} inició sesión exitosamente", user.Username, user.Role);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de login");
                ModelState.AddModelError(string.Empty, "Error al procesar el inicio de sesión. Intente nuevamente.");
                return View();
            }
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var username = User.Identity?.Name;
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("Usuario {Username} cerró sesión", username);
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el logout");
                return RedirectToAction("Login", "Account");
            }
        }

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Account/Register 
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register 
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password, string role)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
                {
                    ModelState.AddModelError(string.Empty, "Todos los campos son requeridos");
                    return View();
                }

                var existingUser = await _userRepository.ObtenerUsuarioPorUsernameAsync(username);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "El nombre de usuario ya existe");
                    return View();
                }

                string hashedPassword = HashPassword(password);

                var newUser = new User
                {
                    Username = username,
                    PasswordHash = hashedPassword,
                    Role = role,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await _userRepository.AgregarUsuarioAsync(newUser);

                _logger.LogInformation("Nuevo usuario {Username} creado exitosamente", username);
                TempData["SuccessMessage"] = "Usuario creado exitosamente";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                ModelState.AddModelError(string.Empty, "Error al crear el usuario");
                return View();
            }
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

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                if (storedHash.Contains(":"))
                {
                    var parts = storedHash.Split(':');
                    if (parts.Length != 2) return false;

                    string salt = parts[0];
                    string hash = parts[1];

                    string passwordWithSalt = salt + password;
                    byte[] hashBytes;
                    using (var sha256 = SHA256.Create())
                    {
                        hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
                    }
                    string computedHash = BitConverter.ToString(hashBytes).Replace("-", "");

                    return hash.Equals(computedHash, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    return storedHash == password;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}