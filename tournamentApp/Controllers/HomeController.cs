using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using tournamentApp.Data.Repository;
using tournamentApp.Models;
using tournamentApp.Models.Entities;
using tournamentApp.AuditService;

namespace tournamentApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly FileLogger _logger;

		private readonly CreateTournament _createTournament;

		private readonly DisplayTournaments _displayTournaments;

		private readonly DisplayPlayers _displayPlayers;

		private readonly Matches _matches;

		private readonly Profile _profile;

		private readonly DisplayRatingPlayers _displayRating;

		private readonly IWebHostEnvironment _webHostEnv;

		public HomeController(FileLogger logger
                            , CreateTournament createTournament
                            , DisplayTournaments tournaments
                            , DisplayPlayers players
							, Profile profile
							, Matches matches
							, DisplayRatingPlayers displayRating
							, IWebHostEnvironment webHostEnv)
        {
            _logger = logger;
            _createTournament = createTournament;
			_displayTournaments = tournaments;
            _displayPlayers = players;
			_profile = profile;
			_matches = matches;
			_displayRating = displayRating;
			_webHostEnv = webHostEnv;
		}

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CreateTournament()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateTournament(CreateTournamentViewModel viewModel) 
        {
            if (ModelState.IsValid)
            {
				int? userId = HttpContext.Session.GetInt32("UserId");

				if (userId == null)
				{
					return RedirectToAction("Login", "Account");
				}

				try
                {
					_createTournament.AddTournament(viewModel, userId);

					_logger.AddLogToFile(
						_webHostEnv,
						"User created tournament",
						userId,
						$"Name tournament:{viewModel.NameTournament}; " +
						$"Game:{viewModel.SelectGame}; " +
						$"Format:{viewModel.SelectFormat}; " +
						$"Start_Date:{viewModel.dateEvent}");

					return RedirectToAction("Index");
				}
                catch (InvalidOperationException ex)
                {
					_logger.AddLogToFile(
						_webHostEnv,
						"Failed attempt to create tournament",
						userId,
						$"{ex.Message}");

					ModelState.AddModelError("NameTournament", ex.Message);
                }
			}

            return View(viewModel);
        }

        [HttpPost]
		public IActionResult JoinSide(int id, string side)
		{
			int? userId = HttpContext.Session.GetInt32("UserId");

			if (userId == null)
			{
				return RedirectToAction("Login", "Account");
			}

			try
			{
				_createTournament.AddUserToSide(id, userId.Value, side);

				_logger.AddLogToFile(
						_webHostEnv,
						"User joined the tournament",
						userId,
						$"Side:{side}");
			}
			catch (InvalidOperationException ex)
			{
				_logger.AddLogToFile(
						_webHostEnv,
						"Failed attempt to join the tournament",
						userId,
						$"{ex.Message}");

				TempData["JoinError"] = ex.Message;
			}

			return RedirectToAction("Teams", new { id });
		}

		[HttpPost]
		public IActionResult LeaveTournament(int id)
		{
			int? userId = HttpContext.Session.GetInt32("UserId");

			if (userId == null)
			{
				return RedirectToAction("Login", "Account");
			}

			try
			{
				_createTournament.RemoveParticipant(id, userId.Value);

				_logger.AddLogToFile(
					_webHostEnv,
					"User left the tournament",
					userId,
					$"TournamentId: {id}");

				TempData["LeaveSuccess"] = "Вы успешно покинули турнир.";
			}
			catch (InvalidOperationException ex)
			{
				_logger.AddLogToFile(
					_webHostEnv,
					"Failed attempt to leave the tournament",
					userId,
					$"{ex.Message}");

				TempData["LeaveError"] = ex.Message;
			}

			return RedirectToAction("Teams", new { id });
		}

		[HttpGet]
        public IActionResult ListTournaments(TournamentFilter? filter)
        {
			var tournaments = _displayTournaments.GetAllTournaments(filter);
            ViewBag.AvailableGames = _displayTournaments.GetAvailableGames();
			ViewBag.AvailableFormats = _displayTournaments.GetAvailableFormats();
            return View(tournaments);
        }

		[HttpGet]
		public IActionResult MyTournaments(TournamentFilter? filter)
		{
			int? userId = HttpContext.Session.GetInt32("UserId");

			if (userId == null)
			{
				return RedirectToAction("Login", "Account");
			}

			var tournaments = _displayTournaments.GetTournamentsByCreator(userId.Value);

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.GameName))
                    tournaments = tournaments.Where(t => t.GameName == filter.GameName).ToList();

                if (!string.IsNullOrEmpty(filter.Format))
                    tournaments = tournaments.Where(t => t.Format == filter.Format).ToList();

                if (!string.IsNullOrEmpty(filter.SearchName))
                    tournaments = tournaments.Where(t => t.Name.Contains(filter.SearchName)).ToList();
            }

            ViewBag.AvailableGames = _displayTournaments.GetAvailableGames();
            ViewBag.AvailableFormats = _displayTournaments.GetAvailableFormats();

            return View("ListTournaments", tournaments);
		}

		[HttpGet]
        public IActionResult Teams(int id)
        {
			var model = _displayPlayers.GetTournamentParticipants(id);
			return View(model);
		}

        [HttpGet]
        public IActionResult Profile()
        {
			int? userId = HttpContext.Session.GetInt32("UserId");

			var profileUser = _profile.GetProfileUser(userId.Value);

            return View(profileUser);
        }

		[HttpGet]
        public IActionResult Matches(int tournamentId)
        {
			var viewModel = new MatchViewModel
			{
				TournamentId = tournamentId,
				ScoreLeftSide = _matches.GetMatch(tournamentId).ScoreSideL,
				ScoreRightSide = _matches.GetMatch(tournamentId).ScoreSideR
			};

			return View(viewModel);
		}

		[HttpPost]
		public IActionResult UpdateMatch(MatchViewModel viewModel)
		{
			if (ModelState.IsValid)
			{
				int? userId = HttpContext.Session.GetInt32("UserId");

				if (userId == null || HttpContext.Session.GetString("UserRole") != "admin")
				{
					return RedirectToAction("Login", "Account");
				}
				else
				{
					try
					{
						string tournamentStatus = _displayTournaments.GetStatusTournament(viewModel.TournamentId);

						_matches.UpdateMatch(viewModel, tournamentStatus);

						_logger.AddLogToFile(
						_webHostEnv,
						"Administrator updated the match results",
						userId,
						$"TournamentId:{viewModel.TournamentId}; " +
						$"ScoreLeftSide:{viewModel.ScoreLeftSide}; " +
						$"ScoreRightSide:{viewModel.ScoreRightSide}");

						return RedirectToAction("Teams", new { id = viewModel.TournamentId });
					}
					catch (InvalidOperationException ex)
					{
						_logger.AddLogToFile(
						_webHostEnv,
						"Unsuccessful attempt to save the match results",
						userId,
						$"{ex.Message}");

						TempData["UpdateError"] = ex.Message;
					}
				}
			}

			viewModel.ScoreLeftSide = _matches.GetMatch(viewModel.TournamentId).ScoreSideL;
			viewModel.ScoreRightSide = _matches.GetMatch(viewModel.TournamentId).ScoreSideR;
			return View("Matches", viewModel);
		}

        [HttpGet]
        public IActionResult DisplayUsers()
		{
			var users = _profile.GetUsers();
			return View(users);
		}

		[HttpPost]
		public IActionResult DeleteUser(User user)
		{
			int? adminId = HttpContext.Session.GetInt32("UserId");

			if (HttpContext.Session.GetString("UserRole") != "admin" || adminId == null)
			{
				return RedirectToAction("Login", "Account");
			}

			_profile.DeleteUser(user.Id);

			_logger.AddLogToFile(
					_webHostEnv,
					$"User has been deleted by admin",
                    user.Id,
					$"UserName:{user.Name}; " +
					$"UserEmail:{user.Email}");

			return RedirectToAction("DisplayUsers");

        }

		[HttpGet]
		public IActionResult EditUser(int userId)
		{
			var users = _profile.GetUsers();
			var userToEdit = users.FirstOrDefault(u => u.Id == userId);

			if (userToEdit == null)
			{
				return NotFound();
			}

			var editUser = new EditUser
			{
				Id = userToEdit.Id,
				Name = userToEdit.Name,
				Email = userToEdit.Email
			};

			return View(editUser);
		}

		[HttpPost]
        public IActionResult EditUser(EditUser user)
        {
			if (ModelState.IsValid)
			{
				int? adminId = HttpContext.Session.GetInt32("UserId");

				if (HttpContext.Session.GetString("UserRole") != "admin" || adminId == null)
				{
					return RedirectToAction("Login", "Account");
				}
				try
				{
					_profile.EditUser(user);

					_logger.AddLogToFile(
					_webHostEnv,
					"User data updated by admin",
					adminId,
					$"UserID:{user.Id}; " +
					$"NewName:{user.Name ?? "not changed"}; " +
					$"NewEmail:{user.Email ?? "not changed"}");

					return RedirectToAction("DisplayUsers");
				}
				catch (InvalidOperationException ex)
				{
					_logger.AddLogToFile(
					_webHostEnv,
					"Failed to update user data",
					adminId,
					$"Error:{ex.Message}");

					TempData["ErrorMessage"] = ex.Message;
				}
				
			}

			return View(user);
		}

        [HttpGet]
		public IActionResult PlayersRating()
		{
			var ratingPlayers = _displayRating.GetPlayerRatings();
			return View(ratingPlayers);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
