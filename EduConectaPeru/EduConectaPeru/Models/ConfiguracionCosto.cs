using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class ConfiguracionCosto
    {
        public int ConfigId { get; set; }

        [Display(Name = "Grado/Sección")]
        public int? GradoSeccionId { get; set; }

        [Required(ErrorMessage = "El tipo de costo es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Tipo de Costo")]
        public string TipoCosto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, 9999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El año escolar es obligatorio")]
        [Display(Name = "Año Escolar")]
        public int AnioEscolar { get; set; }

        [Display(Name = "Fecha de Vigencia")]
        public DateTime FechaVigencia { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Modificación")]
        public DateTime? FechaModificacion { get; set; }

        [StringLength(100)]
        [Display(Name = "Usuario que Modificó")]
        public string? UsuarioModificacion { get; set; }

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        public int? PaymentTypeId { get; set; }
        public int? BankId { get; set; }

     
        public PaymentType? PaymentType { get; set; }
        public Bank? Bank { get; set; }
        public GradoSeccion? GradoSeccion { get; set; }
    }
}
