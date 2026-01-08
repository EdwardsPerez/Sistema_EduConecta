using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class InscripcionCursoVacacional
    {
        public int InscripcionId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un curso vacacional")]
        [Display(Name = "Curso Vacacional")]
        public int CursoVacacionalId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un estudiante")]
        [Display(Name = "Estudiante")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un apoderado")]
        [Display(Name = "Apoderado")]
        public int LegalGuardianId { get; set; }

        [Display(Name = "Fecha de Inscripción")]
        public DateTime FechaInscripcion { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, 9999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activa";

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

 
        public CursoVacacional? CursoVacacional { get; set; }
        public Student? Student { get; set; }
        public LegalGuardian? LegalGuardian { get; set; }
    }
}
