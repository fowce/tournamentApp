using Microsoft.Data.SqlClient;
using tournamentApp.Models.Entities;

namespace tournamentApp.Data.Repository
{
	public class CreateTournament
	{
		private readonly string _connectionString;

		public CreateTournament(string connection)
		{
			_connectionString = connection;	
		}

		public void AddTournament(CreateTournamentViewModel viewModel, int? userId)
		{

			if (TournamentExists(viewModel.NameTournament))
			{
				throw new InvalidOperationException("Имя для турнира занято");
			}
			else if (viewModel.SelectFormat == "")
			{
				throw new InvalidOperationException("Выберите формат!");
			}
            else if (viewModel.dateEvent < DateTime.Now)
            {
                throw new InvalidOperationException("Нельзя создавать турнир в прошлом.");
            }

            string query = "INSERT INTO Tournaments " +
						   "(Name, Format, Status, Game_Id, Date_Event, CreatorId, MaxNumberPlayers) " +
						   "VALUES (@Name, @Format, @Status, @Game_Id, @Date_Event, @CreatorId, @MaxNumberPlayers)";

            string status = "Предстоящий";

            int gameId = GetGameId(viewModel.SelectGame);
			int maxNumberPlayers = GetMaxNumberPlayers(viewModel.SelectFormat);

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@Name", viewModel.NameTournament);
					command.Parameters.AddWithValue("@Format", viewModel.SelectFormat);
					command.Parameters.AddWithValue("@Status", status);
					command.Parameters.AddWithValue("@Game_Id", gameId);
					command.Parameters.AddWithValue("@Date_Event", viewModel.dateEvent);
					command.Parameters.AddWithValue("@CreatorId", userId);
					command.Parameters.AddWithValue("@MaxNumberPlayers", maxNumberPlayers);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}

			int tournamentId = GetLastInsertedTournamentId();
            AddParticipant(tournamentId, userId.Value, "L");
			AddMatch(tournamentId);

        }

		public void AddMatch(int tournamentId)
		{
			string queryToAddMatch = @"
				   INSERT INTO Matches
				   (TournamentId, WinningSide, IsDraw)
				   VALUES (@TournamentId, @WinningSide, @IsDraw)";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(queryToAddMatch, connection))
				{
					command.Parameters.AddWithValue("@TournamentId", tournamentId);
					command.Parameters.AddWithValue("@WinningSide", DBNull.Value);
					command.Parameters.AddWithValue("@IsDraw", false);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}

		public int GetLastInsertedTournamentId()
        {
            string query = "SELECT TOP 1 Id FROM Tournaments ORDER BY Id DESC";
            using (var connection = new SqlConnection(_connectionString))
			{
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    return (int)command.ExecuteScalar();
                }
            }
        }

