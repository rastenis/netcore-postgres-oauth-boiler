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
    [Authorize("Authorized")]
    public class SessionController : Controller
    {
        public SessionController()
        {
        }

        [HttpGet]
        public IActionResult SessionTest()
        {
            return Ok("You are: " + HttpContext.Session?.GetString("user") ?? "not logged in.");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // Removing session
            HttpContext.Session.Clear();

            TempData["info"] = "You have logged out!";
            return Redirect("/");
        }
    }
}
