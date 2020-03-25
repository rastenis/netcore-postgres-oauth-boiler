using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using netcore_postgres_oauth_boiler.Models;
using netcore_postgres_oauth_boiler.Models.Config;
using Newtonsoft.Json;

namespace netcore_postgres_oauth_boiler.Controllers
{
    public class AuthController : Controller
    {

        private readonly DatabaseContext _context;

        private readonly ILogger<AuthController> _logger;
        private readonly IOptions<GoogleConfig> _googleConfig;
        public AuthController(ILogger<AuthController> logger, IOptions<GoogleConfig> googleConfig, DatabaseContext context)
        {
            _logger = logger;
            _context = context;
            _googleConfig = googleConfig;
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

            // Disallowing already logged-in users
            if (HttpContext.Session.GetString("user") != null)
            {
                TempData["error"] = "You are already logged in!";
                return View("Login");
            }

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
            // Loading session
            if (!HttpContext.Session.IsAvailable)
                await HttpContext.Session.LoadAsync();

            // Verifying user is not logged in
            if (HttpContext.Session.GetString("user") != null)
            {
                TempData["error"] = "You are already logged in!";
                return View("Register");
            }

            // Verifying data
            if (email == null || password == null)
            {
                TempData["error"] = "Missing username or password!";
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

        [HttpGet]
        public async Task<IActionResult> SessionTest()
        {
            if (!HttpContext.Session.IsAvailable)
                await HttpContext.Session.LoadAsync();
            var c = HttpContext.Session.GetString("user");

            return Ok("You are: " + c ?? "not logged in.");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Removing session
            if (!HttpContext.Session.IsAvailable)
                await HttpContext.Session.LoadAsync();
            HttpContext.Session.Clear();

            TempData["info"] = "You have logged out!";
            return Redirect("/");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> Google()
        {
            string Googleurl = $"https://accounts.google.com/o/oauth2/auth?response_type=code&redirect_uri=https://{this.Request.Host}/Auth/GoogleCallback&scope=email+profile+openid&client_id={_googleConfig.Value.client_id}";
            return Redirect(Googleurl);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback([FromQuery]IDictionary<string, string> query)
        {
            if (query["code"] == null)
            {
                return Redirect("/");
            }

            GoogleToken userToken = await _requestAuthDetails<GoogleToken>("https://accounts.google.com/o/oauth2/token",
                $"code={ query["code"] }&client_id={ _googleConfig.Value.client_id }&client_secret={_googleConfig.Value.client_secret}&redirect_uri=https://{this.Request.Host}/Auth/GoogleCallback&grant_type=authorization_code");

            if (userToken == null)
            {
                TempData["error"] = "Could not link your account.";
                return Redirect("/");
            }

            if (string.IsNullOrEmpty(userToken.access_token) || string.IsNullOrEmpty(userToken.id_token))
            {
                TempData["error"] = "Could not link your account. Provider returned no info.";
                return Redirect("/");
            }

            // An alternative for using Google's library for verifying the token would be to
            // make a GET request to https://www.googleapis.com/oauth2/v2/tokeninfo?id_token={id_token}
            // which would verify the token but would introduce one more web request in the process.
            // This does not scale well for more complex use cases; Google's library does the verification offline.
            var validPayload = await GoogleJsonWebSignature.ValidateAsync(userToken.id_token);
            if (validPayload is null)
            {
                TempData["error"] = "Identity of the provider token could not be verified.";
                return Redirect("/");
            }

            // Fetching data
            var userWithMatchingToken = await _context.Users.Where(c => c.Credentials.Any(cred => cred.Provider == AuthProvider.GOOGLE && cred.Token == validPayload.Subject)).FirstOrDefaultAsync();
            var userWithMatchingEmail = await _context.Users.Where(c => c.Email == validPayload.Email).FirstOrDefaultAsync();

            // If user is logged in and the auth token is not registered yet, link.
            if (HttpContext.Session.GetString("user") != null)
            {
                var user = await _context.Users.Where(c => c.Id == HttpContext.Session.GetString("user")).FirstOrDefaultAsync();

                // If someone already has that token OR there is a user that has the email but is not the same user.
                if (userWithMatchingToken != null || (userWithMatchingEmail != null && userWithMatchingEmail.Email != user.Email))
                {
                    TempData["error"] = "This Google account is already linked!";
                    return Redirect("/");
                }

                // Adding the token and saving
                user.Credentials.Add(new Credential(AuthProvider.GOOGLE, validPayload.Subject));
                await _context.SaveChangesAsync();

                TempData["info"] = "You have linked your Google account!";
                return Redirect("/");
            }

            // If user is NOT logged in, check if linked to some account, and log user in.
            if (userWithMatchingToken != null)
            {
                HttpContext.Session.SetString("user", userWithMatchingToken.Id);
                return Redirect("/");
            }

            // If NOT linked, create a new account ONLY if that email is not used already.`
            if (userWithMatchingEmail?.Email == validPayload.Email)
            {
                TempData["error"] = "This Google account's email has been used to create an account here, so you can not link it!";
                return Redirect("/");
            }

            // Creating a new account:
            User u = new User(null, "", new Credential(AuthProvider.GOOGLE, validPayload.Subject));
            _context.Users.Add(u);
            await _context.SaveChangesAsync();

            // Assigning user id to session
            HttpContext.Session.SetString("user", u.Id);

            // Setting info alert
            TempData["info"] = "You have successfully created an account with Google!";

            return Redirect("/");
        }

        public async Task<T> _requestAuthDetails<T>(string path, string parameters)
        {
            // Constructing request to retrieve access token
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(path);
            webRequest.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(parameters);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;

            // Opening a request
            Stream postStream = await webRequest.GetRequestStreamAsync();

            // Add the post data to the web request
            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            // Getting response
            WebResponse response = await webRequest.GetResponseAsync();
            postStream = response.GetResponseStream();

            // Reading response
            string responseFromServer = await new StreamReader(postStream).ReadToEndAsync();

            // Deserializing Google auth info
            T userToken = JsonConvert.DeserializeObject<T>(responseFromServer);

            return userToken;
        }

    }
    public class GoogleToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string id_token { get; set; }
        public string refresh_token { get; set; }
    }
}
