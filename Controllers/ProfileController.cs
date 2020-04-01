using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using netcore_postgres_oauth_boiler.Models;
using netcore_postgres_oauth_boiler.Models.Config;
using netcore_postgres_oauth_boiler.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static netcore_postgres_oauth_boiler.Models.OAuthData;


namespace netcore_postgres_oauth_boiler.Controllers
{
    public class ProfileController : Controller
    {

        private readonly DatabaseContext _context;

        public ProfileController(ILogger<AuthController> logger, DatabaseContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("user") == null)
            {
                return Redirect("/Auth/Login");
            }

            var user = await _context.Users.Where(c => Regex.IsMatch(c.Id, HttpContext.Session.GetString("user"))).Include("Credentials").FirstOrDefaultAsync();

            if (user.Credentials == null)
            {
                user.Credentials = new List<Credential>();
            }

            var blankPassword = BCrypt.Net.BCrypt.Verify("", user.Password);
            ViewData["HasPassword"] = user.Password != null && !blankPassword;

            ViewData["GoogleLinked"] = user.Credentials.Exists(c => { return c.Provider == AuthProvider.GOOGLE; });
            ViewData["GithubLinked"] = user.Credentials.Exists(c => { return c.Provider == AuthProvider.GITHUB; });
            ViewData["RedditLinked"] = user.Credentials.Exists(c => { return c.Provider == AuthProvider.REDDIT; });

            int amount = user.Credentials.Count;

            ViewData["CanUnlinkGoogle"] = !(amount == 1 && (bool)ViewData["GoogleLinked"]);
            ViewData["CanUnlinkGithub"] = !(amount == 1 && (bool)ViewData["GithubLinked"]);
            ViewData["CanUnlinkReddit"] = !(amount == 1 && (bool)ViewData["RedditLinked"]);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromForm] string currentPassword, [FromForm] string newPassword)
        {
            // Loading session
            if (!HttpContext.Session.IsAvailable)
                await HttpContext.Session.LoadAsync();

            // Disallowing non logged-in users
            if (HttpContext.Session.GetString("user") == null)
            {
                return Redirect("/Auth/Login");
            }

            // Fetching the user
            var user = await _context.Users.Where(c => Regex.IsMatch(c.Id, HttpContext.Session.GetString("user"))).FirstOrDefaultAsync();

            // Checking if user exists and verifying password existence
            if (user == null || user.Password == null)
            {
                TempData["error"] = "You can not change your password.";
                return View("Index");
            }

            // Validating password
            if (!Validator.validatePassword(newPassword))
            {
                TempData["error"] = "Password must be between 6 and a 100 characters.";
                return View("Index");
            }

            // Verifying and changing password
            try
            {
                user.Password = BCrypt.Net.BCrypt.ValidateAndReplacePassword(currentPassword, user.Password, newPassword);
            }
            catch (Exception e)
            {
                TempData["error"] = "Incorrect old password!";
                return View("Index");
            }

            // Saving changes
            await _context.SaveChangesAsync();

            // Setting info alert to be shown
            TempData["info"] = "You have changed your password!";

            // Rendering index
            return Redirect("/");
        }


        [HttpPost]
        public async Task<IActionResult> ChangeOAuth(string submit)
        {
            // Loading session
            if (!HttpContext.Session.IsAvailable)
                await HttpContext.Session.LoadAsync();

            // Disallowing non logged-in users
            if (HttpContext.Session.GetString("user") == null)
            {
                return Redirect("/Auth/Login");
            }

            if (submit == null || submit == "")
            {
                TempData["error"] = $"No provider supplied.";
                return View("Index");
            }

            // Uppercasing first letter for formatting
            submit = submit.First().ToString().ToUpper() + submit.Substring(1);

            // Fetching the user
            var user = await _context.Users.Where(c => Regex.IsMatch(c.Id, HttpContext.Session.GetString("user"))).Include("Credentials").FirstOrDefaultAsync();

            AuthProvider provider;
            if (!Enum.TryParse(submit.ToUpper(), out provider))
            {
                TempData["error"] = $"Invalid provider.";
                return View("Index");
            }

            Credential c = user.Credentials.FirstOrDefault(c => { return c.Provider == provider; });
            if (c != null)
            {
                // Unlinking if possible
                if (user.Password != null || (user.Credentials.Count > 1))
                {
                    user.Credentials.Remove(c);
                }
                else
                {
                    // Should not be reachable.
                    TempData["error"] = $"You cannot unlink {submit}.";
                    return View("Index");
                }
            }
            else
            {
                return Redirect($"/Auth/{submit}");
            }

            // Saving changes
            await _context.SaveChangesAsync();

            // Setting info alert to be shown
            TempData["info"] = $"You have unlinked {submit}!";

            // Rendering index
            return Redirect("/");
        }
    }
}
