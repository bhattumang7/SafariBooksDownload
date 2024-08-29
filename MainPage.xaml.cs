

using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SafariBooksDownload
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<Book> Books { get; set; }
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            Books = new ObservableCollection<Book>();
            BindingContext = this;
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private async void OnSearchButtonClick(object sender, EventArgs e)
        {
            // Handle button click here
            //await DisplayAlert("Alert", "Clicked", "ok");

            var searchTerm = bookName.Text;
             await getJsonAsync(searchTerm);

        }


        private async Task<string> getJsonAsync(String searchContent)
        {
         
            try
            {
                
                searchContent = System.Web.HttpUtility.UrlEncode(searchContent);

                //CustomHttpClientHandlerInstance.Instance

                //var client = new HttpClient(CustomHttpClientHandlerInstance.Instance);
                /*string requestURL = "https://www.oreilly.com/search/api/search/?q=design&type=book&rows=100&language_with_transcripts=en&tzOffset=-5.5&feature_flags=improveSearchFilters&report=true&isTopics=false";
                var request = new HttpRequestMessage(HttpMethod.Get, requestURL);


                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

               var jsonDocument = JsonDocument.Parse(content);

                await DisplayAlert("asdf ", content + "\n" + response.StatusCode.ToString(), " ok");
                */
                /*string requestURL = "https://learning.oreilly.com/search/api/search/?q=sdfg&type=book&rows=100&language_with_transcripts=en&tzOffset=-5.5&feature_flags=improveSearchFilters&report=true&isTopics=false\r\n";
               */

                string requestURL = "https://www.oreilly.com/search/api/search/?q=" + searchContent + "&type=book&rows=100&language_with_transcripts=en&tzOffset=-5.5&feature_flags=improveSearchFilters&report=true&isTopics=false";
                CustomHttpClientHandler customHttpClientHandler = new CustomHttpClientHandler();
                var response = await customHttpClientHandler.GetAsync(requestURL);

                response.EnsureSuccessStatusCode();
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);


                var jsonDocument = JsonDocument.Parse(stringResponse);

                if (jsonDocument.RootElement.GetProperty("data").GetProperty("products").GetArrayLength() == 0)
                {
                    await DisplayAlert("Book not found", "No book was found for " + searchContent, " ok");
                }
               // List<Book> localList = new List<Book>();
                Books.Clear();
                foreach (var bookEntry in jsonDocument.RootElement.GetProperty("data").GetProperty("products").EnumerateArray())
                {
                    Book book = new Book
                    {
                        title = bookEntry.GetProperty("title").GetString(),
                        product_id = bookEntry.GetProperty("product_id").GetString(),
                        cover_image = bookEntry.GetProperty("cover_image").GetString(),
                        description = bookEntry.GetProperty("description").GetString(),
                        url = bookEntry.GetProperty("url").GetString(),
                        authors = string.Join(", ", bookEntry.GetProperty("authors").EnumerateArray().Select(author => author.GetString()))

                    };

                    Books.Add(book);
                  //  await DisplayAlert("asdf ", "\n" + response.StatusCode.ToString(), " ok");
                }
                //books.Add(localList);
                await DisplayAlert("asdf ", "\n" + Books.Count + " \n " + response.StatusCode.ToString(), " ok");
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return "";
        }
    }

}
