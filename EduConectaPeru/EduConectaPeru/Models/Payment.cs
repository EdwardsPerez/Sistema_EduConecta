using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una cuota")]
        [Display(Name = "Cuota")]
        public int QuotaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un estudiante")]
        [Display(Name = "Estudiante")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, 9999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Display(Name = "Fecha de Pago")]
        public DateTime FechaPago { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El tipo de pago es obligatorio")]
        [Display(Name = "Tipo de Pago")]
        public int PaymentTypeId { get; set; }

        [Display(Name = "Banco")]
        public int? BankId { get; set; }

        [StringLength(50)]
        [Display(Name = "Número de Operación")]
        public string? NumeroOperacion { get; set; }

        [StringLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

    
        public Quota? Quota { get; set; }
        public Student? Student { get; set; }
        public PaymentType? PaymentType { get; set; }
        public Bank? Bank { get; set; }
    }
}
