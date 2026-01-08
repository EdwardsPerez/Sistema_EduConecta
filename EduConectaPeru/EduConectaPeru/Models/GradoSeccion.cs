using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class GradoSeccion
    {
        public int GradoSeccionId { get; set; }

        [Required(ErrorMessage = "El grado es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Grado")]
        public string Grado { get; set; } = string.Empty;

        [Required(ErrorMessage = "La sección es obligatoria")]
        [StringLength(10)]
        [Display(Name = "Sección")]
        public string Seccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El año escolar es obligatorio")]
        [Display(Name = "Año Escolar")]
        public int AnioEscolar { get; set; }

        [Required(ErrorMessage = "La capacidad es obligatoria")]
        [Range(1, 50, ErrorMessage = "La capacidad debe estar entre 1 y 50")]
        [Display(Name = "Capacidad")]
        public int Capacidad { get; set; } = 30;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Grado - Sección")]
        public string GradoSeccionNombre => $"{Grado} - {Seccion}";
    }
}
