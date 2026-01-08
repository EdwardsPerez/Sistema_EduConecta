using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class TransaccionPago
    {
        public int TransaccionId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un carrito")]
        [Display(Name = "Carrito")]
        public int CarritoId { get; set; }

        [Required(ErrorMessage = "El número de tarjeta es obligatorio")]
        [StringLength(4, MinimumLength = 4)]
        [Display(Name = "Últimos 4 dígitos")]
        public string NumeroTarjeta { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Tipo de Tarjeta")]
        public string? TipoTarjeta { get; set; }

        [Required(ErrorMessage = "El monto total es obligatorio")]
        [Range(0.01, 9999999.99)]
        [Display(Name = "Monto Total")]
        public decimal MontoTotal { get; set; }

        [Display(Name = "Fecha de Transacción")]
        public DateTime FechaTransaccion { get; set; } = DateTime.Now;

        [StringLength(50)]
        [Display(Name = "Código de Autorización")]
        public string? CodigoAutorizacion { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Aprobado";

        [StringLength(255)]
        [Display(Name = "Mensaje de Respuesta")]
        public string? MensajeRespuesta { get; set; }

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

    
        public CarritoCompras? Carrito { get; set; }
        public PaymentType? PaymentType { get; set; }
        public Bank? Bank { get; set; }
        public int? PaymentTypeId { get; set; }
        public int? BankId { get; set; }
    }
}
