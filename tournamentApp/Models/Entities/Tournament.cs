namespace tournamentApp.Models.Entities
{
    public class TournamentFilter
    {
        public string? GameName { get; set; }

        public string? Format { get; set; }

        public string? SearchName { get; set; }
    }

    public class Tournament
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public string? CreatorName { get; set; } = string.Empty;

		public string Format { get; set; } = string.Empty;

		public int CurrentPlayers { get; set; }

		public int MaxPlayers { get; set; }

        public string GameName { get; set; } = string.Empty;
    }
}
