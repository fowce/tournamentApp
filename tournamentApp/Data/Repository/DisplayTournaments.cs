using tournamentApp.Models.Entities;
using Microsoft.Data.SqlClient;

namespace tournamentApp.Data.Repository
{
	public class DisplayTournaments
	{
		private readonly string _connectionString;

		public DisplayTournaments(string connection)
		{
			_connectionString = connection;
		}

		public List<Tournament> GetAllTournaments(TournamentFilter? filter = null)
		{
            UpdateTournamentStatuses();

            var tournaments = new List<Tournament>();

            string query = @"
						SELECT t.[Name], t.[Format], t.CreatorId, t.Id, t.MaxNumberPlayers, g.[Name] as GameName 
						FROM Tournaments t
						JOIN Games g ON t.Game_Id = g.Id";

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.GameName))
                    query += " AND g.[Name] = @GameName";

                if (!string.IsNullOrEmpty(filter.Format))
                    query += " AND t.[Format] = @Format";

                if (!string.IsNullOrEmpty(filter.SearchName))
                    query += " AND t.[Name] LIKE @SearchName";
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
			{
				using (SqlCommand command = new SqlCommand(query, connection))
				{
                    if (filter != null)
                    {
                        if (!string.IsNullOrEmpty(filter.GameName))
                            command.Parameters.AddWithValue("@GameName", filter.GameName);

                        if (!string.IsNullOrEmpty(filter.Format))
                            command.Parameters.AddWithValue("@Format", filter.Format);

                        if (!string.IsNullOrEmpty(filter.SearchName))
                            command.Parameters.AddWithValue("@SearchName", $"%{filter.SearchName}%");
                    }

                    connection.Open();
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							tournaments.Add(new Tournament
							{
								Name = reader.GetString(0),
								Format = reader.GetString(1),
								Id = reader.GetInt32(3),
								CreatorName = GetUserName(reader.GetInt32(2)),
								CurrentPlayers = GetCurrentNumPlayers(reader.GetInt32(3)),
								MaxPlayers = reader.GetInt32(4),
                                GameName = reader.GetString(5)
                            });
						}
					}
				}
			}

			return tournaments;
		}

        public List<string> GetAvailableGames()
        {
            var games = new List<string>();

            string query = "SELECT [Name] FROM Games";

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            games.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return games;
        }

        public List<string> GetAvailableFormats()
        {
            return new List<string> { "1 x 1", "3 x 3", "5 x 5" };
        }

        public List<Tournament> GetTournamentsByCreator(int creatorId)
		{
			UpdateTournamentStatuses();

			var tournaments = new List<Tournament>();

            string query = @"
				 SELECT t.[Name], t.[Format], t.CreatorId, t.Id, t.MaxNumberPlayers, g.[Name] as GameName 
                 FROM Tournaments t
                 JOIN Games g ON t.Game_Id = g.Id
                 WHERE t.CreatorId = @creatorId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
			{
				using (SqlCommand command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@creatorId", creatorId);
					connection.Open();
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							tournaments.Add(new Tournament
							{
								Name = reader.GetString(0),
								Format = reader.GetString(1),
								Id = reader.GetInt32(3),
								CreatorName = GetUserName(reader.GetInt32(2)),
								CurrentPlayers = GetCurrentNumPlayers(reader.GetInt32(3)),
								MaxPlayers = reader.GetInt32(4),
                                GameName = reader.GetString(5)
                            });
						}
					}
				}
			}

			return tournaments;
		}

		public string? GetUserName(int userId)
		{
			string query = "SELECT [Name] " +
						   "FROM Users " +
						   "WHERE Id = @userId";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@userId", userId);
					connection.Open();
					object result = command.ExecuteScalar();

					return result?.ToString();
				}
			}
		}

		public int GetCurrentNumPlayers(int tournamentsId)
		{
			string query = "SELECT COUNT(tp.UserId) " +
                           "FROM TournamentParticipants tp " +
                           "WHERE tp.TournamentId = @TournamentId";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TournamentId", tournamentsId);
					connection.Open();
					object result = command.ExecuteScalar();

					return (int)result;
				}
			}
		}

		public string GetStatusTournament(int tournamentId)
		{
			string getStatus = @"SELECT [Status]
								 FROM Tournaments
								 WHERE Id = @tournamentId";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(getStatus, connection))
				{
					command.Parameters.AddWithValue("@tournamentId", tournamentId);
					connection.Open();
					object result = command.ExecuteScalar();

					return result.ToString();
				}
			}
		}

        public void UpdateTournamentStatuses()
        {
			string updateStatus = @"
					UPDATE Tournaments
					SET Status = 
					CASE 
						WHEN GETDATE() BETWEEN Date_Event AND DATEADD(MINUTE, 1, Date_Event) THEN
						CASE
							WHEN (SELECT COUNT(*) 
								  FROM TournamentParticipants 
								  WHERE TournamentId = Tournaments.Id) >= MaxNumberPlayers
							THEN 'Активный'
							ELSE 'Завершенный'
						END

						WHEN GETDATE() > DATEADD(MINUTE, 1, Date_Event)
						AND Status = 'Активный' 
						THEN 'Завершенный'

						ELSE Status
					END
					WHERE Status IN ('Предстоящий', 'Активный')";

			using (var connection = new SqlConnection(_connectionString))
			{
                connection.Open();
				using (var command = new SqlCommand(updateStatus, connection))
				{
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
