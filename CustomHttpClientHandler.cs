using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SafariBooksDownload
{

    public class CustomHttpClientHandler 
    {
        private readonly HttpClient client;
        private readonly CookieContainer cookieContainer;

        public CustomHttpClientHandler()
        {
            cookieContainer = new CookieContainer();
            LoadCookiesFromFile(Config.COOKIES_FILE);

            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = false
            };

            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", @"*/*"); // Example 
            client.DefaultRequestHeaders.Add("Accept-Encoding", @"deflate"); // Example 
            client.DefaultRequestHeaders.Add("Referer", @"https://learning.oreilly.com/"); // Example
            //client.DefaultRequestHeaders.Add("Host", @"www.oreilly.com"); // Example


            //client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");


        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            try
            {
                client.DefaultRequestHeaders.Add("Host", new Uri(url).Host);
                var response = await client.GetAsync(url);
                UpdateCookies(response);  // Update cookies after the request
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
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


        private string GetCookiesHeader()
        {
            var cookies = cookieContainer.GetCookies(new Uri(Config.SAFARI_BASE_URL));
            return string.Join("; ", cookies.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));
        }

        private void LoadCookiesFromFile(string filePath)
        {
            //if (File.Exists(filePath))
            {
                var cookieData = File.ReadAllText(filePath);
                var cookies = JsonConvert.DeserializeObject<Dictionary<string, string>>(cookieData);

                foreach (var cookie in cookies)
                {
                    cookieContainer.Add(new Uri(Config.SAFARI_BASE_URL), new Cookie(cookie.Key, cookie.Value));
                    Console.WriteLine(cookie.Key);
                }
            }
        }

        private bool UpdateCookies(HttpResponseMessage response)
        {
            var updatedCookies = false;
            IEnumerable<string> cookies;
            
            if (response.Headers.TryGetValues("Set-Cookie", out cookies))
            {
                foreach (var cookie in cookies)
                {
                    var cookieData = cookie.Split(';')[0];
                    var cookieKeyValue = cookieData.Split('=');
                    if (cookieKeyValue.Length == 2)
                    {
                        cookieContainer.Add(new Uri(Config.SAFARI_BASE_URL), new Cookie(cookieKeyValue[0], cookieKeyValue[1]));
                        updatedCookies = true;
                    }
                    
                }
            }
            if (updatedCookies)
            {
                SaveCookiesToFile(Config.COOKIES_FILE);
                LoadCookiesFromFile(Config.COOKIES_FILE);
            }
            return updatedCookies;
        }

        private void SaveCookiesToFile(string filePath)
        {
            var cookies = new Dictionary<string, string>();
            foreach (Cookie cookie in cookieContainer.GetCookies(new Uri(Config.SAFARI_BASE_URL)))
            {
                cookies[cookie.Name] = cookie.Value;
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(cookies));
        }
    }
}