        public void AddParticipant(int tournamentId, int userId, string side)
        {
            string query = "INSERT INTO TournamentParticipants (TournamentId, UserId, Side) " +
						   "VALUES (@TournamentId, @UserId, @Side)";
            using (var connection = new SqlConnection(_connectionString))
			{
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TournamentId", tournamentId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Side", side);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

		public void AddUserToSide(int tournamentId, int userId, string side)
		{
            string statusCheck = @"
						SELECT Status 
						FROM Tournaments 
						WHERE Id = @TournamentId";

            string checkQuery = @"
						SELECT COUNT(*) 
						FROM TournamentParticipants 
						WHERE TournamentId = @TournamentId AND UserId = @UserId";

            string checkQueryFull = @"
						SELECT COUNT(tp.UserId)
                        FROM TournamentParticipants tp
                        WHERE tp.TournamentId = @TournamentId";

            string sideCountQuery = @"
						SELECT COUNT(*) 
						FROM TournamentParticipants 
						WHERE TournamentId = @TournamentId AND Side = @Side";

            using (var connection = new SqlConnection(_connectionString))
			{
				connection.Open();
                
                using (var cmd = new SqlCommand(statusCheck, connection))
                {
                    cmd.Parameters.AddWithValue("@TournamentId", tournamentId);
                    var status = (string)cmd.ExecuteScalar();

                    if (status == "Активный" || status == "Завершенный")
                        throw new InvalidOperationException("Нельзя войти из-за статуса.");
                }

                using (var cmd = new SqlCommand(checkQueryFull, connection))
                {
                    cmd.Parameters.AddWithValue("@TournamentId", tournamentId);
                    int checkFull = (int)cmd.ExecuteScalar();
					int maxNumPlayers = MaxNumberPlayers(tournamentId);
					
                    if (checkFull == maxNumPlayers)
                        throw new InvalidOperationException("Турнир заполнен.");
                }

                using (var checkCommand = new SqlCommand(checkQuery, connection))
				{
					checkCommand.Parameters.AddWithValue("@TournamentId", tournamentId);
					checkCommand.Parameters.AddWithValue("@UserId", userId);

					int exists = (int)checkCommand.ExecuteScalar();
					if (exists > 0)
						throw new InvalidOperationException("Пользователь уже участвует в этом турнире.");
				}

                using (var countCmd = new SqlCommand(sideCountQuery, connection))
                {
                    countCmd.Parameters.AddWithValue("@TournamentId", tournamentId);
                    countCmd.Parameters.AddWithValue("@Side", side);
                    int currentCount = (int)countCmd.ExecuteScalar();
					int sideLimit = MaxNumberPlayers(tournamentId) / 2;

                    if (currentCount >= sideLimit)
                        throw new InvalidOperationException("Эта сторона заполнена.");
                }

                string insertQuery = @"
							INSERT INTO TournamentParticipants (TournamentId, UserId, Side) 
							VALUES (@TournamentId, @UserId, @Side)";

				using (var command = new SqlCommand(insertQuery, connection))
				{
					command.Parameters.AddWithValue("@TournamentId", tournamentId);
					command.Parameters.AddWithValue("@UserId", userId);
					command.Parameters.AddWithValue("@Side", side);

					command.ExecuteNonQuery();
				}
			}
		}

		public void RemoveParticipant(int tournamentId, int userId)
		{
			string statusCheck = @"
						SELECT Status 
						FROM Tournaments 
						WHERE Id = @TournamentId";

			string checkQuery = @"
						SELECT COUNT(*) 
						FROM TournamentParticipants 
						WHERE TournamentId = @TournamentId AND UserId = @UserId";

			using (var connection = new SqlConnection(_connectionString))
			{
				connection.Open();

				using (var cmd = new SqlCommand(statusCheck, connection))
				{
					cmd.Parameters.AddWithValue("@TournamentId", tournamentId);
					var status = (string)cmd.ExecuteScalar();

					if (status == "Активный" || status == "Завершенный")
						throw new InvalidOperationException("Нельзя выйти из турнира с текущим статусом.");
				}

				using (var checkCommand = new SqlCommand(checkQuery, connection))
				{
					checkCommand.Parameters.AddWithValue("@TournamentId", tournamentId);
					checkCommand.Parameters.AddWithValue("@UserId", userId);

					int exists = (int)checkCommand.ExecuteScalar();
					if (exists == 0)
						throw new InvalidOperationException("Пользователь не участвует в этом турнире.");
				}

				string deleteQuery = @"
							DELETE FROM TournamentParticipants 
							WHERE TournamentId = @TournamentId AND UserId = @UserId";

				using (var command = new SqlCommand(deleteQuery, connection))
				{
					command.Parameters.AddWithValue("@TournamentId", tournamentId);
					command.Parameters.AddWithValue("@UserId", userId);
					command.ExecuteNonQuery();
				}
			}
		}

		public int MaxNumberPlayers(int tournamentId)
		{
            string getMaxPlayers = @"
						SELECT MaxNumberPlayers
						FROM Tournaments
						WHERE Id = @TournamentId";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(getMaxPlayers, connection))
                {
                    command.Parameters.AddWithValue("@TournamentId", tournamentId);
                    return (int)command.ExecuteScalar();
                }
            }
        }

		public bool TournamentExists(string tournamentName)
		{
			string query = "SELECT COUNT(*) FROM Tournaments " +
						   "WHERE Name = @Name";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@Name", tournamentName);
					connection.Open();
					int count = (int)command.ExecuteScalar();
					return count > 0;
				}
			}
		}

		public int GetGameId(string gameName)
		{
			string query = "SELECT Id FROM Games " +
						   "WHERE Name = @Name";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@Name", gameName);
					connection.Open();
					object result = command.ExecuteScalar();
					if (result == null || result == DBNull.Value)
					{
						throw new InvalidOperationException("Игры не найдено");
					}
					return Convert.ToInt32(result);
				}
			}
		}

		public int GetMaxNumberPlayers(string format)
		{
			if (format == "1 x 1") { return 2; }
			else if (format == "3 x 3") { return 6; }
			else { return 10; }
		}

		public string GetUserName(int userId)
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

					return result.ToString();
				}
			}
		}
	}
}
