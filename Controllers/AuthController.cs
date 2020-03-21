using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

		  [HttpPost]
		  public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
		  {
				Console.WriteLine("Logging in with ", email, password);

				BadRequestObjectResult failure = BadRequest("Wrong email/password combination!");
				var user = await _context.Users.Where(c => Regex.IsMatch(c.email, email)).FirstOrDefaultAsync();

				if (user==null)
				{
					 return failure;
				}

				if (!BCrypt.Net.BCrypt.Verify(password,user.password))
				{
					 return failure;
				}
				
				// TODO: assign session

				return Redirect("/");
		  }

		  [HttpPost]
		  public async Task<IActionResult> Register([FromForm] string email, [FromForm] string password)
		  {
				Console.WriteLine("Registering with ", email, password);

				var count = await _context.Users.Where(c => Regex.IsMatch(c.email, email)).CountAsync();
				if (count != 0)
				{
					 return BadRequest("This email is already taken!");
				}

				_context.Users.Add(new User(email, password, null));
				await _context.SaveChangesAsync();

				// TODO: Assign session

				return Ok();
		  }

		  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		  public IActionResult Error()
		  {
				return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		  }
	 }
}
