using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using netcore_postgres_oauth_boiler.Models;

namespace netcore_postgres_oauth_boiler.Controllers
{
	 public class AuthController : Controller
	 {

		  private readonly DatabaseContext _context;

		  private readonly ILogger<AuthController> _logger;

		  public AuthController(ILogger<AuthController> logger, DatabaseContext context)
		  {
				_logger = logger;
				_context = context;
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
				Console.WriteLine($"{email} is logging in.");

				// Loading session
				if (!HttpContext.Session.IsAvailable)
					 await HttpContext.Session.LoadAsync();

				if (HttpContext.Session.GetString("user") != null)
				{
					 ViewData["error"] = "You are already logged in!";
					 return View("Login");
				}

				BadRequestObjectResult failure = BadRequest("Wrong email/password combination!");
				var user = await _context.Users.Where(c => Regex.IsMatch(c.email, email)).FirstOrDefaultAsync();

				if (user==null)
				{
					 ViewData["error"] = "Incorrect email or password!";
					 return View("Login");
				}

				if (!BCrypt.Net.BCrypt.Verify(password,user.password))
				{
					 ViewData["error"] = "Incorrect email or password!";
					 return View("Login");
				}

				HttpContext.Session.SetString("user", user.id);

				ViewData["info"] = "You have logged in!";

				return View("~/Views/Home/Index.cshtml");
		  }

		  [HttpPost]
		  public async Task<IActionResult> Register([FromForm] string email, [FromForm] string password)
		  {
				// Loading session
				if (!HttpContext.Session.IsAvailable)
					 await HttpContext.Session.LoadAsync();

				// Verifying user is not logged in
				if (HttpContext.Session.GetString("user")!=null)
				{
					 ViewData["error"] = "You are already logged in!";
					 return View("Register");
				}

				// Verifying data
				if (email==null || password==null)
				{
					 ViewData["error"] = "Missing username or password!";
					 return View("Register");
				}

				// Checking for duplicates
				var count = await _context.Users.Where(c => Regex.IsMatch(c.email, email)).CountAsync();
				if (count != 0)
				{
					 ViewData["error"] = "This email is already taken!";
					 return View("Register");
				}

				// Saving the user
				User u = new User(email, password);
				_context.Users.Add(u);
				await _context.SaveChangesAsync();

				// Assigning user id to session
				HttpContext.Session.SetString("user", u.id);

				// Setting info alert
				ViewData["info"] = "You have successfully registered!";

				return View("~/Views/Home/Index.cshtml");
		  }

		  [HttpGet]
		  public async Task<IActionResult> SessionTest()
		  {
				if (!HttpContext.Session.IsAvailable)
					 await HttpContext.Session.LoadAsync();
				var c = HttpContext.Session.GetString("user");

				return Ok("You are: "+c);
		  }


		  [HttpGet]
		  public async Task<IActionResult> Logout()
		  {
				// Removing session
				if (!HttpContext.Session.IsAvailable)
					 await HttpContext.Session.LoadAsync();
				HttpContext.Session.Clear();

				return Redirect("/");
		  }

		  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		  public IActionResult Error()
		  {
				return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		  }
	 }
}
