using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class CursoVacacional
    {
        public int CursoVacacionalId { get; set; }

        [Required(ErrorMessage = "El nombre del curso es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre del Curso")]
        public string NombreCurso { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Inicio")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Fin")]
        public DateTime FechaFin { get; set; }

        [Required(ErrorMessage = "El costo es obligatorio")]
        [Range(0.01, 9999999.99, ErrorMessage = "El costo debe ser mayor a 0")]
        [Display(Name = "Costo")]
        public decimal Costo { get; set; }

        [Required(ErrorMessage = "La capacidad máxima es obligatoria")]
        [Range(1, 100, ErrorMessage = "La capacidad debe estar entre 1 y 100")]
        [Display(Name = "Capacidad Máxima")]
        public int CapacidadMaxima { get; set; }

        [Display(Name = "Cupos Disponibles")]
        public int CuposDisponibles { get; set; }

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
