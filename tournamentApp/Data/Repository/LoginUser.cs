using Microsoft.Data.SqlClient;
using tournamentApp.Models.Entities;

namespace tournamentApp.Data.Repository
{
    public class LoginUser
    {
        private readonly string _connectionString;
        private readonly PasswordHasherService _passwordHasher;

        public LoginUser(string connection, PasswordHasherService passwordHasher)
        {
            _connectionString = connection;
            _passwordHasher = passwordHasher;
        }

        public string GetUserHashedPassword(LoginViewModel viewModel)
        {
            string query = "SELECT Password FROM Users " +
                           "WHERE Email = @Email OR Name = @Name";

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", viewModel.EmailOrName);
                    command.Parameters.AddWithValue("@Name", viewModel.EmailOrName);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    connection.Close();

                    return result as string;
                }
            }
        }

        public bool ValidateUser(LoginViewModel viewModel)
        {
            string? hashedPassword = GetUserHashedPassword(viewModel);

            if (hashedPassword == null)
            {
                return false;
            }

            return _passwordHasher.VerifyPassword(hashedPassword, viewModel.Password);
        }

		public int GetUserId(string emailOrName)
		{
			string query = "SELECT Id " +
                           "FROM Users " +
                           "WHERE Name = @EmailOrName OR Email = @EmailOrName";

			using (var connection = new SqlConnection(_connectionString))
			{
				using (var command = new SqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@EmailOrName", emailOrName);
					connection.Open();
					return (int)command.ExecuteScalar();
				}
			}
		}

        public string GetUserRole(string emailOrName)
        {
            string query = "SELECT Role " +
                           "FROM Users " +
                           "WHERE Name = @EmailOrName OR Email = @EmailOrName";

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmailOrName", emailOrName);
                    connection.Open();
                    return command.ExecuteScalar().ToString();
                }
            }
        }

        public string GetNameUser(int? userId)
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
					return command.ExecuteScalar().ToString();
				}
			}
		}

	}
}
