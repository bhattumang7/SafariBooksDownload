﻿using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp;
using HtmlAgilityPack;
using System.ComponentModel;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace SafariBooksDownload
{
    public partial class MainPage : ContentPage
    {
        private DownloadViewModel Progress { get; set; }

        private MainViewModel ViewModel { get; set; }


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
                // Handle the case where permission is not granted (e.g., disable file access features)
            }
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
                //await DisplayAlert("Book not found", selectedBook.title + " " + selectedBook.product_id + "book selected" + "book selected", " ok");
                ViewModel.DownloadProgress.DownloadLabel = selectedBook.title + " (getting details of the book)";
                ViewModel.DownloadProgress.ProgressLabel = "Getting book details";
                string _1 = await PupulateBookDetails(selectedBook);
                //selectedBook.nextedTOC = await getFlatTableOFContent(selectedBook);


                ViewModel.DownloadProgress.ProgressLabel = "Fetching list of files";
                await PrepareListOfFiles(selectedBook);

                ViewModel.DownloadProgress.ProgressLabel = "Fetching chapter list";
                var chapters = await FetchChapterInfo(selectedBook);

                var localEpubFolder = Path.Join(Config.BooksPath, selectedBook.product_id);


                Progress.ProgressBarValue = 0;
                var opfPath = GetOpfFileFullPathFromBook(selectedBook.fileList);

                var s = await DownloadFileAsync(selectedBook, chapters, localEpubFolder);

                // put additional override css file to make the code look better
                var targetOverrideCssFilePath = Path.Join(localEpubFolder, "override_v1.css");
                var sourceFileContent = await ReadTextFileAsync("override_v1.css");
                await File.WriteAllTextAsync(targetOverrideCssFilePath, sourceFileContent);

                await AddOverrideCssToManifest(Path.Join(localEpubFolder, opfPath));

                // create meta-inf folder.
                var containerXmlPath = Path.Join(localEpubFolder, "/META-INF/container.xml");
                string directoryPath = Path.GetDirectoryName(containerXmlPath) 
                                       ?? throw new InvalidOperationException("Count not get directory name from containerXmlPath");
                CreateDirectoryIfDoesNotExist(directoryPath);

                var xmlString = """
                                <?xml version="1.0"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml" /></rootfiles></container>
                                """;
                xmlString = xmlString.Replace("OEBPS/content.opf", opfPath);
                await File.WriteAllTextAsync(containerXmlPath, xmlString);

                var folderName = Path.GetFileName(localEpubFolder);
                var localDirectoryName = Path.GetDirectoryName(localEpubFolder) ?? throw new ArgumentNullException("Path.GetDirectoryName(localEpubFolder)"); 
                var zipPath = Path.Combine(localDirectoryName, selectedBook.product_id + ".zip");
                var epubPath = Path.Combine(localDirectoryName,
                    selectedBook.product_id + ".epub");


                Progress.DownloadLabel = "Generating epub";
                Progress.ProgressBarValue = 0.1d;
                Progress.ProgressLabel = "Creating zip";
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                // Create zip file
                ZipFile.CreateFromDirectory(localEpubFolder, zipPath);

                Progress.ProgressBarValue = 0.3d;
                Progress.ProgressLabel = "Creating epub";

                DeleteFileIfExists(epubPath);

                // Rename zip file to .epub
                File.Move(zipPath, epubPath);

                // attempt to rename the file to a better name if it works. If not, let's use the older name. 
                try
                {
                    string newEpubPath = Path.Combine(localDirectoryName,
                        selectedBook.getTitle_file_name_safe() + ".epub");
                    DeleteFileIfExists(newEpubPath);
                    
                    File.Move(epubPath, newEpubPath);

                    epubPath = newEpubPath; // Update epubPath to the new name if successful
                }
                catch (Exception ex)
                {
                    // do not do anything. we keep using the older path
                }

                ViewModel.LastFileDownloadName = selectedBook.getTitle_file_name_safe() + ".epub";
                ViewModel.LastFileDownloadPath = epubPath;

                if (!ViewModel.RetainFolder)
                {
                    Directory.Delete(localEpubFolder, recursive: true);
                }

                HideSearchProgressBar();
            }
            catch (Exception exception)
            {
                EnableBookListView();
                await DisplayAlert("Error occured", exception.Message + "\r\n" + exception.StackTrace, " ok");
            }
        }

        private static void DeleteFileIfExists(string epubPath)
        {
            if (File.Exists(epubPath))
            {
                File.Delete(epubPath);
            }
        }

        private static void CreateDirectoryIfDoesNotExist(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
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

        private static string GetOpfFileFullPathFromBook(List<BookFile> listBookFiles)
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

        public async Task AddOverrideCssToManifest(string filePath)
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

        private async Task<List<ChappterInfo>> FetchChapterInfo(Book selectedBook)
        {
            string requestUrl = "https://learning.oreilly.com/api/v2/epub-chapters/?epub_identifier=urn:orm:book:" +
                                selectedBook.product_id;
            ORiellyHttpClientAdapter oRiellyHttpClientAdapter = new ORiellyHttpClientAdapter();
            var response = await oRiellyHttpClientAdapter.GetAsync(requestUrl);

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
            
            return JsonSerializer.Deserialize<List<ChappterInfo>>(results, options) ?? throw new InvalidOperationException("from FetchChapterInfo");
        }

        private async Task<string> DownloadFileAsync(Book selectedBook, List<ChappterInfo> chapters,
            string localEpubFolder)
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
                        ViewModel.DownloadProgress.ProgressBarValue = percentDone / 100;
                        ViewModel.DownloadProgress.ProgressLabel =
                            $"Downloading files. {percentDone} percentage done. ({currentFileNo}/{totalFileCount}) ";
                    });
                }

                var downloadTasks = fileChunk
                    .Select(file => DownloadSingleFIle(selectedBook, chapters, localEpubFolder, file)).ToList();
                await Task.WhenAll(downloadTasks);

                currentFileNo += 7;
            }

            return "";
        }

        private static async Task DownloadSingleFIle(Book selectedBook, List<ChappterInfo> chapters,
            string localEpubFolder, BookFile file)
        {
            ChappterInfo? selectedChapter = null;
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


            string localPath = Path.Join(localEpubFolder, file.full_path) ??
                               throw new Exception(
                                   $"Could not join {localEpubFolder} and {file.full_path} to create EPUB path");
            string directoryPath = Path.GetDirectoryName(localPath) ??
                                   throw new Exception($"Could get directory name from {localPath}.");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(localPath))
            {
                ORiellyHttpClientAdapter oRiellyHttpClientAdapter = new ORiellyHttpClientAdapter();
                HttpResponseMessage response = await oRiellyHttpClientAdapter.GetAsync(file.url);

                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();


                await File.WriteAllBytesAsync(localPath, fileBytes);

                if (file.media_type == "text/css")
                {
                    string cssContent = await File.ReadAllTextAsync(localPath);

                    // Set up AngleSharp configuration for CSS parsing
                    var config = Configuration.Default.WithCss();
                    var context = BrowsingContext.New(config);
                    var parser = context.GetService<ICssParser>();

                    // Parse the CSS content
                    var stylesheet = parser.ParseStyleSheet(cssContent);
                    RemoveVisibilityNoneFromCss(stylesheet);
                    AdjustPathForImagesEtcReferredInCss(selectedBook, file, stylesheet);

                    // Serialize the modified stylesheet back to a string
                    var modifiedCssContent = stylesheet.ToCss();

                    // Write the modified CSS content back to the file (or another file if needed)
                    File.WriteAllText(localPath, modifiedCssContent, Encoding.UTF8);
                }


                var styleSheetSection = $$"""
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
                                          """;
                if (file.media_type == "text/html" || file.media_type == "application/xhtml+xml")
                {
                    var pathAdjuster = new PathAdjuster(selectedBook.product_id);
                    var extraCssInfo = "";

                    if (selectedChapter?.related_assets.stylesheets is { Count: > 0 })
                    {
                        selectedChapter.related_assets.stylesheets.Add(
                            "https://learning.oreilly.com/api/v2/epubs/urn:orm:book:" + selectedBook.product_id +
                            "/files/override_v1.css");
                        foreach (var styleSheetUrl in selectedChapter.related_assets.stylesheets)
                        {
                            var path = GetRelativePath(selectedBook, file.url, styleSheetUrl);
                            extraCssInfo += $"<link href=\"{path}\" rel=\"stylesheet\" type=\"text/css\" />\n";
                        }
                    }

                    var adjustedHtml = await File.ReadAllTextAsync(localPath);
                    var pointMessage = $"""
                                        <!DOCTYPE html>
                                        <html lang="en" xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://www.w3.org/2002/06/xhtml2/ http://www.w3.org/MarkUp/SCHEMA/xhtml2.xsd" xmlns:epub="http://www.idpf.org/2007/ops">
                                         <title> {selectedBook.title} </title> 
                                        <head>
                                        <meta charset="utf-8" /> 
                                        {extraCssInfo}
                                        {styleSheetSection}
                                       </head>
                                       <body><div class="ucvMode-white"><div id="book-content">{adjustedHtml}</div></div></body>
                                       </html>
                    """;

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

        private static void AdjustPathForImagesEtcReferredInCss(Book selectedBook, BookFile file,
            ICssStyleSheet stylesheet)
        {
            // update images and make them a relative path
            foreach (var rule in stylesheet.Rules)
            {
                if (rule is ICssStyleRule styleRule)
                {
                    // Properties that can contain URLs
                    string[] propertiesWithUrls =
                    [
                        "background",
                        "background-image",
                        "border-image",
                        "content",
                        "cursor",
                        "list-style-image"
                    ];

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
                                string extractedUrl = match.Groups[1].Value;

                                var updatedPath = GetRelativePath(selectedBook, file.url, extractedUrl);
                                var escapedUpdatedPath = updatedPath.Replace("\\", "\\\\");
                                var updatedValue = propertyValue.Replace(extractedUrl, escapedUpdatedPath);
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

        private static void RemoveVisibilityNoneFromCss(ICssStyleSheet stylesheet)
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
            var toPthUri = new Uri(toPath, UriKind.RelativeOrAbsolute);

            if (!Uri.IsWellFormedUriString(toPath, UriKind.Absolute))
            {
                bool found = false;
                foreach (var file in selectedBook.fileList)
                {
                    if (file.url.EndsWith(toPath))
                    {
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
            return Uri.UnescapeDataString(relativeUri.ToString())
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private async Task<string> PrepareListOfFiles(Book selectedBook)
        {
            var requestUrl = selectedBook.files_URL;
            var customHttpClientHandler = new ORiellyHttpClientAdapter();
            var response = await customHttpClientHandler.GetAsync(requestUrl);

            response.EnsureSuccessStatusCode();
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);


            var jsonDocument = JsonDocument.Parse(stringResponse);
            var totalFilesCount = jsonDocument.RootElement.GetProperty("count").GetInt32();
            ViewModel.DownloadProgress.DownloadLabel = selectedBook.title + " - (Getting list of files for this book)";
            ViewModel.DownloadProgress.ProgressLabel =
                "Getting list of files that needs to be downloaded for " + selectedBook.title;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ViewModel.DownloadProgress.ProgressLabel =
                    $"Total {totalFilesCount} files found. 0 of {totalFilesCount / 20} page's information fetched.";
                ViewModel.DownloadProgress.ProgressBarValue = 0;
            });


            await GetNextUrl(selectedBook, selectedBook.files_URL, totalFilesCount, totalFilesCount / 20, 0);
            return "";
        }

        private async Task<string> GetNextUrl(Book selectedBook, string url, int totalFileCount, int pageCount,
            int downloaded)
        {
            ORiellyHttpClientAdapter oRiellyHttpClientAdapter = new ORiellyHttpClientAdapter();
            var response = await oRiellyHttpClientAdapter.GetAsync(url);

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
            double progressLevel = ((downloaded * 100) / pageCount) / 100;
            //pageCount 100
            //downloaded 

            //Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
            //{
            //    ViewModel.DownloadProgress.ProgressLabel = $"Total {totalFilesCount} files found. {++downloaded} of {pageCount} page's information fetched.";
            //    ViewModel.DownloadProgress.ProgressBarValue = percentageDone;
            //});
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ViewModel.DownloadProgress.ProgressLabel =
                    $"Total {totalFilesCount} files found. {++downloaded} of {pageCount} page's information fetched.";
                ViewModel.DownloadProgress.ProgressBarValue = progressLevel;
            });

            if (jsonDocument.RootElement.TryGetProperty("next", out JsonElement next))
            {
                var nextString = next.GetString();
                if (nextString != null)
                {
                    await GetNextUrl(selectedBook, nextString, totalFileCount, pageCount, downloaded);
                }
            }

            return "";
        }

        private async Task<List<JsonNodeInfo>> GetFlatTableOfContent(Book selectedBook)
        {
            List<JsonNodeInfo> tableOfContent = new List<JsonNodeInfo>();
            string requestUrl = selectedBook.table_of_contents;
            ORiellyHttpClientAdapter oRiellyHttpClientAdapter = new ORiellyHttpClientAdapter();
            var response = await oRiellyHttpClientAdapter.GetAsync(requestUrl);

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

        private async Task<string> DownloadPages(String oebpsPath, Book selectedBook)
        {
            foreach (string chapter in selectedBook.chapters)
            {
                ORiellyHttpClientAdapter oRiellyHttpClientAdapter = new ORiellyHttpClientAdapter();
                var response = await oRiellyHttpClientAdapter.GetAsync(chapter);

                response.EnsureSuccessStatusCode();
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
                int i = 12;
            }

            return "";
        }

        private static void EnsurePathExists(string localEpubFolder)
        {
            bool exists = System.IO.Directory.Exists(localEpubFolder);

            if (!exists)
            {
                System.IO.Directory.CreateDirectory(localEpubFolder);
            }
        }

        private async Task<string> PupulateBookDetails(Book book)
        {
            string requestUrl = "https://learning.oreilly.com/api/v2/epubs/urn:orm:book:" + book.product_id + "/";
            ORiellyHttpClientAdapter oRiellyHttpClientAdapter = new ORiellyHttpClientAdapter();
            var response = await oRiellyHttpClientAdapter.GetAsync(requestUrl);

            response.EnsureSuccessStatusCode();
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            var stringResponse = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);


            var jsonDocument = JsonDocument.Parse(stringResponse);
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

        private async Task<string> GetJsonAsync(String searchContent)
        {
            try
            {
                searchContent = System.Web.HttpUtility.UrlEncode(searchContent);

                string searchRequestUrl = "https://www.oreilly.com/search/api/search/?q=" + searchContent +
                                    "&type=book&rows=20&language_with_transcripts=en&tzOffset=-5.5&feature_flags=improveSearchFilters&report=true&isTopics=false";
                ORiellyHttpClientAdapter oRiellyHttpClientAdapter = new ORiellyHttpClientAdapter();
                var response = await oRiellyHttpClientAdapter.GetAsync(searchRequestUrl);

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
                foreach (var bookEntry in jsonDocument.RootElement.GetProperty("data").GetProperty("products")
                             .EnumerateArray())
                {
                    Book book = new Book
                    {
                        title = bookEntry.GetProperty("title").GetString(),
                        product_id = bookEntry.GetProperty("product_id").GetString(),
                        cover_image = bookEntry.GetProperty("cover_image").GetString(),
                        description = bookEntry.GetProperty("description").GetString(),
                        url = bookEntry.GetProperty("url").GetString(),
                        authors = string.Join(", ",
                            bookEntry.GetProperty("authors").EnumerateArray().Select(author => author.GetString())),
                        publication_date = bookEntry.GetProperty("custom_attributes").GetProperty("publication_date")
                            .GetString()
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