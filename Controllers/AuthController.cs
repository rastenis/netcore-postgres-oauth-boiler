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
            if (HttpContext.Session.GetString("user") != null)
            {
                return Redirect("/");
            }

            return View();
        }

        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("user") != null)
            {
                return Redirect("/");
            }

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
                return Redirect("/");
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
                return Redirect("/");
            }

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
        public IActionResult Google()
        {
            string googleUrl = $"https://accounts.google.com/o/oauth2/auth?response_type=code&redirect_uri=https://{this.Request.Host}/Auth/GoogleCallback&scope=email+profile+openid&client_id={_oauthConfig.Value.Google.client_id}";
            return Redirect(googleUrl);
        }

        [HttpGet]
        public IActionResult Github()
        {
            string GithubUrl = $"https://github.com/login/oauth/authorize?scope=user&client_id={_oauthConfig.Value.Github.client_id}";
            return Redirect(GithubUrl);
        }

        [HttpGet]
        public IActionResult Reddit()
        {
            string RedditUrl = $"https://www.reddit.com/api/v1/authorize?scope=identity&client_id={_oauthConfig.Value.Reddit.client_id}&response_type=code&state={Guid.NewGuid().ToString()}&redirect_uri=https://{this.Request.Host}/Auth/RedditCallback&duration=temporary";
            return Redirect(RedditUrl);
        }


        [HttpGet]
        public async Task<IActionResult> GoogleCallback([FromQuery]IDictionary<string, string> query)
        {
            if (query["code"] == null)
            {
                return Redirect("/");
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["code"] = query["code"];
            parameters["client_id"] = _oauthConfig.Value.Google.client_id;
            parameters["redirect_uri"] = $"https://{this.Request.Host}/Auth/GoogleCallback";
            parameters["client_secret"] = _oauthConfig.Value.Google.client_secret;
            parameters["grant_type"] = "authorization_code";

            GoogleToken userToken = await requester.Post<GoogleToken>("https://accounts.google.com/o/oauth2/token", parameters);

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
                var user = await _context.Users.Where(c => c.Id == HttpContext.Session.GetString("user")).Include("Credentials").FirstOrDefaultAsync();

                // If someone already has that token OR there is a user that has the email but is not the same user.
                if (userWithMatchingToken != null || (userWithMatchingEmail != null && userWithMatchingEmail.Email != user.Email))
                {
                    TempData["error"] = "This Google account is already linked!";
                    return Redirect("/");
                }

                if (user.Credentials == null)
                {
                    user.Credentials = new List<Credential>();
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
            User u = new User(validPayload.Email, "", new Credential(AuthProvider.GOOGLE, validPayload.Subject));
            _context.Users.Add(u);
            await _context.SaveChangesAsync();

            // Assigning user id to session
            HttpContext.Session.SetString("user", u.Id);

            // Setting info alert
            TempData["info"] = "You have successfully created an account with Google!";

            return Redirect("/");
        }

        [HttpGet]
        public async Task<IActionResult> GithubCallback([FromQuery]IDictionary<string, string> query)
        {

            if (query["code"] == null)
            {
                return Redirect("/");

            }
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["code"] = query["code"];
            parameters["client_id"] = _oauthConfig.Value.Github.client_id;
            parameters["redirect_uri"] = $"https://{this.Request.Host}/Auth/GithubCallback";
            parameters["client_secret"] = _oauthConfig.Value.Github.client_secret;
            parameters["state"] = Guid.NewGuid().ToString();

            GithubToken userToken = await requester.Post<GithubToken>("https://github.com/login/oauth/access_token", parameters);

            if (userToken == null)
            {
                TempData["error"] = "Could not link your Github account.";
                return Redirect("/");
            }

            if (string.IsNullOrEmpty(userToken.access_token))
            {
                TempData["error"] = "Could not link your account. Provider (Github) returned no info.";
                return Redirect("/");
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", $"token {userToken.access_token}");

            GithubUserInfo userinfo = await requester.Get<GithubUserInfo>("https://api.github.com/user", headers);

            if (userinfo is null)
            {
                TempData["error"] = "Github identity could not be resolved.";
                return Redirect("/");
            }

            // Fetching data
            var userWithMatchingToken = await _context.Users.Where(c => c.Credentials.Any(cred => cred.Provider == AuthProvider.GITHUB && cred.Token == userinfo.Id)).FirstOrDefaultAsync();
            var userWithMatchingEmail = await _context.Users.Where(c => userinfo.Email != null && c.Email == userinfo.Email).FirstOrDefaultAsync();

            // If user is logged in and the auth token is not registered yet, link.
            if (HttpContext.Session.GetString("user") != null)
            {
                var user = await _context.Users.Where(c => c.Id == HttpContext.Session.GetString("user")).Include("Credentials").FirstOrDefaultAsync();

                // If someone already has that token OR there is a user that has the email but is not the same user.
                if (userWithMatchingToken != null || (userWithMatchingEmail != null && userWithMatchingEmail.Email != user.Email))
                {
                    TempData["error"] = "This Github account is already linked!";
                    return Redirect("/");
                }

                if (user.Credentials == null)
                {
                    user.Credentials = new List<Credential>();
                }

                // Adding the token and saving
                user.Credentials.Add(new Credential(AuthProvider.GITHUB, userinfo.Id));
                await _context.SaveChangesAsync();

                TempData["info"] = "You have linked your Github account!";
                return Redirect("/");
            }

            // If user is NOT logged in, check if linked to some account, and log user in.
            if (userWithMatchingToken != null)
            {
                HttpContext.Session.SetString("user", userWithMatchingToken.Id);
                return Redirect("/");
            }

            // If NOT linked, create a new account ONLY if that email is not used already.`
            if (userinfo.Email != null && userWithMatchingEmail?.Email == userinfo.Email)
            {
                TempData["error"] = "This Github account's email has been used to create an account here, so you can not link it!";
                return Redirect("/");
            }

            // Creating a new account:
            User u = new User(userinfo.Email, "", new Credential(AuthProvider.GITHUB, userinfo.Id));
            _context.Users.Add(u);
            await _context.SaveChangesAsync();

            // Assigning user id to session
            HttpContext.Session.SetString("user", u.Id);

            // Setting info alert
            TempData["info"] = "You have successfully created an account with Github!";

            return Redirect("/");
        }

        [HttpGet]
        public async Task<IActionResult> RedditCallback([FromQuery]IDictionary<string, string> query)
        {
            if (query["code"] == null)
            {
                TempData["info"] = "Link via Reddit failed. Try again later.";
                return Redirect("/");
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            var authBytes = Encoding.UTF8.GetBytes($"{_oauthConfig.Value.Reddit.client_id}:{_oauthConfig.Value.Reddit.client_secret}");
            headers.Add("Authorization", $"Basic {Convert.ToBase64String(authBytes)}");

            RedditToken userToken = await requester.Post<RedditToken>("https://www.reddit.com/api/v1/access_token", null, headers, new StringContent($"grant_type=authorization_code&code={query["code"]}&redirect_uri=https://{this.Request.Host}/Auth/RedditCallback",
                                              Encoding.UTF8,
                                              "application/x-www-form-urlencoded"));

            if (userToken == null)
            {
                TempData["error"] = "Could not link your Reddit account.";
                return Redirect("/");
            }

            if (string.IsNullOrEmpty(userToken.access_token))
            {
                TempData["error"] = "Could not link your account. Provider (Reddit) returned no info.";
                return Redirect("/");
            }

            headers = new Dictionary<string, string>();
            headers.Add("Authorization", $"bearer {userToken.access_token}");

            RedditUserInfo userinfo = await requester.Get<RedditUserInfo>("https://oauth.reddit.com/api/v1/me", headers);

            if (userinfo is null)
            {
                TempData["error"] = "Reddit identity could not be resolved.";
                return Redirect("/");
            }

            // Fetching data
            var userWithMatchingToken = await _context.Users.Where(c => c.Credentials.Any(cred => cred.Provider == AuthProvider.REDDIT && cred.Token == userinfo.Id)).FirstOrDefaultAsync();

            // Reddit does not have force-verified emails, so we do not look for email collisions.

            // If user is logged in and the auth token is not registered yet, link.
            if (HttpContext.Session.GetString("user") != null)
            {
                var user = await _context.Users.Where(c => c.Id == HttpContext.Session.GetString("user")).Include("Credentials").FirstOrDefaultAsync();

                // If someone already has that token OR there is a user that has the email but is not the same user.
                if (userWithMatchingToken != null)
                {
                    TempData["error"] = "This Reddit account is already linked!";
                    return Redirect("/");
                }

                if (user.Credentials == null)
                {
                    user.Credentials = new List<Credential>();
                }

                // Adding the token and saving
                user.Credentials.Add(new Credential(AuthProvider.REDDIT, userinfo.Id));
                await _context.SaveChangesAsync();

                TempData["info"] = "You have linked your Reddit account!";
                return Redirect("/");
            }

            // If user is NOT logged in, check if linked to some account, and log user in.
            if (userWithMatchingToken != null)
            {
                HttpContext.Session.SetString("user", userWithMatchingToken.Id);
                return Redirect("/");
            }

            // Creating a new account:
            User u = new User(null, "", new Credential(AuthProvider.REDDIT, userinfo.Id));
            _context.Users.Add(u);
            await _context.SaveChangesAsync();

            // Assigning user id to session
            HttpContext.Session.SetString("user", u.Id);

            // Setting info alert
            TempData["info"] = "You have successfully created an account with Reddit!";

            return Redirect("/");
        }
    }
}
