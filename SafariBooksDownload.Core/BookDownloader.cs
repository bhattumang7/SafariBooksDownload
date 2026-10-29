using System.Text;

namespace SafariBooksDownload
{
    public class BookDownloader
    {
        private const int DownloadBatchSize = 7;
        private const int FilesPerPage = 20;

        private readonly IOReillyClient _client;
        private readonly DownloadViewModel _progress;
        private readonly Action<Action> _runOnUi;

        /// <param name="runOnUi">Marshals progress updates to the UI thread; defaults to running inline.</param>
        public BookDownloader(IOReillyClient client, DownloadViewModel progress, Action<Action>? runOnUi = null)
        {
            _client = client;
            _progress = progress;
            _runOnUi = runOnUi ?? (action => action());
        }

        /// <summary>Downloads the whole book into a folder under <paramref name="booksPath"/> and returns the .epub path.</summary>
        public async Task<string> DownloadBookAsync(Book book, string booksPath, string overrideCss, bool retainFolder)
        {
            string? localEpubFolder = null;
            try
            {
                _progress.DownloadLabel = book.title + " (getting details of the book)";
                _progress.ProgressLabel = "Getting book details";
                await PopulateBookDetailsAsync(book);

                _progress.ProgressLabel = "Fetching list of files";
                await LoadFileListAsync(book);

                _progress.ProgressLabel = "Fetching chapter list";
                var chapters = await FetchChaptersAsync(book);

                localEpubFolder = Path.Join(booksPath, book.product_id);
                Directory.CreateDirectory(localEpubFolder);
                var markerPath = Path.Combine(localEpubFolder, WorkFolders.Marker);
                File.WriteAllText(markerPath, "");

                _progress.ProgressBarValue = 0;
                var opfPath = EpubManifest.GetOpfFileFullPath(book.fileList);

                await DownloadFilesAsync(book, chapters, localEpubFolder);

                await File.WriteAllTextAsync(Path.Join(localEpubFolder, ChapterHtmlBuilder.OverrideCssFileName), overrideCss);
                EpubManifest.AddOverrideCssToFile(Path.Join(localEpubFolder, opfPath), ChapterHtmlBuilder.OverrideCssFileName);

                var containerXmlPath = Path.Join(localEpubFolder, "/META-INF/container.xml");
                Directory.CreateDirectory(Path.GetDirectoryName(containerXmlPath)
                                          ?? throw new InvalidOperationException("Could not get directory name from containerXmlPath"));
                await File.WriteAllTextAsync(containerXmlPath, EpubManifest.BuildContainerXml(opfPath));

                _progress.DownloadLabel = "Generating epub";
                _progress.ProgressBarValue = 0.1d;
                _progress.ProgressLabel = "Creating zip";
                var epubPath = EpubPackager.Package(localEpubFolder, book.product_id, book.getTitle_file_name_safe());
                _progress.ProgressBarValue = 0.3d;
                _progress.ProgressLabel = "Creating epub";

                File.Delete(markerPath);
                if (!retainFolder)
                {
                    Directory.Delete(localEpubFolder, recursive: true);
                }

                return epubPath;
            }
            finally
            {
                if (localEpubFolder != null
                    && Directory.Exists(localEpubFolder)
                    && File.Exists(Path.Combine(localEpubFolder, WorkFolders.Marker)))
                {
                    try { Directory.Delete(localEpubFolder, recursive: true); }
                    catch { /* best-effort cleanup on failure */ }
                }
            }
        }

        public async Task PopulateBookDetailsAsync(Book book) =>
            OReillyApi.PopulateBookDetails(book, await _client.GetStringAsync(OReillyApi.BookDetailsUrl(book.product_id)));

        public async Task<List<ChappterInfo>> FetchChaptersAsync(Book book) =>
            OReillyApi.ParseChapters(await _client.GetStringAsync(OReillyApi.ChaptersUrl(book.product_id)));

        public async Task LoadFileListAsync(Book book)
        {
            var page = OReillyApi.ParseFilesPage(await _client.GetStringAsync(book.files_URL));
            int totalFiles = page.Count;
            int pageCount = totalFiles / FilesPerPage;

            _progress.DownloadLabel = book.title + " - (Getting list of files for this book)";
            _progress.ProgressLabel = "Getting list of files that needs to be downloaded for " + book.title;
            _runOnUi(() =>
            {
                _progress.ProgressLabel = $"Total {totalFiles} files found. 0 of {pageCount} page's information fetched.";
                _progress.ProgressBarValue = 0;
            });

            int fetched = 0;
            while (true)
            {
                book.fileList.AddRange(page.Files);
                fetched++;
                int pagesDone = fetched;
                _runOnUi(() =>
                {
                    _progress.ProgressLabel = $"Total {totalFiles} files found. {pagesDone} of {pageCount} page's information fetched.";
                    _progress.ProgressBarValue = ProgressMath.PageFraction(pagesDone, pageCount);
                });

                if (page.Next == null)
                {
                    break;
                }

                page = OReillyApi.ParseFilesPage(await _client.GetStringAsync(page.Next));
            }
        }

        public async Task DownloadFilesAsync(Book book, List<ChappterInfo> chapters, string localEpubFolder)
        {
            _progress.DownloadLabel = book.title + " (downloading files)";
            double lastPercentage = -1;
            var totalFileCount = book.fileList.Count;
            int currentFileNo = 1;
            foreach (var fileChunk in book.fileList.Chunk(DownloadBatchSize))
            {
                double percentDone = ProgressMath.FilePercent(currentFileNo, totalFileCount);
                if (lastPercentage != percentDone)
                {
                    lastPercentage = percentDone;
                    int fileNo = currentFileNo;
                    _runOnUi(() =>
                    {
                        _progress.ProgressBarValue = percentDone / 100;
                        _progress.ProgressLabel =
                            $"Downloading files. {percentDone} percentage done. ({fileNo}/{totalFileCount}) ";
                    });
                }

                await Task.WhenAll(fileChunk.Select(file => DownloadSingleFileAsync(book, chapters, localEpubFolder, file)));

                currentFileNo += DownloadBatchSize;
            }
        }

        public async Task DownloadSingleFileAsync(Book book, List<ChappterInfo> chapters, string localEpubFolder, BookFile file)
        {
            string localPath = Path.Join(localEpubFolder, file.full_path);
            string directoryPath = Path.GetDirectoryName(localPath)
                                   ?? throw new InvalidOperationException($"Could not get directory name from {localPath}.");
            Directory.CreateDirectory(directoryPath);

            if (File.Exists(localPath))
            {
                return;
            }

            byte[] fileBytes = await _client.GetBytesAsync(file.url);

            if (file.media_type == "text/css")
            {
                var css = Encoding.UTF8.GetString(fileBytes);
                var modified = CssProcessor.Process(css, book.fileList, file.url);
                await File.WriteAllTextAsync(localPath, modified, Encoding.UTF8);
            }
            else if (ChapterHtmlBuilder.IsHtml(file))
            {
                var chapter = chapters.LastOrDefault(c => c.content_url == file.url);
                var html = ChapterHtmlBuilder.Build(book, file, chapter, Encoding.UTF8.GetString(fileBytes));
                await File.WriteAllTextAsync(localPath, html, Encoding.UTF8);
            }
            else
            {
                await File.WriteAllBytesAsync(localPath, fileBytes);
            }
        }
    }
}
