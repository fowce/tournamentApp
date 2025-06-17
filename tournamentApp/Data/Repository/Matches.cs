using tournamentApp.Models.Entities;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Transactions;
using System.Data.Common;

namespace tournamentApp.Data.Repository
{
	public class Matches
	{
		private readonly string _connectionString;

		public Matches(string connection)
		{
			_connectionString = connection;	
		}

		public void UpdateMatch(MatchViewModel model, string status)
		{
			if (status == "Предстоящий" || status == "Активный")
			{
				throw new InvalidOperationException("Вы не можете записывать/обновлять результат при статусе Предстоящий или Активный");
			}

			if (MatchResultsExist(model.TournamentId))
			{
				throw new InvalidOperationException("Результаты уже были сохранены ранее. Повторное сохранение невозможно.");
			}

			if (model.ScoreLeftSide == model.ScoreRightSide)
			{
				model.IsDraw = true;
				model.WinningSide = null;
			}
			else
			{
				model.IsDraw = false;
				model.WinningSide = model.ScoreLeftSide > model.ScoreRightSide ? "L" : "R";
			}

			string queryToAddMatch = @"
				   UPDATE Matches 
				   SET 
					  WinningSide = @WinningSide,
					  ScoreSideL = @ScoreSideL,
					  ScoreSideR = @ScoreSideR,
					  IsDraw = @IsDraw
				   WHERE TournamentId = @TournamentId";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(queryToAddMatch, connection))
				{
					command.Parameters.AddWithValue("@TournamentId", model.TournamentId);
					command.Parameters.AddWithValue("@WinningSide", (object)model.WinningSide ?? DBNull.Value);
					command.Parameters.AddWithValue("@ScoreSideL", model.ScoreLeftSide);
					command.Parameters.AddWithValue("@ScoreSideR", model.ScoreRightSide);
					command.Parameters.AddWithValue("@IsDraw", model.IsDraw);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}

			if (!model.IsDraw)
			{
				UpdateRatingsForWinningTeam(model.TournamentId, model.WinningSide);
			}
		}

		private void UpdateRatingsForWinningTeam(int tournamentId, string winningSide)
		{
			string getWinnersQuery = @"
					SELECT UserId 
					FROM TournamentParticipants 
					WHERE TournamentId = @TournamentId AND Side = @WinningSide";

			var winnerIds = new List<int>();

			using (var connection = new SqlConnection(_connectionString))
			{
				connection.Open();
				using (var command = new SqlCommand(getWinnersQuery, connection))
				{
					command.Parameters.AddWithValue("@TournamentId", tournamentId);
					command.Parameters.AddWithValue("@WinningSide", winningSide);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							winnerIds.Add(reader.GetInt32(0));
						}
					}
				}
			}

			if (winnerIds.Any())
			{
				string updateRatingQuery = @"
						UPDATE Ratings 
						SET Rating = Rating + 1 
						WHERE UserId = @UserId";

				foreach (var userId in winnerIds)
				{
					using (var connection = new SqlConnection(_connectionString))
					{
						using (var updateCommand = new SqlCommand(updateRatingQuery, connection))
						{
							connection.Open();
							updateCommand.Parameters.AddWithValue("@UserId", userId);
							updateCommand.ExecuteNonQuery();
						}
					}
				}
			}
		}

		private bool MatchResultsExist(int tournamentId)
		{
			string query = @"
					SELECT COUNT(*) 
					FROM Matches 
					WHERE TournamentId = @TournamentId 
					AND (ScoreSideL > 0 OR ScoreSideR > 0)";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@TournamentId", tournamentId);
					connection.Open();
					int count = (int)command.ExecuteScalar();
					return count > 0;
				}
			}
		}

		public Match GetMatch(int tournamentId)
		{
			string queryToGetMatch = @"
                            SELECT ScoreSideL, ScoreSideR
                            FROM Matches
                            WHERE TournamentId = @TournamentId";

			var match = new Match();

			using (SqlConnection connection = new SqlConnection(_connectionString))
			{
				connection.Open();
				using (SqlCommand command = new SqlCommand(queryToGetMatch, connection))
				{
					command.Parameters.AddWithValue("@TournamentId", tournamentId);

					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							match.ScoreSideL = reader.GetInt32(0);
							match.ScoreSideR = reader.GetInt32(1);
						}
					}
				}
			}
			return match;
		}
	}
}
