using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using netcore_postgres_oauth_boiler.Models;
using netcore_postgres_oauth_boiler.Models.Config;
using netcore_postgres_oauth_boiler.Utilities;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace netcore_postgres_oauth_boiler.Controllers
{
    [Authorize("UnAuthorized")]
    public class AuthController : Controller
    {
        private readonly DatabaseContext _context;

        private readonly Request requester;
        private readonly ILogger<AuthController> _logger;
        private readonly IOptions<OAuthConfig> _oauthConfig;
        public AuthController(ILogger<AuthController> logger, IOptions<OAuthConfig> googleConfig, DatabaseContext context)
        {
            _logger = logger;
            _context = context;
            _oauthConfig = googleConfig;
            requester = new Request();
        }
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            _logger.LogInformation($"{email} is logging in.");

            // Fetching the user
            var user = await _context.Users.Where(c => Regex.IsMatch(c.Email, email)).FirstOrDefaultAsync();

            // Checking if user exists and verifying password
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                TempData["error"] = "Incorrect email or password!";
                return View("Login");
            }

            // Attaching user to session
            HttpContext.Session.SetString("user", user.Id);

            // Setting info alert to be shown
            TempData["info"] = "You have logged in!";

            // Rendering index
            return Redirect("/");
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromForm] string email, [FromForm] string password)
        {
            // Verifying data
            if (email == null || password == null)
            {
                TempData["error"] = "Missing username or password!";
                return View("Register");
            }

            // validating data
            if (!Validator.validatePassword(password) || !Validator.validateEmail(email))
            {
                TempData["error"] = "Email must be valid and the password must be between 6 and a 100 characters.";
                return View("Register");
            }

            // Checking for duplicates
            var count = await _context.Users.Where(c => Regex.IsMatch(c.Email, email)).CountAsync();
            if (count != 0)
            {
                TempData["error"] = "This email is already taken!";
                return View("Register");
            }

            // Saving the user
            User u = new User(email, password);
            _context.Users.Add(u);
            await _context.SaveChangesAsync();

            // Assigning user id to session
            HttpContext.Session.SetString("user", u.Id);

            // Setting info alert
            TempData["info"] = "You have successfully registered!";

            return Redirect("/");
        }
    }
}
