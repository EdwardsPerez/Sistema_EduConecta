using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class Matricula
    {
        public int MatriculaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un estudiante")]
        [Display(Name = "Estudiante")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un apoderado")]
        [Display(Name = "Apoderado")]
        public int LegalGuardianId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un grado/sección")]
        [Display(Name = "Grado/Sección")]
        public int GradoSeccionId { get; set; }

        [Required(ErrorMessage = "El año escolar es obligatorio")]
        [Display(Name = "Año Escolar")]
        public int AnioEscolar { get; set; }

        [Display(Name = "Fecha de Matrícula")]
        public DateTime FechaMatricula { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El monto de matrícula es obligatorio")]
        [Range(0.01, 9999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto Matrícula")]
        public decimal MontoMatricula { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activa";

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

   
        public Student? Student { get; set; }
        public LegalGuardian? LegalGuardian { get; set; }
        public GradoSeccion? GradoSeccion { get; set; }
    }
}
