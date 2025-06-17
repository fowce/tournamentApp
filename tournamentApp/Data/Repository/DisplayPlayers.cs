using tournamentApp.Models.Entities;
using Microsoft.Data.SqlClient;

namespace tournamentApp.Data.Repository
{
	public class DisplayPlayers
	{
		private readonly string _connectionString;

		public DisplayPlayers(string connection)
		{
			_connectionString = connection;
		}

        public TournamentTeamsViewModel GetTournamentParticipants(int tournamentId)
        {
            var participants = new List<ParticipantViewModel>();
            int maxPlayers = 0;
            string status = string.Empty;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string queryMaxNumPlayers = 
                               "SELECT MaxNumberPlayers " +
                               "FROM Tournaments " +
                               "WHERE Id = @TournamentId";
                // 1. Get max players from Tournaments table
                using (var command = new SqlCommand(queryMaxNumPlayers, connection))
                {
                    command.Parameters.AddWithValue("@TournamentId", tournamentId);
                    var result = command.ExecuteScalar();
                    if (result != null)
                        maxPlayers = Convert.ToInt32(result);
                }

                string queryStatus =
                               "SELECT Status " +
                               "FROM Tournaments " +
                               "WHERE Id = @TournamentId";
                // 2. Get status from Tournaments table
                using (var command = new SqlCommand(queryStatus, connection))
                {
                    command.Parameters.AddWithValue("@TournamentId", tournamentId);
                    var result = command.ExecuteScalar();
                    if (result != null)
                        status = result.ToString();
                }

                // 3. Get players and their sides
                string query = @"
                    SELECT u.Name, tp.Side
                    FROM TournamentParticipants tp
                    JOIN Users u ON tp.UserId = u.Id
                    WHERE tp.TournamentId = @TournamentId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TournamentId", tournamentId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            participants.Add(new ParticipantViewModel
                            {
                                UserName = reader.GetString(0),
                                Side = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            return new TournamentTeamsViewModel
            {
                TournamentId = tournamentId,
                MaxPlayers = maxPlayers,
                Participants = participants,
                Status = status
            };
        }
    }
}
