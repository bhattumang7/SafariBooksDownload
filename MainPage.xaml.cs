


using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp;
using HtmlAgilityPack;
using System.ComponentModel;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Xml.Linq;


namespace SafariBooksDownload
{
    public partial class MainPage : ContentPage
    {
       // public ObservableCollection<Book> Books { get; set; }
        public ICommand DownloadBookCommand { get;  }
        public string progressText = "";

        public DownloadViewModel progress { get; set; } = new DownloadViewModel();

        public MainViewModel ViewModel { get; set; }
        

        public MainPage()
        {
            InitializeComponent();
            
            progress = new DownloadViewModel();
            //BindingContext = this;
            ViewModel = new MainViewModel();
            ViewModel.RetainFolder = false;
            //ViewModel.Books = new ObservableCollection<Book>();
            BindingContext = ViewModel;
            downloadbtn.IsEnabled = false;
            //progressBar.TE
            progress.ProgressBarValue = 0;
            downloadLabel.IsVisible = false;
            progressBar.IsVisible = false;
            progressLabel.IsVisible = false;

            if (File.Exists(Config.COOKIES_FILE))
            {
                AuthWebView.IsVisible = false;
                downloadbtn.IsEnabled = true;
            }
            
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool hasPermission = await PermissionHelper.RequestStoragePermissions();

            if (!hasPermission)
            {
                await DisplayAlert("Permission Required", "Storage access is required to proceed.", "OK");
                // Handle the case where permission is not granted (e.g., disable file access features)
            }
            else
            {
                // Proceed with file access since permission is granted
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
        private void OnSearchTextCompleted(object sender, EventArgs e)
        {
            downloadbtn.SendClicked();
        }

        private async void OnSearchButtonClick(object sender, EventArgs e)
        {
            // Handle button click here
            //await DisplayAlert("Alert", "Clicked", "ok");
            try
            {
                ViewModel.searchInProgress = true;
                ViewModel.Books.Clear();
                var searchTerm = bookName.Text;
                await getJsonAsync(searchTerm);
                ViewModel.searchInProgress = false;
            }
            catch(Exception ex)
            {
                ViewModel.searchInProgress = false;
                await DisplayAlert("Error occured", ex.Message + "\r\n" + ex.StackTrace, " ok");
            }
            

        }

        private async void downloadBook(object sender, EventArgs e)
        {
            try
            {
                progressBar.IsVisible = true;
                downloadLabel.IsVisible = true;
                progressLabel.IsVisible = true;
                booksListView.IsVisible = false;

                Book selectedBook = (Book)((Button)sender).BindingContext;
                //await DisplayAlert("Book not found", selectedBook.title + " " + selectedBook.product_id + "book selected" + "book selected", " ok");
                ViewModel.DownloadProgress.DownloadLabel = selectedBook.title + " (getting details of the book)";
                ViewModel.DownloadProgress.ProgressLabel = "Getting book details";
                string _1 = await pupulateBookDetails(selectedBook);
                //selectedBook.nextedTOC = await getFlatTableOFContent(selectedBook);


                ViewModel.DownloadProgress.ProgressLabel = "Fetching list of files";
                await parepareListOFFiles(selectedBook);

                ViewModel.DownloadProgress.ProgressLabel = "Fetching chapter list";
                List<ChappterInfo> chapters = await fetchChapterInfo(selectedBook);

                var localEpubFolder = Path.Join(Config.BooksPath, selectedBook.product_id);


                progress.ProgressBarValue = 0;
                string opfPath = getOpfFileFullPathFromBook(selectedBook.fileList);

                var s = await DownloadFileAsync(selectedBook, chapters, localEpubFolder);

                // put additional file 
                //var sourceCssFilePath = Path.Combine(FileSystem.AppDataDirectory, "Resources", "Raw", );
                var targetOverrideCSSFilePath = Path.Join(localEpubFolder, "override_v1.css");
                var sourceFileContent = await ReadTextFileAsync("override_v1.css");
                File.WriteAllText(targetOverrideCSSFilePath, sourceFileContent);

                await AddOverrideCSSToManifest(Path.Join(localEpubFolder, opfPath));

                // create meta-inf folder.
                var containeXMLPath = Path.Join(localEpubFolder, "/META-INF/container.xml");
                string directoryPath = Path.GetDirectoryName(containeXMLPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
               
                var xmlString = """
                <?xml version="1.0"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml" /></rootfiles></container>
                """;
                xmlString = xmlString.Replace("OEBPS/content.opf", opfPath);
                File.WriteAllText(containeXMLPath, xmlString);
               
                string folderName = Path.GetFileName(localEpubFolder);
                string zipPath = Path.Combine(Path.GetDirectoryName(localEpubFolder), selectedBook.product_id + ".zip");
                string epubPath = Path.Combine(Path.GetDirectoryName(localEpubFolder),selectedBook.product_id + ".epub");


                progress.DownloadLabel = "Generating epub";
                progress.ProgressBarValue = 10/100;
                progress.ProgressLabel = "Creating zip";
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                // Create zip file
                ZipFile.CreateFromDirectory(localEpubFolder, zipPath);

                progress.ProgressBarValue = 30/100;
                progress.ProgressLabel = "Creating epub";

                if (File.Exists(epubPath))
                {
                    File.Delete(epubPath);
                }
                // Rename zip file to .epub
                File.Move(zipPath, epubPath);

                // attempt to rename the file to a better name if it works. If not, let's use the older name. 
                try
                {
                    string newEpubPath = Path.Combine(Path.GetDirectoryName(localEpubFolder), selectedBook.getTitle_file_name_safe() + ".epub");
                    if (File.Exists(newEpubPath))
                    {
                        File.Delete(newEpubPath);
                    }
                    File.Move(epubPath, newEpubPath);
           
                    epubPath = newEpubPath; // Update epubPath to the new name if successful
                }
                catch (Exception ex)
                {
                    // do not do anything. we keep using the older path
                }

                await DisplayAlert("Epug generated", epubPath , " ok");

                if(!ViewModel.RetainFolder)
                {
                    Directory.Delete(localEpubFolder, recursive: true);
                }

                // now lets download the files into oebpsPath folder
                //await downloadPages(oebpsPath, selectedBook);
                progressBar.IsVisible = false;
                downloadLabel.IsVisible = false;
                progressLabel.IsVisible = false;
                booksListView.IsVisible = true;
            }
            catch (Exception exception)
            {
                progressBar.IsVisible = false;
                downloadLabel.IsVisible = false;
                progressLabel.IsVisible = false;
                booksListView.IsVisible = true;
                await DisplayAlert("Error occured", exception.Message + "\r\n" + exception.StackTrace , " ok");
            }
        }

        private static string getOpfFileFullPathFromBook(List<BookFile> listBookFiles)
        {
            string opfPath = "";
            foreach (var file in listBookFiles)
            {
                if (file.full_path.EndsWith(".opf"))
                {
                    opfPath = file.full_path;
                    break;
                }
            }

            return opfPath;
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

        public async Task AddOverrideCSSToManifest(string filePath)
        {

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            // Load the XML document
            XDocument xmlDoc = XDocument.Load(filePath);

            // Create a new item element
            XNamespace ns = "http://www.idpf.org/2007/opf";

            XElement newItem = new XElement(ns + "item",
                new XAttribute("id", "Oververyowncustomoverrideumang"),
                new XAttribute("href", "override_v1.css"),
                new XAttribute("media-type", "text/css")
            );

            XElement existingItem = xmlDoc.Root.Element("{http://www.idpf.org/2007/opf}manifest")
                .Elements("item")
                .FirstOrDefault(e => e.Attribute("id")?.Value == "Oververyowncustomoverrideumang");

            if (existingItem == null)
            {
                // Add the new item to the manifest
                xmlDoc.Root.Element("{http://www.idpf.org/2007/opf}manifest").Add(newItem);

                // Save the updated XML document
                using (var stream = File.Create(filePath))
                {
                    xmlDoc.Save(stream);
                }
            }
        }

        private async Task<List<ChappterInfo>> fetchChapterInfo(Book selectedBook)
        {
            List<ChappterInfo> chappterInfos = new List<ChappterInfo>();
            string requestURL = "https://learning.oreilly.com/api/v2/epub-chapters/?epub_identifier=urn:orm:book:" + selectedBook.product_id;
            CustomHttpClientHandler customHttpClientHandler = new CustomHttpClientHandler();
            var response = await customHttpClientHandler.GetAsync(requestURL);

            response.EnsureSuccessStatusCode();
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);


            var jsonDocument = JsonDocument.Parse(stringResponse);
            int totalFilesCount = jsonDocument.RootElement.GetProperty("count").GetInt32();
            var results = jsonDocument.RootElement.GetProperty("results");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

            chappterInfos = JsonSerializer.Deserialize<List<ChappterInfo>>(results, options);
            return chappterInfos;
        }

        private async Task<string> DownloadFileAsync(Book selectedBook, List<ChappterInfo> chapters, string localEpubFolder)
        {
            ViewModel.DownloadProgress.DownloadLabel = selectedBook.title + " (downloading files)";
            double lastPercentage = -1;
            var totalFileCount = selectedBook.fileList.Count;
            int currentFileNo = 1;
            foreach (var fileChunk in selectedBook.fileList.Chunk(7))
            {
                double percentDone = (currentFileNo * 100) / totalFileCount;
                if (lastPercentage != percentDone)
                {
                    lastPercentage = percentDone;
                    //await DisplayAlert("percentage", percentDone.ToString(), "ok");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ViewModel.DownloadProgress.ProgressBarValue = percentDone/100;
                        ViewModel.DownloadProgress.ProgressLabel = $"Downloading files. {percentDone} percentage done. ({currentFileNo}/{totalFileCount}) ";
                    });
                }
             
                var downloadTasks = fileChunk.Select(file => downloadSingleFIle(selectedBook, chapters, localEpubFolder, file)).ToList();
                await Task.WhenAll(downloadTasks);

                currentFileNo += 7;

            }
            return "";
        }

        private static async Task downloadSingleFIle(Book selectedBook, List<ChappterInfo> chapters, string localEpubFolder, BookFile file)
        {
            ChappterInfo selectedChapter = null;
            foreach (var chapter in chapters)
            {
                if (chapter.content_url == file.url)
                {
                    selectedChapter = chapter;
                }
            }


            //progress.ProgressBarValue = 0;


            // total fileCOunt  100 
            //Current




            var localPath = Path.Join(localEpubFolder, file.full_path);
            string directoryPath = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (!File.Exists(localPath))
            {

                CustomHttpClientHandler customHttpClientHandler = new CustomHttpClientHandler();
                HttpResponseMessage response = await customHttpClientHandler.GetAsync(file.url);

                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();



                await File.WriteAllBytesAsync(localPath, fileBytes);

                if (file.media_type == "text/css")
                {
                    string cssContent = File.ReadAllText(localPath);

                    // Set up AngleSharp configuration for CSS parsing
                    var config = Configuration.Default.WithCss();
                    var context = BrowsingContext.New(config);
                    var parser = context.GetService<ICssParser>();

                    // Parse the CSS content
                    var stylesheet = parser.ParseStyleSheet(cssContent);
                    removeVisibilityNoneFromCss(stylesheet);
                    AdjustPathForImagesEtcReferredInCSS(selectedBook, file, stylesheet);

                    // Serialize the modified stylesheet back to a string
                    var modifiedCssContent = stylesheet.ToCss();

                    // Write the modified CSS content back to the file (or another file if needed)
                    File.WriteAllText(localPath, modifiedCssContent, Encoding.UTF8);
                }


                if (file.media_type == "text/html" || file.media_type == "application/xhtml+xml")
                {
                    PathAdjuster pathAdjuster = new PathAdjuster(selectedBook.product_id);
                    String extraCSSInfo = "";

                    if (selectedChapter != null)
                    {
                        if (selectedChapter.related_assets != null)
                        {
                            if (selectedChapter.related_assets.stylesheets != null && selectedChapter.related_assets.stylesheets.Count > 0)
                            {

                                selectedChapter.related_assets.stylesheets.Add("https://learning.oreilly.com/api/v2/epubs/urn:orm:book:" + selectedBook.product_id + "/files/override_v1.css");
                                foreach (var styleSheetURL in selectedChapter.related_assets.stylesheets)
                                {
                                    string path = GetRelativePath(selectedBook, file.url, styleSheetURL);


                                    extraCSSInfo += $"<link href=\"{path}\" rel=\"stylesheet\" type=\"text/css\" />\n";
                                }

                            }
                        }
                    }

                    string adjustedHtml = File.ReadAllText(localPath);
                    var pointMessage = "<!DOCTYPE html>\n" +
                        "<html lang=\"en\" xml:lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\"" +
                        " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
                        " xsi:schemaLocation=\"http://www.w3.org/2002/06/xhtml2/" +
                        " http://www.w3.org/MarkUp/SCHEMA/xhtml2.xsd\"" +
                        " xmlns:epub=\"http://www.idpf.org/2007/ops\">\n" +
                        $" <title> {selectedBook.title} </title> \n" +
                        "<head>\n" +
                        "<meta charset=\"utf-8\" /> \n" +
                        //"<link href=\"override_v1.css\" rel=\"stylesheet\" type=\"text/css\" /> \n" +
                        $"{extraCSSInfo}\n" +
                        """
                                                        <style type="text/css">
                              body {
                                margin: 1em;
                                background-color: transparent !important;
                              }
                              #sbo-rt-content * {
                                text-indent: 0pt !important;
                              }
                              #sbo-rt-content .bq {
                                margin-right: 1em !important;
                              }
                              {%- if should_support_kindle -%}
                              #sbo-rt-content * {
                                word-wrap: break-word !important;
                                word-break: break-word !important;
                              }
                              #sbo-rt-content table,
                              #sbo-rt-content pre {
                                overflow-x: unset !important;
                                overflow: unset !important;
                                overflow-y: unset !important;
                                white-space: pre-wrap !important;
                              }
                              {%- endif -%}
                            </style>
                            """ +

                            "</head>\n" +
                            $"<body><div class=\"ucvMode-white\"><div id=\"book-content\">{adjustedHtml}</div></div></body>\n</html>";

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(pointMessage);
                    var imgNodes = htmlDoc.DocumentNode.SelectNodes("//img");
                    if (imgNodes != null)
                    {
                        foreach (var imgNode in imgNodes)
                        {
                            var src = imgNode.GetAttributeValue("src", null);
                            if (src != null)
                            {
                                var relativePath = GetRelativePath(selectedBook, file.url, src);
                                imgNode.SetAttributeValue("src", relativePath);
                            }
                        }
                    }
                    htmlDoc.Save(localPath);

                    //File.WriteAllText(localPath, pointMessage);
                }

                Console.WriteLine($"File downloaded and saved to {localPath}");


            }
        }

        private static void AdjustPathForImagesEtcReferredInCSS(Book selectedBook, BookFile file, ICssStyleSheet stylesheet)
        {
            // update images and make them a relative path
            foreach (var rule in stylesheet.Rules)
            {
                if (rule is ICssStyleRule styleRule)
                {
                    // Properties that can contain URLs
                    string[] propertiesWithUrls = {
                                        "background",
                                        "background-image",
                                        "border-image",
                                        "content",
                                        "cursor",
                                        "list-style-image"
                                    };

                    // Check each property for URLs
                    foreach (var propertyName in propertiesWithUrls)
                    {
                        var propertyValue = styleRule.Style.GetPropertyValue(propertyName);
                        if (!string.IsNullOrWhiteSpace(propertyValue) && propertyValue.Contains("url("))
                        {
                            var pattern = @"url\(\""(.*?)\""\)";
                            var match = Regex.Match(propertyValue, pattern);
                            if (match.Success)
                            {
                                string extractedURL = match.Groups[1].Value;
                                
                                var updatedPath = GetRelativePath(selectedBook, file.url, extractedURL);
                                var escapedUpdatedPath = updatedPath.Replace("\\", "\\\\");
                                var updatedValue = propertyValue.Replace(extractedURL, escapedUpdatedPath);
                                styleRule.Style.SetProperty(propertyName, updatedValue);
                                Console.WriteLine($"Found URL in {propertyName}: {propertyValue}");
                                
                                
                            }

                        }
                    }
                }
                else if (rule is ICssImportRule importRule)
                {
                    // Handle @import rule
                    //Console.WriteLine($"Found @import URL: {importRule.Href}");
                }
            }
        }

