using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class Docente
    {
        public int DocenteId { get; set; }

        [Required(ErrorMessage = "El DNI es obligatorio")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "El DNI debe tener 8 dígitos")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe contener solo números")]
        [Display(Name = "DNI")]
        public string DNI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Especialidad")]
        public string? Especialidad { get; set; }

        [StringLength(15)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Fecha de Contratación")]
        public DateTime FechaContratacion { get; set; } = DateTime.Now;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Nombre Completo")]
        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}
