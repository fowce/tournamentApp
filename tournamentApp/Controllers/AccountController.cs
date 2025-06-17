using Microsoft.AspNetCore.Mvc;
using tournamentApp.Data.Repository;
using tournamentApp.Models.Entities;
using tournamentApp.AuditService;

namespace tournamentApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly RegisterUser _registerUser;

        private readonly LoginUser _loginUser;

		private readonly FileLogger _logger;

		private readonly IWebHostEnvironment _webHostEnv;

		public AccountController(
                                RegisterUser registerUser, 
                                LoginUser loginUser, 
                                FileLogger logger,
								IWebHostEnvironment webHostEnv)
        {
            _registerUser = registerUser;
            _loginUser = loginUser;
			_logger = logger;
            _webHostEnv = webHostEnv;
		}

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    int userId = _registerUser.AddUser(viewModel);

                    _registerUser.AddRatingUser(userId);

					_logger.AddLogToFile(
						_webHostEnv,
						$"User {viewModel.Name} successfully registered",
						userId,
						"");

					return RedirectToAction("Login");
                }
                catch (InvalidOperationException ex)
                {
					_logger.AddLogToFile(
						_webHostEnv,
						"Unsuccessful attempt to register in the system",
						userId: null,
						$"{ex.Message}");

					ModelState.AddModelError("Name", ex.Message);
                }
            }
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                bool isValidUser =_loginUser.ValidateUser(viewModel);

                if (isValidUser)
                {

					int userId = _loginUser.GetUserId(viewModel.EmailOrName);
                    string userRole = _loginUser.GetUserRole(viewModel.EmailOrName);

                    _logger.AddLogToFile(
                        _webHostEnv, 
                        "The user is logged in", 
                        userId, 
                        $"Email/Name: {viewModel.EmailOrName}");

					HttpContext.Session.SetInt32("UserId", userId);
                    HttpContext.Session.SetString("UserRole", userRole);

					return RedirectToAction("Index", "Home");
                }
                else
                {
					_logger.AddLogToFile(
						_webHostEnv,
						"Failed login attempt",
						userId: null,
						$"Email/Name: {viewModel.EmailOrName}");

					ModelState.AddModelError("EmailOrName", "Неверный email или пароль.");
                }
            }

            return View(viewModel);
        }

        [HttpGet]
		public IActionResult LogOut()
		{
            string userName = _loginUser.GetNameUser(HttpContext.Session.GetInt32("UserId"));

			_logger.AddLogToFile(
						_webHostEnv,
						"User logged out",
						HttpContext.Session.GetInt32("UserId"),
						$"Name: {userName}");

			return View("Login");
		}
	}
}