        private static void removeVisibilityNoneFromCss(ICssStyleSheet stylesheet)
        {
            // Iterate over all CSS rules
            foreach (var rule in stylesheet.Rules)
            {
                if (rule is ICssStyleRule styleRule)
                {
                    // Check if the rule contains 'display: none;' and replace it with 'visibility: hidden;'
                    var displayProperty = styleRule.Style.GetPropertyValue("display");
                    if (displayProperty == "none")
                    {
                        styleRule.Style.RemoveProperty("display");
                        styleRule.Style.SetProperty("visibility", "hidden");
                    }
                }
            }
        }

        private static string GetRelativePath(Book selectedBook, string fromPath, string toPath)
        {
            var fromUri = new Uri(fromPath);
            Uri toPthUri = new Uri(toPath, UriKind.RelativeOrAbsolute);
            
            if (!Uri.IsWellFormedUriString(toPath, UriKind.Absolute))
            {
                bool found = false;
                foreach (var file in selectedBook.fileList)
                {
                    if (file.url.EndsWith(toPath)){
                        found = true;
                        toPath = file.url;
                        break;
                    }
                }
                if (!found)
                {
                    return toPath;
                }
            }
            
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private async Task<String> parepareListOFFiles(Book selectedBook)
        {
            string requestURL = selectedBook.files_URL;
            CustomHttpClientHandler customHttpClientHandler = new CustomHttpClientHandler();
            var response = await customHttpClientHandler.GetAsync(requestURL);

            response.EnsureSuccessStatusCode();
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);


            var jsonDocument = JsonDocument.Parse(stringResponse);
            int totalFilesCount = jsonDocument.RootElement.GetProperty("count").GetInt32();
            ViewModel.DownloadProgress.DownloadLabel = selectedBook.title + " - (Getting list of files for this book)";
            ViewModel.DownloadProgress.ProgressLabel = "Getting list of files that needs to be dowloaded for " + selectedBook.title;
            MainThread.BeginInvokeOnMainThread(() =>
            {

                ViewModel.DownloadProgress.ProgressLabel = $"Total {totalFilesCount} files found. 0 of {totalFilesCount / 20} page's information fetched.";
                ViewModel.DownloadProgress.ProgressBarValue = 0;
            });

            
            
            await GetNextUrl(selectedBook, selectedBook.files_URL, totalFilesCount , totalFilesCount / 20, 0);
            return "";
        }

