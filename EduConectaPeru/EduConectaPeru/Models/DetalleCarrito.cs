using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class DetalleCarrito
    {
        public int DetalleId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un carrito")]
        [Display(Name = "Carrito")]
        public int CarritoId { get; set; }

        [Display(Name = "Cuota")]
        public int? QuotaId { get; set; }

        [Display(Name = "Cuota Vacacional")]
        public int? QuotaVacacionalId { get; set; }

        [Required(ErrorMessage = "El concepto es obligatorio")]
        [StringLength(255)]
        [Display(Name = "Concepto")]
        public string Concepto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, 9999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

     
        public CarritoCompras? Carrito { get; set; }
        public Quota? Quota { get; set; }
        public QuotaCursoVacacional? QuotaVacacional { get; set; }
    }
}
