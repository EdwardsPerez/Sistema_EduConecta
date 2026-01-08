using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class PaymentType
    {
        public int PaymentTypeId { get; set; }

        [Required(ErrorMessage = "El nombre del tipo es requerido")]
        [StringLength(50)]
        [Display(Name = "Tipo de Pago")]
        public string TypeName { get; set; }

        [StringLength(255)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Payment> Payments { get; set; }
    }
}
