using tournamentApp.Models.Entities;
using Microsoft.Data.SqlClient;

namespace tournamentApp.Data.Repository
{
	public class Profile
	{
		private readonly string _connectionString;

        public Profile(string connection)
        {
            _connectionString = connection;
        }

        public ProfileUser GetProfileUser(int userId)
        {
            string query = @"
                            SELECT [Name], Email, Rating
							FROM Users ur
							JOIN Ratings rt ON ur.Id = rt.UserId
							WHERE rt.UserId = @userId";

			var profileUser = new ProfileUser();

			using (SqlConnection connection = new SqlConnection(_connectionString))
			{
				connection.Open();
				using (SqlCommand command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@userId", userId);
					
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							profileUser.Name = reader.GetString(0);
							profileUser.Email = reader.GetString(1);
							profileUser.Rating = reader.GetInt32(2);
						}
					}
				}
			}
			return profileUser;
		}

		public List<User> GetUsers()
		{
            var user = new List<User>();

            string query = @"
				 SELECT Id, [Name], Email, Role 
                 FROM Users
                 WHERE Role = 'user'";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user.Add(new User
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2),
                                Role = reader.GetString(3)
                            });
                        }
                    }
                }
            }

            return user;
        }

        public void DeleteUser(int userId)
        {
            var tournamentID = GetTournamentId(userId);

            if (CheckUserIsTournamentCreator(userId))
            {
                for (int i = 0; i < tournamentID.Count; i++)
                {
                    if (GetCountTournamentParticipants(tournamentID[i]) > 1)
                    {
                        string queryUpdateCreatorId = @"
                                    UPDATE Tournaments 
                                    SET CreatorId = @creatorId
                                    WHERE Id = @tournamentId";

                        using (SqlConnection connection = new SqlConnection(_connectionString))
                        {
                            connection.Open();
                            using (SqlCommand command = new SqlCommand(queryUpdateCreatorId, connection))
                            {
                                command.Parameters.AddWithValue("@tournamentId", tournamentID[i]);

                                command.Parameters.AddWithValue("@creatorId", GetParticipantsIds(
                                                                              tournamentID[i])
                                                                              .Where(ur => ur != userId)
                                                                              .First());

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        string queryDeleteTournament = @"
                                DELETE FROM Matches
                                WHERE TournamentId = @tournamentId
                                
                                DELETE FROM TournamentParticipants
                                WHERE TournamentId = @tournamentId;

                                DELETE FROM Tournaments
                                WHERE Id = @tournamentId";

                        using (SqlConnection connection = new SqlConnection(_connectionString))
                        {
                            connection.Open();
                            using (SqlCommand command = new SqlCommand(queryDeleteTournament, connection))
                            {
                                command.Parameters.AddWithValue("@tournamentId", tournamentID[i]);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }

                DeleteUserFromSystem(userId);
            }
            else
            {
                DeleteUserFromSystem(userId);
            }
        }

        public void DeleteUserFromSystem(int userId)
        {
            string queryDeleteUser = @"
				 DELETE FROM Ratings
                 WHERE UserId = @userId;

                 DELETE FROM TournamentParticipants
                 WHERE UserId = @userId;

                 DELETE FROM Users
                 WHERE Id = @userId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(queryDeleteUser, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@userId", userId);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void EditUser(EditUser user)
        {
			if (!string.IsNullOrEmpty(user.Name))
			{
				string checkNameQuery = @"
                        SELECT COUNT(*) 
                        FROM Users 
                        WHERE [Name] = @Name AND Id != @Id";
				using (var connection = new SqlConnection(_connectionString))
                {
					using (var command = new SqlCommand(checkNameQuery, connection))
					{
						command.Parameters.AddWithValue("@Name", user.Name);
						command.Parameters.AddWithValue("@Id", user.Id);
						connection.Open();
						int nameCount = (int)command.ExecuteScalar();
						if (nameCount > 0)
						{
							throw new InvalidOperationException("Имя пользователя уже занято");
						}
					}
				}
				
			}

			if (!string.IsNullOrEmpty(user.Email))
			{
				string checkEmailQuery = @"
                        SELECT COUNT(*) 
                        FROM Users 
                        WHERE Email = @Email AND Id != @Id";

				using (var connection = new SqlConnection(_connectionString))
                {
					using (var command = new SqlCommand(checkEmailQuery, connection))
					{
						command.Parameters.AddWithValue("@Email", user.Email);
						command.Parameters.AddWithValue("@Id", user.Id);
						connection.Open();
						int emailCount = (int)command.ExecuteScalar();
						if (emailCount > 0)
						{
							throw new InvalidOperationException("Email уже используется");
						}
					}
				}
				
			}

			string updateQuery = @"
                    UPDATE Users 
                    SET 
                        [Name] = CASE WHEN @Name IS NOT NULL THEN @Name ELSE [Name] END,
                        Email = CASE WHEN @Email IS NOT NULL THEN @Email ELSE Email END
                    WHERE Id = @Id";

			using (var connection = new SqlConnection(_connectionString))
            {
				using (var command = new SqlCommand(updateQuery, connection))
				{
					command.Parameters.AddWithValue("@Id", user.Id);
					command.Parameters.AddWithValue("@Name", string.IsNullOrEmpty(user.Name) ? DBNull.Value : user.Name);
					command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(user.Email) ? DBNull.Value : user.Email);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}
			
		}

		public bool CheckUser(EditUser viewModel)
		{
			string query = "SELECT COUNT(*) FROM Users " +
						   "WHERE Email = @Email or Name = @Name";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (SqlCommand command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@Name", viewModel.Name);
					command.Parameters.AddWithValue("@Email", viewModel.Email);

					connection.Open();
					int count = (int)command.ExecuteScalar();
					return count > 0;
				}
			}
		}

		public bool CheckUserIsTournamentCreator(int userId)
        {
            string queryCheckCreator = @"
                 SELECT COUNT(*) FROM Tournaments
                 WHERE CreatorId = @userId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(queryCheckCreator, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@userId", userId);

                    int userIsTournamentCreator = (int)command.ExecuteScalar();
                    return userIsTournamentCreator > 0;
                }
            }
        }

        public int GetCountTournamentParticipants(int tournamentId)
        {
            string queryGetCountParticipants = @"
					SELECT COUNT(*) 
					FROM TournamentParticipants 
					WHERE TournamentId = @TournamentId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(queryGetCountParticipants, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@TournamentId", tournamentId);

                    int CountParticipants = (int)command.ExecuteScalar();
                    return CountParticipants;
                }
            }
        }

        public List<int> GetTournamentId(int userId)
        {
            var tournamentIds = new List<int>();

            string queryGetTournamentID = @"
                 SELECT Id
                 FROM Tournaments
                 WHERE CreatorId = @userId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(queryGetTournamentID, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@userId", userId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tournamentIds.Add(reader.GetInt32(0));
                        }
                    }
                }
            }

            return tournamentIds;
        }

        public List<int> GetParticipantsIds(int tournamentId)
        {
            var participantsIds = new List<int>();

            string queryGetParticipantsIds = @"
					SELECT UserId 
					FROM TournamentParticipants 
					WHERE TournamentId = @TournamentId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(queryGetParticipantsIds, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@TournamentId", tournamentId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            participantsIds.Add(reader.GetInt32(0));
                        }
                    }
                }
            }

            return participantsIds;
        }
    }
}