        private async Task<string> GetNextUrl(Book selectedBook, string url, int totalFileCount, int pageCount, int downloaded)
        {
            string requestURL = url;
            CustomHttpClientHandler customHttpClientHandler = new CustomHttpClientHandler();
            var response = await customHttpClientHandler.GetAsync(requestURL);

            response.EnsureSuccessStatusCode();
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);


            var jsonDocument = JsonDocument.Parse(stringResponse);
            int totalFilesCount = jsonDocument.RootElement.GetProperty("count").GetInt32();
            var results = jsonDocument.RootElement.GetProperty("results");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

            var fileInfo = JsonSerializer.Deserialize<List<BookFile>>(results, options);
            selectedBook.fileList.AddRange(fileInfo.ToList());
            float percentageDone = (downloaded * 100) / pageCount;
            //pageCount 100
            //downloaded 

            //Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
            //{
            //    ViewModel.DownloadProgress.ProgressLabel = $"Total {totalFilesCount} files found. {++downloaded} of {pageCount} page's information fetched.";
            //    ViewModel.DownloadProgress.ProgressBarValue = percentageDone;
            //});
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ViewModel.DownloadProgress.ProgressLabel = $"Total {totalFilesCount} files found. {++downloaded} of {pageCount} page's information fetched.";
                ViewModel.DownloadProgress.ProgressBarValue = percentageDone / 100;
            });
            
            if (jsonDocument.RootElement.TryGetProperty("next", out JsonElement next))
            {
                var nextString = next.GetString();
                if (nextString != null)
                {
                    await GetNextUrl(selectedBook, next.GetString(), totalFileCount , pageCount, downloaded);
                }
            }
            return "";
        }

        private async Task<List<JsonNodeInfo>> getFlatTableOFContent(Book selectedBook)
        {
            
            List <JsonNodeInfo> tableOfCOntent = new List<JsonNodeInfo>();
            string requestURL = selectedBook.table_of_contents;
            CustomHttpClientHandler customHttpClientHandler = new CustomHttpClientHandler();
            var response = await customHttpClientHandler.GetAsync(requestURL);

            response.EnsureSuccessStatusCode();
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

            var jsonDocument = JsonDocument.Parse(stringResponse);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

            List<JsonNodeInfo> chapters = JsonSerializer.Deserialize<List<JsonNodeInfo>>(stringResponse, options);

            JsonNodeProcessor jsonNodeProcessor = new JsonNodeProcessor();
            jsonNodeProcessor.AssignOrder(chapters);
            int maxDepth = jsonNodeProcessor.getMaxDepth();

            return chapters;
        }

        private async Task<string> downloadPages(String oebpsPath, Book selectedBook)
        {
            foreach(string chapter in selectedBook.chapters)
            {
                CustomHttpClientHandler customHttpClientHandler = new CustomHttpClientHandler();
                var response = await customHttpClientHandler.GetAsync(chapter);

                response.EnsureSuccessStatusCode();
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                int i = 12;
            }
            return "";
        }

        private static void ensurePathExists(string localEpubFolder)
        {
            bool exists = System.IO.Directory.Exists(localEpubFolder);

            if (!exists)
            {
                System.IO.Directory.CreateDirectory(localEpubFolder);
            }
        }

        private async Task<string> pupulateBookDetails(Book book)
        {
            string requestURL = "https://learning.oreilly.com/api/v2/epubs/urn:orm:book:" + book.product_id+"/";
            CustomHttpClientHandler customHttpClientHandler = new CustomHttpClientHandler();
            var response = await customHttpClientHandler.GetAsync(requestURL);

            response.EnsureSuccessStatusCode();
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);


            var jsonDocument = JsonDocument.Parse(stringResponse);
            /*book.chapters = new List<string>();
            foreach(var chapter in jsonDocument.RootElement.GetProperty("chapters").EnumerateArray() )
            {
              book.chapters.Add(chapter.GetString());
            }
            */
            book.isbn = jsonDocument.RootElement.GetProperty("isbn").GetString();
            book.table_of_contents = jsonDocument.RootElement.GetProperty("table_of_contents").GetString();
            book.files_URL = jsonDocument.RootElement.GetProperty("files").GetString();

            return "";
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
                        string output = await webView.EvaluateJavaScriptAsync("JSON.stringify(document.cookie.split(';').map(c => c.split('=')).map(i => [i[0].trim(), i[1].trim()]).reduce((r, i) => {r[i[0]] = i[1]; return r;}, {}))");
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

        private async Task<string> getJsonAsync(String searchContent)
        {
            try
            {   
                searchContent = System.Web.HttpUtility.UrlEncode(searchContent);

                string requestURL = "https://www.oreilly.com/search/api/search/?q=" + searchContent + "&type=book&rows=20&language_with_transcripts=en&tzOffset=-5.5&feature_flags=improveSearchFilters&report=true&isTopics=false";
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
                ViewModel.Books.Clear();
                foreach (var bookEntry in jsonDocument.RootElement.GetProperty("data").GetProperty("products").EnumerateArray())
                {
                    Book book = new Book
                    {
                        title = bookEntry.GetProperty("title").GetString(),
                        product_id = bookEntry.GetProperty("product_id").GetString(),
                        cover_image = bookEntry.GetProperty("cover_image").GetString(),
                        description = bookEntry.GetProperty("description").GetString(),
                        url = bookEntry.GetProperty("url").GetString(),
                        authors = string.Join(", ", bookEntry.GetProperty("authors").EnumerateArray().Select(author => author.GetString())),
                        publication_date = bookEntry.GetProperty("custom_attributes").GetProperty("publication_date").GetString()
                    };
                    ViewModel.Books.Add(book);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return "";
        }

    }

}
