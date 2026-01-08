using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class AsignacionDocente
    {
        public int AsignacionId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un docente")]
        [Display(Name = "Docente")]
        public int DocenteId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un horario")]
        [Display(Name = "Horario")]
        public int HorarioId { get; set; }

        [Display(Name = "Fecha de Asignación")]
        public DateTime FechaAsignacion { get; set; } = DateTime.Now;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        public Docente? Docente { get; set; }
        public Horario? Horario { get; set; }
    }
}
