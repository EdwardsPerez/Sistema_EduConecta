using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class CarritoCompras
    {
        public int CarritoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un apoderado")]
        [Display(Name = "Apoderado")]
        public int LegalGuardianId { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Pendiente";

        [Display(Name = "Monto Total")]
        public decimal MontoTotal { get; set; } = 0;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        public LegalGuardian? LegalGuardian { get; set; }
        public List<DetalleCarrito>? DetallesCarrito { get; set; }
    }
}
