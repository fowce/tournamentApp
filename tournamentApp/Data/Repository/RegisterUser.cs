using Microsoft.Data.SqlClient;
using tournamentApp.Models.Entities;

namespace tournamentApp.Data.Repository
{
	public class RegisterUser
	{
        private readonly string _connectionString;

        private readonly PasswordHasherService _passwordHasher;

        public RegisterUser(string connection, PasswordHasherService passwordHasher)
        {
            _connectionString = connection;
            _passwordHasher = passwordHasher;
        }

        public int AddUser(RegisterViewModel viewModel)
        {
            if (CheckUser(viewModel))
            {
                throw new InvalidOperationException("Имя или Email уже заняты");
            }

            string hashedPassword = _passwordHasher.HashPassword(viewModel.Password);

            string query = "INSERT INTO Users " +
                           "(Name, Email, Password, Role) " +
                           "VALUES (@Name, @Email, @Password, @Role) " +
						   "SELECT SCOPE_IDENTITY()";

            string role = viewModel.Name == "admin" ? "admin" : "user";

            using (var connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", viewModel.Name);
                    command.Parameters.AddWithValue("@Email", viewModel.Email);
                    command.Parameters.AddWithValue("@Password", hashedPassword);
                    command.Parameters.AddWithValue("@Role", role);

                    connection.Open();
					return Convert.ToInt32(command.ExecuteScalar());
				}
            }

        }

        public void AddRatingUser(int userId)
        {
			string insertRatingQuery = @"
                    INSERT INTO Ratings (UserId)
                    VALUES (@UserId)";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (SqlCommand command = new SqlCommand(insertRatingQuery, connection))
				{
					command.Parameters.AddWithValue("@UserId", userId);

					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}

        public bool CheckUser(RegisterViewModel viewModel) 
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
    }
}
