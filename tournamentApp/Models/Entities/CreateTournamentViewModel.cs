using System.ComponentModel.DataAnnotations;

namespace tournamentApp.Models.Entities
{
	public class CreateTournamentViewModel
	{
		[Required(ErrorMessage = "Данное поле обязательно")]
		[StringLength(25, MinimumLength = 2, ErrorMessage = "Название турнира от 2 до 25 символов")]
		[RegularExpression(@"^[a-zA-Zа-яА-ЯёЁ0-9\s]+$", ErrorMessage = "Допустимы только буквы и цифры")]
		public string NameTournament { get; set; } = string.Empty;

		public string SelectGame { get; set; } = string.Empty;

		public string SelectFormat { get; set; } = string.Empty;

		public DateTime dateEvent { get; set; }
    }
}
