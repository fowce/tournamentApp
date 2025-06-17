using System.ComponentModel.DataAnnotations;

namespace tournamentApp.Models.Entities
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Данное поле обязательно")]
        public string EmailOrName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Данное поле обязательно")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
