using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace netcore_postgres_oauth_boiler.Utilities
{
    public class Request
    {
        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        static readonly HttpClient client = new HttpClient();

        public Request()
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "netcore-postgres-oauth-boiler/0.0.1");
        }

        public async Task<T> Post<T>(string path, Dictionary<string, string> body, Dictionary<string, string> headers = null, StringContent customContent = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, path);

            // customContent allows for using application/x-www-form-urlencoded 
            request.Content = customContent ?? new StringContent(body != null ? JsonConvert.SerializeObject(body) : "",
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
            T userToken = JsonConvert.DeserializeObject<T>(responseContent, settings);

            return userToken;
        }

        public async Task<T> Get<T>(string path, Dictionary<string, string> headers = null)
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
            T userToken = JsonConvert.DeserializeObject<T>(responseContent, settings);

            return userToken;
        }
    }
}
