using System.ComponentModel.DataAnnotations;

namespace EduConectaPeru.Models
{
    public class Bank
    {
        public int BankId { get; set; }

        [Required(ErrorMessage = "El nombre del banco es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre del Banco")]
        public string BankName { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;
    }
}
