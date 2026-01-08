using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class PaymentStatus
    {
        public int StatusId { get; set; }

        [Required(ErrorMessage = "El nombre del estado es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Estado de Pago")]
        public string StatusName { get; set; } = string.Empty;
    }
}
