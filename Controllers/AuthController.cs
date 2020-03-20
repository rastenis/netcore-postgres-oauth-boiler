using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

		  [HttpPost]
		  public IActionResult Login([FromForm] string email, [FromForm] string password)
		  {
				Console.WriteLine("Logging in with ", email, password);
				return Redirect("/");
		  }

		  [HttpPost]
		  public async Task<IActionResult> Register([FromForm] string email, [FromForm] string password)
		  {
				Console.WriteLine("Registering with ", email, password);
				_context.Users.Add(new User("", ""));
				await _context.SaveChangesAsync();
				return Ok();
		  }

		  [HttpGet]
		  public IActionResult Register()
		  {
				Console.WriteLine("ssss with ");
				return Ok("OKAY");
		  }

		  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		  public IActionResult Error()
		  {
				return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		  }
	 }
}
