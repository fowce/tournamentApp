using tournamentApp.Models.Entities;
using Microsoft.Data.SqlClient;

namespace tournamentApp.Data.Repository
{
	public class DisplayRatingPlayers
	{
		private readonly string _connectionString;

		public DisplayRatingPlayers(string connection)
		{
			_connectionString = connection;
		}

		public List<PlayerRatingViewModel> GetPlayerRatings()
		{
			string query = @"
					SELECT [Name], Rating
					FROM Users ur
					JOIN Ratings rt ON ur.Id = rt.UserId
					ORDER BY rt.Rating DESC";

			var ratings = new List<PlayerRatingViewModel>();

			using (var connection = new SqlConnection(_connectionString))
			{
				connection.Open();
				using (var command = new SqlCommand(query, connection))
				{
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							ratings.Add(new PlayerRatingViewModel
							{
								Name = reader.GetString(0),
								Rating = reader.GetInt32(1)
							});
						}
					}
				}
			}

			return ratings;
		}
	}
}
