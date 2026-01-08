using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(255)]
        [Display(Name = "Contraseña Hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Rol")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}