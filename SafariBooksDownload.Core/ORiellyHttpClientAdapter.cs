using System.Net;
using Newtonsoft.Json;

namespace SafariBooksDownload
{

    public class ORiellyHttpClientAdapter
    {
        public const string SafariHost = "learning.oreilly.com";
        public static string SafariBaseUrl => "https://" + SafariHost;

        private readonly HttpClient client;
        private readonly CookieContainer cookieContainer;
        private readonly string cookiesFile;

        public ORiellyHttpClientAdapter(string cookiesFile)
        {
            this.cookiesFile = cookiesFile;
            cookieContainer = new CookieContainer();
            LoadCookiesFromFile(cookiesFile);

            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = false
            };

            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", @"*/*"); // Example 
            client.DefaultRequestHeaders.Add("Accept-Encoding", @"deflate"); // Example 
            client.DefaultRequestHeaders.Add("Referer", SafariBaseUrl + "/"); // Example
        }

        public async Task<HttpResponseMessage?> GetAsync(string url)
        {
            try
            {
                client.DefaultRequestHeaders.Add("Host", new Uri(url).Host);
              
                var response = await client.GetAsync(url);

                UpdateCookies(response);  // Update cookies after the request

                if ((int)response.StatusCode >= 300)
                    throw new InvalidOperationException(
                        $"Unexpected status {(int)response.StatusCode} for {url} — session may be expired (auto-redirect is off, so 3xx is treated as auth failure).");

                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }

        public async Task<HttpResponseMessage?> PostAsync(string url, HttpContent content)
        {
            try
            {
                var response = await client.PostAsync(url, content);
                UpdateCookies(response);  // Update cookies after the request
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }


        private void LoadCookiesFromFile(string filePath)
        {
            var cookieData = File.ReadAllText(filePath);
            var cookies = JsonConvert.DeserializeObject<Dictionary<string, string>>(cookieData)
                          ?? new Dictionary<string, string>();

            foreach (var cookie in cookies)
            {
                cookieContainer.Add(new Uri(SafariBaseUrl), new Cookie(cookie.Key, cookie.Value));
                Console.WriteLine(cookie.Key);
            }
        }

        private void UpdateCookies(HttpResponseMessage response)
        {
            var updatedCookies = false;
            IEnumerable<string>? cookies;
            
            if (response.Headers.TryGetValues("Set-Cookie", out cookies))
            {
                foreach (var cookie in cookies)
                {
                    var cookieData = cookie.Split(';')[0];
                    var cookieKeyValue = cookieData.Split('=');
                    if (cookieKeyValue.Length == 2)
                    {
                        cookieContainer.Add(new Uri(SafariBaseUrl), new Cookie(cookieKeyValue[0], cookieKeyValue[1]));
                        updatedCookies = true;
                    }
                    
                }
            }
            if (updatedCookies)
            {
                SaveCookiesToFile(cookiesFile);
                LoadCookiesFromFile(cookiesFile);
            }
        }

        private void SaveCookiesToFile(string filePath)
        {
            var cookies = new Dictionary<string, string>();
            foreach (Cookie cookie in cookieContainer.GetCookies(new Uri(SafariBaseUrl)))
            {
                cookies[cookie.Name] = cookie.Value;
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(cookies));
        }
    }
}
