namespace tournamentApp.Models.Entities
{
	public class MatchViewModel
	{
		public int TournamentId { get; set; }

		public int ScoreLeftSide { get; set; }

		public int ScoreRightSide { get; set; }

		public string? WinningSide { get; set; }

		public bool IsDraw { get; set; }
	}
}
