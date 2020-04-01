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

        public ProfileController(ILogger<AuthController> logger,  DatabaseContext context)
        {
            _context = context;
        }
        public IActionResult Profile()
        {
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
                return View("Login");
            }

            // Fetching the user
            var user = await _context.Users.Where(c => Regex.IsMatch(c.Id, HttpContext.Session.GetString("user"))).FirstOrDefaultAsync();

            // Checking if user exists and verifying password existence
            if (user == null || user.Password == null)
            {
                TempData["error"] = "You can not change your password.";
                return View("Profile");
            }

            // Validating password
            if (!Validator.validatePassword( newPassword))
            {
                TempData["error"] = "Password must be between 6 and a 100 characters.";
                return View("Profile");
            }

            // Verifying and changing password
            try
            {
                user.Password = BCrypt.Net.BCrypt.ValidateAndReplacePassword(currentPassword, user.Password, newPassword);
            }catch(Exception e)
            {
                TempData["error"] = "Incorrect old password!";
                return View("Profile");
            }

            // Saving changes
            await _context.SaveChangesAsync();

            // Setting info alert to be shown
            TempData["info"] = "You have changed your password!";

            // Rendering index
            return Redirect("/");
        }

    }
}
