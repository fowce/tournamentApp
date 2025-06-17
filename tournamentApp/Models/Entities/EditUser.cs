using System.ComponentModel.DataAnnotations;

namespace tournamentApp.Models.Entities
{
	public class EditUser
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Данное поле обязательно")]
		[StringLength(20, MinimumLength = 2, ErrorMessage = "Имя содержит от 2 до 20 символов")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Данное поле обязательно")]
		[EmailAddress(ErrorMessage = "Неккоректный формат email")]
		[RegularExpression(@"^([a-zA-Z0-9\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$",
ErrorMessage = "Неккоректный формат email")]
		[StringLength(100)]
		public string Email { get; set; } = string.Empty;
	}
}
