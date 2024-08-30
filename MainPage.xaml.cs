﻿

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.TizenSpecific;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows.Input;


namespace SafariBooksDownload
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<Book> Books { get; set; }
        public ICommand DownloadBookCommand { get;  }
        public string progressText = "";

        DownloadViewModel progress = new DownloadViewModel();

        public MainPage()
        {
            InitializeComponent();
            Books = new ObservableCollection<Book>();
            
            BindingContext = this;
            downloadbtn.IsEnabled = false;
            //progressBar.TE
            progressBar.Value = 0;
            downloadLabel.IsVisible = false;
            progressBar.IsVisible = false;
            progressLabel.IsVisible = false;
            
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void OnSearchButtonClick(object sender, EventArgs e)
        {
            // Handle button click here
            //await DisplayAlert("Alert", "Clicked", "ok");

            var searchTerm = bookName.Text;
             await getJsonAsync(searchTerm);

        }

        private async void downloadBook(object sender, EventArgs e)
        {
            try
            {
                progressBar.IsVisible = true;
                downloadLabel.IsVisible = true;
                progressLabel.IsVisible = true;

                Book selectedBook = (Book)((Button)sender).BindingContext;
                //await DisplayAlert("Book not found", selectedBook.title + " " + selectedBook.product_id + "book selected" + "book selected", " ok");
                progressLabel.Text = "Getting book details";
                string _1 = await pupulateBookDetails(selectedBook);
                //selectedBook.nextedTOC = await getFlatTableOFContent(selectedBook);


                progressLabel.Text = "Fetching list of files";
                await parepareListOFFiles(selectedBook);

                progressLabel.Text = "Fetching chapter list";
                List<ChappterInfo> chapters = await fetchChapterInfo(selectedBook);

                var localEpubFolder = Path.Join(Config.BooksPath, selectedBook.getTitle_file_name_safe());
                //ensurePathExists(localEpubFolder);
             
                
                //var oebpsPath = Path.Join(localEpubFolder, "OEBPS");
                //ensurePathExists(oebpsPath);


                //var stylesPath = Path.Join(oebpsPath, "Styles");
                //ensurePathExists(stylesPath);

                //var imagesPath = Path.Join(oebpsPath, "Images");
                //ensurePathExists(imagesPath);
                progressBar.Value = 0;
                string opfPath = "";
                foreach (var file in selectedBook.fileList)
                {
                    if (file.full_path.EndsWith(".opf"))
                    {
                        opfPath = file.full_path;
                    }
                }

                        
                        var s = await DownloadFileAsync(selectedBook, chapters,  localEpubFolder );
                  
                var containeXMLPath = Path.Join(localEpubFolder, "/META-INF/container.xml");

                // create meta-inf folder.
                string directoryPath = Path.GetDirectoryName(containeXMLPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                if (!File.Exists(containeXMLPath))
                {
                    var xmlString = """
                    <?xml version="1.0"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml" /></rootfiles></container>
                    """;
                    xmlString = xmlString.Replace("OEBPS/content.opf", opfPath);
                    File.WriteAllText(containeXMLPath, xmlString);
                }
                string folderName = Path.GetFileName(localEpubFolder);
                string zipPath = Path.Combine(Path.GetDirectoryName(localEpubFolder), folderName + ".zip");
                string epubPath = Path.Combine(Path.GetDirectoryName(localEpubFolder), folderName + ".epub");


                downloadLabel.Text = "Generating epub";
                progressBar.Value = 10;
                progressLabel.Text = "Creating zip";
                // Create zip file
                ZipFile.CreateFromDirectory(localEpubFolder, zipPath);

                progressBar.Value = 30;
                progressLabel.Text = "Creating epub";

                if (File.Exists(epubPath))
                {
                    File.Delete(epubPath);
                }
                // Rename zip file to .epub
                File.Move(zipPath, epubPath);

                // now lets download the files into oebpsPath folder
                //await downloadPages(oebpsPath, selectedBook);
                progressBar.IsVisible = false;
                downloadLabel.IsVisible = false;
                progressLabel.IsVisible = false;
                
            }
            catch(Exception exception)
            {
                progressBar.IsVisible = false;
                downloadLabel.IsVisible = false;
                progressLabel.IsVisible = false;
                await DisplayAlert("Error occured", exception.Message + "\r\n" + exception.StackTrace , " ok");
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
            downloadLabel.Text = "Downloading files";
            int lastPercentage = -1;
            foreach (var file in selectedBook.fileList)
            {
                
                ChappterInfo selectedChapter = null;
                foreach (var chapter in chapters)
                {
                    if (chapter.content_url == file.url)
                    {
                        selectedChapter = chapter;
                    }
                }
                var totalFileCount = selectedBook.fileList.Count;
                int currentFileNo = 1;
                //progressBar.Value = 0;
                

                // total fileCOunt  100 
                //Current
                foreach (var file2 in selectedBook.fileList)
                {
                    int percentDone = (currentFileNo * 100) / totalFileCount;
                    if (lastPercentage != percentDone)
                    {
                        lastPercentage = percentDone;
                        progressBar.Value = percentDone;
                            
                        
                        ;
                        progressLabel.Text = $"Downloading files. {percentDone} percentage done. ";
                    }
                    
                    /*if (currentFileNo % 2 == 0)
                    {
                        fileDownloadProgress.Value = 30;
                    }
                    else
                    {
                        fileDownloadProgress.Value = 31;
                    }
                    */
                    
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
                                        foreach (var styleSheetURL in selectedChapter.related_assets.stylesheets)
                                        {
                                            var adjustedPath = pathAdjuster.AdjustPathsInHtml(styleSheetURL);
                                            extraCSSInfo += $"<link href=\"{adjustedPath}\" rel=\"stylesheet\" type=\"text/css\" />\n";
                                        }

                                    }
                                }
                            }

                            string adjustedHtml = pathAdjuster.AdjustPathsInHtml(File.ReadAllText(localPath));
                            var pointMessage = "<!DOCTYPE html>\n" +
                                "<html lang=\"en\" xml:lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\"" +
                                " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
                                " xsi:schemaLocation=\"http://www.w3.org/2002/06/xhtml2/" +
                                " http://www.w3.org/MarkUp/SCHEMA/xhtml2.xsd\"" +
                                " xmlns:epub=\"http://www.idpf.org/2007/ops\">\n" +
                                "<head>\n" +
                                "<meta charset=\"utf-8\">" +
                                $"{extraCSSInfo}\n" +
                                "<style type=\"text/css\">" +
                                "body{{margin:1em;background-color:transparent!important;}}" +
                                "#sbo-rt-content *{{text-indent:0pt!important;}}#sbo-rt-content .bq{{margin-right:1em!important;}}" +
                                "img{{height: auto;max-width:100%}}" +
                                "pre {{background-color:#EEF2F6 !important;padding:0.75em 1.500em !important;}} " +
                                "</style>" +
                                    "</head>\n" +
                                 $"<body><div class=\"ucvMode-white\"><div id=\"book-content\">{adjustedHtml}</div></div></body>\n</html>";
                            File.WriteAllText(localPath, pointMessage);
                        }

                        Console.WriteLine($"File downloaded and saved to {localPath}");


                    }
                    ++currentFileNo;
                }
            }
            return "";
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

            progressLabel.Text = $"Total {totalFilesCount} files found. 0 of { totalFilesCount / 20 } page's information fetched.";
            progressBar.Value = 0;
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
            progressLabel.Text = $"Total {totalFilesCount} files found. {++downloaded} of {pageCount} page's information fetched.";
            progressBar.Value = percentageDone;
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
            if (e.Result == WebNavigationResult.Success)
            {
                var webView = sender as WebView;
                if(e.Url== "https://learning.oreilly.com/profile/")
                {
                    string output = await webView.EvaluateJavaScriptAsync("JSON.stringify(document.cookie.split(';').map(c => c.split('=')).map(i => [i[0].trim(), i[1].trim()]).reduce((r, i) => {r[i[0]] = i[1]; return r;}, {}))");
                    output = Regex.Unescape(output);
                    
                    File.WriteAllText(Config.COOKIES_FILE, output);
                    webView.IsVisible = false;
                    downloadbtn.IsEnabled = true;
                }
            }
        }

        private async Task<string> getJsonAsync(String searchContent)
        {
            try
            {   
                searchContent = System.Web.HttpUtility.UrlEncode(searchContent);

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
