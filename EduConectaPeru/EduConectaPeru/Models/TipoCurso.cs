using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class TipoCurso
    {
        public int TipoCursoId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }
    }
}
