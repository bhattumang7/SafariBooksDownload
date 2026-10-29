using System.Text.RegularExpressions;

namespace SafariBooksDownload
{
    public partial class MainPage : ContentPage
    {
        private DownloadViewModel Progress { get; set; }

        private MainViewModel ViewModel { get; set; }

        private readonly IOReillyClient _client = new OReillyClient();


        public MainPage()
        {
            InitializeComponent();

            this.Progress = new DownloadViewModel();
            this.ViewModel = new MainViewModel();
            ViewModel.RetainFolder = false;
            BindingContext = ViewModel;
            downloadbtn.IsEnabled = false;
            Progress.ProgressBarValue = 0;
            downloadLabel.IsVisible = false;
            progressBar.IsVisible = false;
            progressLabel.IsVisible = false;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool hasPermission = await PermissionHelper.RequestStoragePermissions();

            if (!hasPermission)
            {
                await DisplayAlert("Permission Required", "Storage access is required to proceed.", "OK");
                return;
            }

            Config.EnsureBooksPathExists();
            Config.SweepOrphanWorkFolders();
        }
        
        private void OnSearchTextCompleted(object sender, EventArgs e)
        {
            downloadbtn.SendClicked();
        }

        private async void OnSearchButtonClick(object sender, EventArgs e)
        {
            try
            {
                ViewModel.LastFileDownloadPath = "";
                ViewModel.searchInProgress = true;
                ViewModel.Books.Clear();
                var searchTerm = bookName.Text;
                await GetJsonAsync(searchTerm);
                ViewModel.searchInProgress = false;
            }
            catch (Exception ex)
            {
                ViewModel.searchInProgress = false;
                await DisplayAlert("Error occured", ex.Message + "\r\n" + ex.StackTrace, " ok");
            }
        }

        private async void DownloadBook(object sender, EventArgs e)
        {
            try
            {
                progressBar.IsVisible = true;
                downloadLabel.IsVisible = true;
                progressLabel.IsVisible = true;
                booksListView.IsVisible = false;
                ViewModel.LastFileDownloadPath = "";

                Book selectedBook = (Book)((Button)sender).BindingContext;
                var overrideCss = await ReadTextFileAsync(ChapterHtmlBuilder.OverrideCssFileName);

                var downloader = new BookDownloader(_client, ViewModel.DownloadProgress, MainThread.BeginInvokeOnMainThread);
                var epubPath = await downloader.DownloadBookAsync(selectedBook, Config.BooksPath, overrideCss, ViewModel.RetainFolder);

                ViewModel.LastFileDownloadName = selectedBook.getTitle_file_name_safe() + ".epub";
                ViewModel.LastFileDownloadPath = epubPath;

                HideSearchProgressBar();
            }
            catch (Exception exception)
            {
                EnableBookListView();
                await DisplayAlert("Error occured", exception.Message + "\r\n" + exception.StackTrace, " ok");
            }
        }

        private void CloseShareWidget(object sender, EventArgs e)
        {
            ViewModel.LastFileDownloadPath = "";
            ViewModel.LastFileDownloadName = "";
            EnableBookListView();
        }

        private async void DeleteFile(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(ViewModel.LastFileDownloadPath))
                {
                    File.Delete(ViewModel.LastFileDownloadPath);
                }
            }
            catch (Exception exception)
            {
                await DisplayAlert("Error occured", exception.Message + "\r\n" + exception.StackTrace, " ok");
            }

            ViewModel.LastFileDownloadPath = "";
            ViewModel.LastFileDownloadName = "";
            EnableBookListView();
        }

        private async void ShareFile(object sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = ViewModel.LastFileDownloadName,
                File = new ShareFile(ViewModel.LastFileDownloadPath)
            });
        }

        private void EnableBookListView()
        {
            HideSearchProgressBar();
            booksListView.IsVisible = true;
        }

        private void HideSearchProgressBar()
        {
            progressBar.IsVisible = false;
            downloadLabel.IsVisible = false;
            progressLabel.IsVisible = false;
        }

        public async Task<string> ReadTextFileAsync(string fileName)
        {
            try
            {
                using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
                using StreamReader reader = new StreamReader(fileStream);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                return $"Error reading file: {ex.Message}";
            }
        }

        private async void AuthWebView_OnNavigated(object sender, WebNavigatedEventArgs e)
        {
            try
            {
                if (e.Result == WebNavigationResult.Success)
                {
                    var webView = sender as WebView;
                    if (e.Url == "https://learning.oreilly.com/profile/")
                    {
                        // execute javascript in browser control to get the cookie if navigation to profile page was sucess
                        string output = await webView.EvaluateJavaScriptAsync(
                            "JSON.stringify(document.cookie.split(';').map(c => c.split('=')).map(i => [i[0].trim(), i[1].trim()]).reduce((r, i) => {r[i[0]] = i[1]; return r;}, {}))");
                        output = Regex.Unescape(output);

                        File.WriteAllText(Config.COOKIES_FILE, output);
                        webView.IsVisible = false;
                        downloadbtn.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error occured", ex.Message + "\r\n" + ex.StackTrace, " ok");
            }
        }

        private async Task GetJsonAsync(string searchContent)
        {
            var stringResponse = await _client.GetStringAsync(OReillyApi.SearchUrl(searchContent));

            var books = OReillyApi.ParseSearchResults(stringResponse);
            if (books.Count == 0)
            {
                await DisplayAlert("Book not found", "No book was found for " + searchContent, " ok");
            }

            ViewModel.Books.Clear();
            foreach (var book in books)
            {
                ViewModel.Books.Add(book);
            }
        }
    }
}
