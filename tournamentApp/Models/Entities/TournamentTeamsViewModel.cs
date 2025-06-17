namespace tournamentApp.Models.Entities
{
    public class TournamentTeamsViewModel
    {
        public int TournamentId { get; set; }

        public int MaxPlayers { get; set; }

        public List<ParticipantViewModel> Participants { get; set; } = [];

        public string Status { get; set; }
    }
}
