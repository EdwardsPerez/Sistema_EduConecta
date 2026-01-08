using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class Horario
    {
        public int HorarioId { get; set; }

        [Display(Name = "Docente")]
        public int? DocenteId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un grado/sección")]
        [Display(Name = "Grado/Sección")]
        public int GradoSeccionId { get; set; }

        [Required(ErrorMessage = "El curso es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Curso")]
        public string Curso { get; set; } = string.Empty;

        [Required(ErrorMessage = "El día de la semana es obligatorio")]
        [StringLength(20)]
        [Display(Name = "Día de la Semana")]
        public string DiaSemana { get; set; } = string.Empty;

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Display(Name = "Hora de Inicio")]
        public TimeSpan HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        [Display(Name = "Hora de Fin")]
        public TimeSpan HoraFin { get; set; }

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

      
        public GradoSeccion? GradoSeccion { get; set; }
        public Docente? Docente { get; set; }
    }
}