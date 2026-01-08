using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class Quota
    {
        public int QuotaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una matrícula")]
        [Display(Name = "Matrícula")]
        public int MatriculaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un estudiante")]
        [Display(Name = "Estudiante")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "El mes es obligatorio")]
        [StringLength(20)]
        [Display(Name = "Mes")]
        public string Mes { get; set; } = string.Empty;

        [Required(ErrorMessage = "El año es obligatorio")]
        [Display(Name = "Año")]
        public int Anio { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, 9999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        [Required(ErrorMessage = "El estado de pago es obligatorio")]
        [Display(Name = "Estado de Pago")]
        public int PaymentStatusId { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;


        public Matricula? Matricula { get; set; }
        public Student? Student { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        [Display(Name = "Período")]
        public string Periodo => $"{Mes} {Anio}";
    }
}
