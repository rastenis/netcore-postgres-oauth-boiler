using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using netcore_postgres_oauth_boiler.Models;
using netcore_postgres_oauth_boiler.Models.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace netcore_postgres_oauth_boiler.Controllers
{
    public class AuthController : Controller
    {

        private readonly DatabaseContext _context;
        static readonly HttpClient client = new HttpClient();


        private readonly ILogger<AuthController> _logger;
        private readonly IOptions<OAuthConfig> _oauthConfig;
        public AuthController(ILogger<AuthController> logger, IOptions<OAuthConfig> googleConfig, DatabaseContext context)
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "netcore-postgres-oauth-boiler/0.0.1");

            _logger = logger;
            _context = context;
            _oauthConfig = googleConfig;
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
            string googleUrl = $"https://accounts.google.com/o/oauth2/auth?response_type=code&redirect_uri=https://{this.Request.Host}/Auth/GoogleCallback&scope=email+profile+openid&client_id={_oauthConfig.Value.Google.client_id}";
            return Redirect(googleUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Github()
        {
            string GithubrUrl = $"https://github.com/login/oauth/authorize?scope=read:user&client_id={_oauthConfig.Value.Github.client_id}";
            return Redirect(GithubrUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Reddit()
        {
            string RedditUrl = $"https://github.com/login/oauth/authorize?scope=identity&client_id={_oauthConfig.Value.Reddit.client_id}&response_type=core&state={Guid.NewGuid().ToString()}&redirect_uri=https://{this.Request.Host}/Auth/RedditCallback&duration=temporary";
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


            GoogleToken userToken = await _post<GoogleToken>("https://accounts.google.com/o/oauth2/token", parameters);

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

            GithubToken userToken = await _post<GithubToken>("https://github.com/login/oauth/access_token", parameters);

            if (userToken == null)
            {
                TempData["error"] = "Could not link your account.";
                return Redirect("/");
            }

            if (string.IsNullOrEmpty(userToken.access_token))
            {
                TempData["error"] = "Could not link your account. Provider returned no info.";
                return Redirect("/");
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", $"token {userToken.access_token}");

            GithubUserInfo userinfo = await _get<GithubUserInfo>("https://api.github.com/user", headers);

            if (userinfo is null)
            {
                TempData["error"] = "Identity could not be resolved.";
                return Redirect("/");
            }

            // Fetching data
            var userWithMatchingToken = await _context.Users.Where(c => c.Credentials.Any(cred => cred.Provider == AuthProvider.GITHUB && cred.Token == userinfo.id)).FirstOrDefaultAsync();
            var userWithMatchingEmail = await _context.Users.Where(c => c.Email == userinfo.email).FirstOrDefaultAsync();

            // If user is logged in and the auth token is not registered yet, link.
            if (HttpContext.Session.GetString("user") != null)
            {
                var user = await _context.Users.Where(c => c.Id == HttpContext.Session.GetString("user")).FirstOrDefaultAsync();

                // If someone already has that token OR there is a user that has the email but is not the same user.
                if (userWithMatchingToken != null || (userWithMatchingEmail != null && userWithMatchingEmail.Email != user.Email))
                {
                    TempData["error"] = "This Github account is already linked!";
                    return Redirect("/");
                }

                // Adding the token and saving
                user.Credentials.Add(new Credential(AuthProvider.GITHUB, userinfo.id));
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
            if (userWithMatchingEmail?.Email == userinfo.email)
            {
                TempData["error"] = "This Github account's email has been used to create an account here, so you can not link it!";
                return Redirect("/");
            }

            // Creating a new account:
            User u = new User(null, "", new Credential(AuthProvider.GITHUB, userinfo.id));
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

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["code"] = query["code"];
            parameters["redirect_uri"] = $"https://{this.Request.Host}/Auth/RedditCallback";


            Dictionary<string, string> headers = new Dictionary<string, string>();
            // TODO: client_id:client_secret in b64
            headers.Add("Authorization", $"Basic {WIP}");

            RedditToken userToken = await _post<RedditToken>("https://www.reddit.com/api/v1/access_token", parameters);

            return Redirect("/");
        }

        public async Task<T> _post<T>(string path, Dictionary<string, string> body, Dictionary<string, string> headers = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = new StringContent(body != null ? JsonConvert.SerializeObject(body) : "",
                                                Encoding.UTF8,
                                                "application/json");

            // Adding headers if any
            foreach (var header in headers ?? new Dictionary<string, string>())
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var response = await client.SendAsync(request);

            // Deserializing Google auth info
            string responseContent = await response.Content.ReadAsStringAsync();
            T userToken = JsonConvert.DeserializeObject<T>(responseContent);

            return userToken;
        }

        public async Task<T> _get<T>(string path, Dictionary<string, string> headers = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, path);

            // Adding headers if any
            foreach (var header in headers ?? new Dictionary<string, string>())
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var response = await client.SendAsync(request);

            // Deserializing Google auth info
            string responseContent = await response.Content.ReadAsStringAsync();
            T userToken = JsonConvert.DeserializeObject<T>(responseContent);

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

    public class GithubToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }

    public class RedditToken : GithubToken { }

    public class GithubUserInfo
    {
        public string avatar_url { get; set; }
        public string created_at { get; set; }
        public string email { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        // And others...
    }
}
